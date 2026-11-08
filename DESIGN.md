# Software Design Document (SDD)
## Subsystem: Persistence, Asset Buffering & State Continuation

**Project Name:** Modular Application Platform & RPG Character Engine  
**Module:** Infrastructure & Persistence Layer (Practical Work 15)  
**Architecture Style:** Clean Architecture (Repository Pattern, Cache-Aside, Atomic File I/O, Memento/Snapshot)  
**Language & Platform:** C# 12, .NET 8.0, xUnit  
**Status:** Approved & Finalized  

---

## 1. Executive Summary & Purpose

The purpose of this document is to specify the architectural design, serialization protocols, and component interactions of the **Persistence & Caching Subsystem** for the RPG platform and Character Catalog.

In enterprise software engineering, preserving state and optimizing network interactions are foundational requirements:
1. **Minimizing Network Bandwidth & Latency (Buffering/Caching):** Frequent REST requests for media assets (character portraits) saturate bandwidth and degrade user experience. A deterministic local disk buffer using the **Cache-Aside pattern** guarantees immediate access to graphics and offline availability.
2. **State Persistence & Fault-Tolerant Continuation:** User progress, created characters, inventory items, and dynamic combat states are held in volatile RAM during execution. By leveraging structured JSON serialization with **atomic write operations** and a dedicated slot manager, the application guarantees that progress survives process restarts and allows seamless resumption from the latest save point.

---

## 2. High-Level Architectural Context

```mermaid
graph TD
    CLI[Presentation: CLI & Facade] --> Game[Domain Facade: Game]
    CLI --> CacheCmd[Infrastructure Command: cache]
    CLI --> SaveCmd[Infrastructure Command: save / load / continue]
    
    subgraph Persistence Subsystem
        Game --> SaveMgr[IGameSaveManager: GameSaveManager]
        SaveMgr --> AtomicWrite[Atomic File Writer: .tmp -> .json]
        SaveMgr --> SavesDir[(Saves Storage: saves/*.save.json)]
        Game --> Repo[IRepositoryService: JsonRepositoryService]
        Repo --> RepoFile[(repository.json)]
    end

    subgraph Caching & Buffering Subsystem
        Game --> CacheSvc[IAssetCacheService: LocalFileAssetCacheService]
        CLI --> CacheSvc
        CacheSvc --> LocalDisk[(Local Disk Cache: cache/portraits/*.png)]
        CacheSvc -->|Cache Miss| RemoteAPI[Remote REST API / CDN]
        CacheSvc -->|Network Fail| Fallback[Embedded 1x1 PNG Synthesizer]
    end
```

### Architectural Principles
- **Separation of Concerns:** Business logic (`Character`, `TurnBasedBattleEngine`) remains completely agnostic of disk layout, serialization format, and network clients.
- **Cache-Aside Pattern:** Consumers query the cache service. If cached, the local disk file is returned immediately; if missed, the file is downloaded, persisted atomically, and returned.
- **Atomic I/O Resilience:** Writes stage data to unique temporary files (`.tmp`) before renaming, eliminating file truncation or corrupt JSON states on unexpected power cuts or crashes.
- **Single Source of Truth for Continuation:** Save snapshots include UTC timestamps and checksum metadata. The save manager determines the latest save point dynamically.

---

## 3. Character Catalog Asset Buffering (Cache-Aside)

### 3.1. Design & Protocol
The asset caching engine (`LocalFileAssetCacheService`) implements `IAssetCacheService`:
- **Cache Key Normalization:** Sanitizes arbitrary character names into URL/filesystem-safe slugs (e.g., `"Raiden Shogun"` $\to$ `"raiden-shogun.png"`).
- **Lookup Protocol:**
  1. Inspects `cache/portraits/{slug}.png`.
  2. If file exists and size $> 0$: **Cache Hit** $\to$ increments hits counter, returns local path.
  3. If missing: **Cache Miss** $\to$ issues HTTP GET request to CDN/API.
  4. On HTTP success: writes response byte array to `.tmp`, atomically renames to target path, increments misses counter.
  5. On HTTP error/offline: falls back to an embedded 67-byte valid PNG byte array, persists locally, and returns valid path.

```mermaid
sequenceDiagram
    autonumber
    participant Client as Consumer (CLI / ViewModel)
    participant Cache as LocalFileAssetCacheService
    participant Disk as Local Filesystem (cache/portraits)
    participant Remote as External REST API / CDN

    Client->>Cache: GetOrDownloadPortraitAsync("Raiden Shogun")
    Cache->>Cache: NormalizeKey("Raiden Shogun") -> "raiden-shogun.png"
    Cache->>Disk: File.Exists("raiden-shogun.png")
    
    alt Cache Hit
        Disk-->>Cache: File exists (bytes > 0)
        Cache->>Cache: Increment Hits
        Cache-->>Client: Local Path: cache/portraits/raiden-shogun.png
    else Cache Miss
        Disk-->>Cache: File missing
        Cache->>Cache: Increment Misses
        Cache->>Remote: HTTP GET /characters/raiden-shogun/icon-big
        alt Remote Available
            Remote-->>Cache: 200 OK (PNG bytes)
        else Network Unavailable / Offline
            Cache->>Cache: Synthesize Fallback PNG bytes
        end
        Cache->>Disk: Write to raiden-shogun.png.tmp
        Cache->>Disk: Atomic Rename to raiden-shogun.png
        Cache-->>Client: Local Path: cache/portraits/raiden-shogun.png
    end
```

### 3.2. Telemetry & Metrics
The cache collects real-time diagnostics accessible via `CacheStatistics`:
$$\text{Total Requests} = \text{Hits} + \text{Misses}$$
$$\text{Hit Ratio} = \frac{\text{Hits}}{\text{Total Requests}}$$
Metrics include file counts, total disk bytes, hit ratio %, and directory paths.

---

## 4. Game State Persistence & Continuation

### 4.1. Save Slot Serialization Model
Save files are stored as independent, human-readable JSON files in `saves/{slotName}.save.json`.
Each save file encapsulates:
- `SaveName`: Identifies the slot name.
- `LastModified`: UTC ISO-8601 timestamp for ordering and continuation.
- `Characters`: List of characters, health, power, armor, speed, resistance, and active `StatusEffects`.
- `Items`: Equipped gear with `HpMod`, `PowerMod`, `ArmorMod`, and `SpeedMod`.
- `Abilities`: Learned abilities, magic flags, resistance penetration, and scaling factors.
- `CharacterItems` & `CharacterAbilities`: Relational join tables.

### 4.2. "Continue from Last Save" Protocol
The `continue` command enables seamless session resumption without specifying slot names:
1. `GameSaveManager.GetLatestSave()` enumerates `saves/*.save.json`.
2. For each file, the header and timestamp are evaluated.
3. The entry with the highest `Timestamp` (or newest file write time) is identified.
4. If found, `ContinueFromLatest(game)` deserializes the file, wipes current memory state, rebuilds domain aggregates, and informs the user.
5. If no saves exist, a graceful notification is rendered.

```mermaid
sequenceDiagram
    autonumber
    participant User as User / CLI
    participant Cmd as InfrastructureCommandStrategy
    participant SaveMgr as GameSaveManager
    participant Disk as Filesystem (saves/)
    participant Game as Domain Facade (Game)

    User->>Cmd: ExecCommand("continue")
    Cmd->>SaveMgr: GetLatestSave()
    SaveMgr->>Disk: EnumerateFiles("saves/*.save.json")
    Disk-->>SaveMgr: [slot1.save.json, slot2.save.json, ...]
    SaveMgr->>SaveMgr: Sort by LastModified DESC
    SaveMgr-->>Cmd: SaveMetadata (Latest: slot2)
    
    Cmd->>SaveMgr: ContinueFromLatest(Game)
    SaveMgr->>Disk: ReadAllText("slot2.save.json")
    Disk-->>SaveMgr: JSON String
    SaveMgr->>SaveMgr: Deserialize to CharacterSave
    SaveMgr->>Game: ImportSave(CharacterSave)
    Game->>Game: Rebuild Characters, Items, Abilities, Statuses
    Cmd-->>User: RenderBox("CONTINUED FROM LAST SAVE: slot2")
```

---

## 5. Class Diagrams & Component Contracts

```mermaid
classDiagram
    class IAssetCacheService {
        <<interface>>
        +string CacheDirectory
        +bool IsCached(string assetKey)
        +string GetLocalFilePath(string assetKey)
        +Task~string~ GetOrDownloadAssetAsync(string assetKey, string remoteUrl)
        +Task~string~ GetOrDownloadPortraitAsync(string characterName, string remoteUrl)
        +Task~List~string~~ PrefetchPortraitsAsync(IEnumerable~string~ names)
        +CacheStatistics GetStatistics()
        +void ClearCache()
    }

    class LocalFileAssetCacheService {
        -string _cacheDirectory
        -HttpClient _httpClient
        -int _cacheHits
        -int _cacheMisses
        +GetOrDownloadPortraitAsync()
        +ClearCache()
    }

    class IGameSaveManager {
        <<interface>>
        +string SavesDirectory
        +SaveMetadata Save(string slotName, Game game)
        +CharacterSave Load(string slotName)
        +SaveMetadata GetLatestSave()
        +CharacterSave ContinueFromLatest(Game targetGame)
        +List~SaveMetadata~ ListSaves()
        +bool DeleteSave(string slotName)
    }

    class GameSaveManager {
        -string _savesDirectory
        -JsonSerializerOptions _jsonOptions
        +Save()
        +Load()
        +ContinueFromLatest()
        +ListSaves()
    }

    class SaveMetadata {
        +string SlotName
        +string FilePath
        +DateTime Timestamp
        +int CharacterCount
        +int ItemCount
        +int AbilityCount
        +long FileSizeBytes
        +List~string~ CharacterNames
    }

    class CacheStatistics {
        +int TotalCachedFiles
        +long TotalSizeBytes
        +int CacheHits
        +int CacheMisses
        +double HitRatio
    }

    IAssetCacheService <|.. LocalFileAssetCacheService
    IGameSaveManager <|.. GameSaveManager
    GameSaveManager ..> SaveMetadata
    LocalFileAssetCacheService ..> CacheStatistics
    Game --> IAssetCacheService
    Game --> IGameSaveManager
```

---

## 6. Storage Locations & System Conventions

As highlighted in the theoretical syllabus:
- **Project-Level Portability:** Defaults to local workspace folders (`cache/portraits/` and `saves/`), allowing self-contained test execution, versioning, and zero permission issues across Windows and Linux.
- **Operating System Standards:** Can be configured to redirect to:
  - Windows: `%LOCALAPPDATA%\ModularRpgPlatform\cache` and `saves`
  - Linux / macOS: `~/.local/share/ModularRpgPlatform/` or `~/.cache/`
- **Clean Hygiene:** No scattered files in user root directories.

---

## 7. Verification & Testing Strategy

The persistence and buffering system is validated via 10 comprehensive xUnit tests in `Tests/PersistenceTests.cs`:
1. `AssetCache_CacheMiss_DownloadsAndSavesFileLocally`: Verifies HTTP fetch and file persistence on cache miss.
2. `AssetCache_CacheHit_DoesNotMakeNetworkRequest`: Verifies zero HTTP calls when asset exists on disk.
3. `AssetCache_OfflineFallback_GeneratesLocalAssetWhenNetworkFails`: Verifies valid PNG synthesis during network outages.
4. `AssetCache_Prefetch_BuffersMultiplePortraits`: Validates concurrent batch caching.
5. `AssetCache_ClearCache_DeletesFilesAndResetsCounters`: Validates disk cleanup and metric reset.
6. `GameSaveManager_SaveAndLoad_RestoresFullGameState`: Asserts full restoration of characters, items, abilities, dynamic stats, and status effects.
7. `GameSaveManager_ContinueFromLatest_RestoresMostRecentSave`: Creates multiple chronological saves and ensures `continue` selects the newest.
8. `GameSaveManager_ContinueFromLatest_ThrowsWhenNoSavesExist`: Ensures graceful error handling.
9. `GameSaveManager_ListSaves_ReturnsSortedByTimestampDescending`: Verifies descending timestamp order.
10. `Cli_SaveAndContinue_ResumesGameSessionSeamlessly`: End-to-end command-line workflow validation.

---

## 8. Conclusion

Practical Work 15 fulfills all functional and architectural requirements:
- High-efficiency asset buffering (Cache-Aside pattern) preventing redundant network calls.
- Atomic file-based state saving with full support for user-created characters.
- Instant, fault-tolerant game resumption via the `continue` command.
- Complete architectural documentation and automated test verification.
