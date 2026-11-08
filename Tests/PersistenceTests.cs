using System.Net;
using Project.CLI.Core;
using Project.CLI.Rendering;
using Project.CLI.Strategies;
using Project.Core.Characters.Models;
using Project.Core.Characters.Services;
using Project.Infra.Caching;
using Project.Infra.Data.Repositories;
using Project.Infra.Data.SaveSystem;
using Project.Infra.Display;
using Project.Infra.Remote;
using Tests.Doubles;

namespace Tests;

/// <summary>
/// Unit and integration tests validating Practical Work 15:
/// - Asset / Portrait local file caching & buffering (Cache-aside, offline fallback, statistics)
/// - Game state persistence, save slots, and "Continue from last save" resumption.
/// </summary>
public class PersistenceTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _cacheDir;
    private readonly string _savesDir;

    public PersistenceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "pk15_tests_" + Guid.NewGuid().ToString("N"));
        _cacheDir = Path.Combine(_testDir, "cache", "portraits");
        _savesDir = Path.Combine(_testDir, "saves");
        Directory.CreateDirectory(_cacheDir);
        Directory.CreateDirectory(_savesDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    #region Asset / Portrait Caching Tests

    [Fact]
    public async Task AssetCache_CacheMiss_DownloadsAndSavesFileLocally()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x01, 0x02, 0x03])
        });
        var httpClient = new HttpClient(fakeHandler);
        var cacheService = new LocalFileAssetCacheService(_cacheDir, httpClient);

        // Act
        var localPath = await cacheService.GetOrDownloadPortraitAsync("Raiden Shogun", "https://example.com/raiden.png");

        // Assert
        Assert.True(File.Exists(localPath));
        Assert.True(cacheService.IsCached("Raiden Shogun"));
        Assert.Equal(1, fakeHandler.CallCount);

        var stats = cacheService.GetStatistics();
        Assert.Equal(1, stats.TotalCachedFiles);
        Assert.Equal(0, stats.CacheHits);
        Assert.Equal(1, stats.CacheMisses);
    }

    [Fact]
    public async Task AssetCache_CacheHit_DoesNotMakeNetworkRequest()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47])
        });
        var httpClient = new HttpClient(fakeHandler);
        var cacheService = new LocalFileAssetCacheService(_cacheDir, httpClient);

        // First call: Cache miss -> downloads
        await cacheService.GetOrDownloadPortraitAsync("Zhongli", "https://example.com/zhongli.png");
        Assert.Equal(1, fakeHandler.CallCount);

        // Act: Second call -> Cache hit
        var localPath = await cacheService.GetOrDownloadPortraitAsync("Zhongli", "https://example.com/zhongli.png");

        // Assert
        Assert.True(File.Exists(localPath));
        Assert.Equal(1, fakeHandler.CallCount); // No new network call!

        var stats = cacheService.GetStatistics();
        Assert.Equal(1, stats.CacheHits);
        Assert.Equal(1, stats.CacheMisses);
        Assert.Equal(0.5, stats.HitRatio, precision: 2);
    }

    [Fact]
    public async Task AssetCache_OfflineFallback_GeneratesLocalAssetWhenNetworkFails()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var httpClient = new HttpClient(fakeHandler);
        var cacheService = new LocalFileAssetCacheService(_cacheDir, httpClient);

        // Act
        var localPath = await cacheService.GetOrDownloadPortraitAsync("Venti", "https://invalid.server/venti.png");

        // Assert
        Assert.True(File.Exists(localPath));
        var bytes = await File.ReadAllBytesAsync(localPath);
        Assert.NotEmpty(bytes);
        Assert.Equal(0x89, bytes[0]); // Valid PNG magic byte
        Assert.Equal(0x50, bytes[1]); // 'P'
        Assert.Equal(0x4E, bytes[2]); // 'N'
        Assert.Equal(0x47, bytes[3]); // 'G'
    }

    [Fact]
    public async Task AssetCache_Prefetch_BuffersMultiplePortraits()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47])
        });
        var httpClient = new HttpClient(fakeHandler);
        var cacheService = new LocalFileAssetCacheService(_cacheDir, httpClient);
        var names = new[] { "Diluc", "Jean", "Kaeya" };

        // Act
        var paths = await cacheService.PrefetchPortraitsAsync(names);

        // Assert
        Assert.Equal(3, paths.Count);
        foreach (var p in paths)
        {
            Assert.True(File.Exists(p));
        }
        var stats = cacheService.GetStatistics();
        Assert.Equal(3, stats.TotalCachedFiles);
    }

    [Fact]
    public void AssetCache_ClearCache_DeletesFilesAndResetsCounters()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([0x01, 0x02, 0x03])
        });
        var httpClient = new HttpClient(fakeHandler);
        var cacheService = new LocalFileAssetCacheService(_cacheDir, httpClient);

        cacheService.GetOrDownloadPortraitAsync("Keqing", "https://example.com/keqing.png").GetAwaiter().GetResult();
        Assert.Equal(1, cacheService.GetStatistics().TotalCachedFiles);

        // Act
        cacheService.ClearCache();

        // Assert
        var stats = cacheService.GetStatistics();
        Assert.Equal(0, stats.TotalCachedFiles);
        Assert.Equal(0, stats.CacheHits);
        Assert.Equal(0, stats.CacheMisses);
        Assert.False(cacheService.IsCached("Keqing"));
    }

    #endregion

    #region Game Save / Load & Continuation Tests

    [Fact]
    public void GameSaveManager_SaveAndLoad_RestoresFullGameState()
    {
        // Arrange
        var saveManager = new GameSaveManager(_savesDir);
        var displayer = new FakeDisplayer();
        var game = new Game(displayer) { SaveManager = saveManager };

        var sword = game.CreateItem("DragonSlayer", 30, 25, armorMod: 10, speedMod: 5);
        var ability = game.CreateAbility("Thunderclap", 60, 0, "Deals massive electro shock");
        var hero = game.CreateCharacter("Kazuha", 150, 40, 0.15, [ability], [sword], armor: 15, speed: 110);
        hero.AddStatusEffect(new StatusEffect("WindWard", StatusEffectType.Buff, 3, armorBonus: 10));

        // Act: Save to slot
        var metadata = saveManager.Save("kazuha_adventure", game);

        // Assert Save Metadata
        Assert.Equal("kazuha_adventure", metadata.SlotName);
        Assert.Equal(1, metadata.CharacterCount);
        Assert.Contains("Kazuha", metadata.CharacterNames);
        Assert.True(File.Exists(metadata.FilePath));

        // Act: Load into fresh game instance
        var restoredGame = new Game(new FakeDisplayer()) { SaveManager = saveManager };
        var loadedSnapshot = saveManager.Load("kazuha_adventure");
        restoredGame.ImportSave(loadedSnapshot);

        // Assert Restored State
        Assert.Single(restoredGame.Characters);
        var loadedHero = restoredGame.Characters[0];
        Assert.Equal("Kazuha", loadedHero.Name);
        Assert.Equal(150, loadedHero.Health);
        Assert.Equal(110, loadedHero.Speed);
        Assert.Equal(15, loadedHero.BaseArmor);
        Assert.Single(loadedHero.ActiveItems);
        Assert.Equal("DragonSlayer", loadedHero.ActiveItems[0].Name);
        Assert.Single(loadedHero.AvailableAbilities);
        Assert.Equal("Thunderclap", loadedHero.AvailableAbilities[0].Name);
        Assert.Single(loadedHero.StatusEffects);
        Assert.Equal("WindWard", loadedHero.StatusEffects[0].Name);
    }

    [Fact]
    public void GameSaveManager_ContinueFromLatest_RestoresMostRecentSave()
    {
        // Arrange
        var saveManager = new GameSaveManager(_savesDir);
        var game = new Game(new FakeDisplayer()) { SaveManager = saveManager };

        // Save slot 1
        game.CreateCharacter("OldHero", 80, 15, 0.1);
        saveManager.Save("slot_1_morning", game);

        // Simulate time delay
        Thread.Sleep(50);

        // Save slot 2 with new character
        game.CreateCharacter("RecentHero", 120, 30, 0.2);
        saveManager.Save("slot_2_evening", game);

        // Act: Create new blank game and continue from latest
        var targetGame = new Game(new FakeDisplayer()) { SaveManager = saveManager };
        var latestSnapshot = saveManager.ContinueFromLatest(targetGame);

        // Assert: Resumed state must contain slot_2_evening
        Assert.Equal("slot_2_evening", latestSnapshot.SaveName);
        Assert.Equal(2, targetGame.Characters.Count);
        Assert.Contains(targetGame.Characters, c => c.Name == "RecentHero");
    }

    [Fact]
    public void GameSaveManager_ContinueFromLatest_ThrowsWhenNoSavesExist()
    {
        // Arrange
        var emptySaveDir = Path.Combine(_testDir, "empty_saves");
        var saveManager = new GameSaveManager(emptySaveDir);
        var targetGame = new Game(new FakeDisplayer()) { SaveManager = saveManager };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => saveManager.ContinueFromLatest(targetGame));
    }

    [Fact]
    public void GameSaveManager_ListSaves_ReturnsSortedByTimestampDescending()
    {
        // Arrange
        var saveManager = new GameSaveManager(_savesDir);
        var game = new Game(new FakeDisplayer()) { SaveManager = saveManager };

        game.CreateCharacter("HeroA", 100, 20, 0.1);
        saveManager.Save("save_alpha", game);

        Thread.Sleep(50);
        game.CreateCharacter("HeroB", 100, 20, 0.1);
        saveManager.Save("save_beta", game);

        Thread.Sleep(50);
        game.CreateCharacter("HeroC", 100, 20, 0.1);
        saveManager.Save("save_gamma", game);

        // Act
        var saves = saveManager.ListSaves();

        // Assert: gamma should be first (most recent)
        Assert.Equal(3, saves.Count);
        Assert.Equal("save_gamma", saves[0].SlotName);
        Assert.Equal("save_beta", saves[1].SlotName);
        Assert.Equal("save_alpha", saves[2].SlotName);
    }

    [Fact]
    public void GameSaveManager_DeleteSave_RemovesFileFromDisk()
    {
        // Arrange
        var saveManager = new GameSaveManager(_savesDir);
        var game = new Game(new FakeDisplayer()) { SaveManager = saveManager };
        game.CreateCharacter("DisposableHero", 50, 10, 0.0);
        var meta = saveManager.Save("to_delete", game);
        Assert.True(File.Exists(meta.FilePath));

        // Act
        var deleted = saveManager.DeleteSave("to_delete");

        // Assert
        Assert.True(deleted);
        Assert.False(File.Exists(meta.FilePath));
        Assert.Empty(saveManager.ListSaves());
    }

    #endregion

    #region CLI Commands Integration Tests

    [Fact]
    public void Cli_SaveAndContinue_ResumesGameSessionSeamlessly()
    {
        // Arrange
        var saveManager = new GameSaveManager(_savesDir);
        var cacheService = new LocalFileAssetCacheService(_cacheDir);
        var repository = new JsonRepositoryService(Path.Combine(_testDir, "repo.json"));
        var apiClient = new GenshinApiClient(new HttpClient());
        var renderer = new FakeRenderer();

        var game = new Game(new FakeDisplayer())
        {
            SaveManager = saveManager,
            PortraitCache = cacheService
        };

        game.CreateCharacter("Ayaka", 110, 35, 0.15);

        var ctx = new CommandExecutionContext(
            game,
            repository,
            apiClient,
            renderer,
            assetCache: cacheService,
            saveManager: saveManager);

        var strategy = new InfrastructureCommandStrategy();
        var commands = strategy.GetCommands().ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        // 1. Execute 'save checkpoint_ayaka'
        var saveCmd = commands["save"];
        var saveResult = saveCmd.Execute(new Args(["checkpoint_ayaka"]), ctx);
        Assert.True(saveResult.IsSuccess);

        // 2. Wipe memory
        var blankGame = new Game(new FakeDisplayer())
        {
            SaveManager = saveManager,
            PortraitCache = cacheService
        };
        Assert.Empty(blankGame.Characters);

        var resumeCtx = new CommandExecutionContext(
            blankGame,
            repository,
            apiClient,
            renderer,
            assetCache: cacheService,
            saveManager: saveManager);

        // 3. Execute 'continue'
        var continueCmd = commands["continue"];
        var resumeResult = continueCmd.Execute(new Args([]), resumeCtx);

        // Assert
        Assert.True(resumeResult.IsSuccess);
        Assert.Single(blankGame.Characters);
        Assert.Equal("Ayaka", blankGame.Characters[0].Name);
    }

    [Fact]
    public void Cli_CacheCommand_StatusAndClear_OperatesCorrectly()
    {
        // Arrange
        var saveManager = new GameSaveManager(_savesDir);
        var cacheService = new LocalFileAssetCacheService(_cacheDir);
        var repository = new JsonRepositoryService(Path.Combine(_testDir, "repo.json"));
        var apiClient = new GenshinApiClient(new HttpClient());
        var renderer = new FakeRenderer();

        var game = new Game(new FakeDisplayer())
        {
            SaveManager = saveManager,
            PortraitCache = cacheService
        };

        var ctx = new CommandExecutionContext(
            game,
            repository,
            apiClient,
            renderer,
            assetCache: cacheService,
            saveManager: saveManager);

        var strategy = new InfrastructureCommandStrategy();
        var cacheCmd = strategy.GetCommands().First(c => c.Name == "cache");

        // 1. Query status
        var statusRes = cacheCmd.Execute(new Args(["status"]), ctx);
        Assert.True(statusRes.IsSuccess);

        // 2. Clear cache
        var clearRes = cacheCmd.Execute(new Args(["clear"]), ctx);
        Assert.True(clearRes.IsSuccess);
        Assert.Equal(0, cacheService.GetStatistics().TotalCachedFiles);
    }

    #endregion

    #region Helper Doubles

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public int CallCount { get; private set; }

        public FakeHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_response);
        }
    }

    private sealed class FakeRenderer : IRenderer
    {
        public List<string> Lines { get; } = [];

        public void RenderLine(string text = "") => Lines.Add(text);
        public void RenderStatus(string message, bool isSuccess) => Lines.Add($"[STATUS:{(isSuccess ? "OK" : "ERR")}] {message}");
        public void RenderTable(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows) => Lines.Add($"[TABLE with {rows.Count} rows]");
        public void RenderBox(string title, IReadOnlyList<string> lines) => Lines.Add($"[BOX: {title}]");
        public void RenderProgressBar(int current, int total, string label = "") => Lines.Add($"[PROGRESS {current}/{total} {label}]");
    }

    #endregion
}
