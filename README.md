# Практичне заняття 15: Постійність інформації

**Дисципліна:** Об'єктно-орієнтоване програмування (3 курс)  
**Тема:** Постійність інформації (Data Persistence, Caching & Buffering): локальна буферизація графічних портретів, збереження/відновлення стану гри через слоти та функція продовження з останнього сейву (Continue)  
**Стек / Платформа:** .NET 8 (C# 12), xUnit 2.5  

---

## 🎯 Мета роботи
- Забезпечити надійне збереження користувацької інформації та стану додатка між різними сеансами запуску програми згідно з вимогами Clean Architecture.
- **Для каталогу персонажів:**
  - Реалізувати сервіс буферизації та кешування зображень (`LocalFileAssetCacheService`) за патерном **Cache-Aside**.
  - Налаштувати завантаження портретів персонажів з віддаленого REST API / CDN і збереження їх у локальні файли на диску (`cache/portraits/`), щоб кардинально зменшити кількість мережевих запитів.
  - Реалізувати механізм відмовостійкості (offline fallback): у разі відсутності інтернету або помилки сервера генерувати валідний бінарний образ для безперебійної роботи.
  - Вести діагностичну статистику кешу: кількість файлів, розмір у байтах, Cache Hits, Cache Misses, коефіцієнт влучань (Hit Ratio %).
- **Для текстової гри / RPG:**
  - Реалізувати систему управління сейвами (`GameSaveManager`) із виділеними слотами (`saves/{slotName}.save.json`).
  - Забезпечити повне збереження (Save) та відновлення (Load) стану гри: створених користувачем персонажів, спорядження, навичок, динамічних характеристик (Armor, Speed, Resistance) та активних статус-ефектів.
  - Забезпечити продовження гри з останнього збереження (**Continue from last save**): автоматичний пошук найновішого сейву за часовою міткою UTC та безшовне відновлення сеансу.
  - Використовувати атомарний запис файлів (через тимчасові файли `.tmp` та `File.Move`), що запобігає пошкодженню даних у разі раптового збою програми.
- Скласти детальний архітектурний дизайн-документ ([DESIGN.md](file:///d:/Project/lerning/pk15persist/DESIGN.md)).
- Покрити всі компоненти модульним тестуванням на базі xUnit.

---

## 🏛️ Архітектура рішення

```
pk15persist/
├── Infra/                              # Рівень інфраструктури (збереження, кеш, мережа)
│   ├── Caching/
│   │   ├── IAssetCacheService.cs       # Контракт кешування та буферизації медіа
│   │   ├── CacheStatistics.cs          # Діагностична телеметрія кешу (Hits, Misses, Ratio)
│   │   └── LocalFileAssetCacheService.cs # Локальний дисковий буфер з атомарним записом
│   ├── Data/
│   │   ├── SaveSystem/
│   │   │   ├── IGameSaveManager.cs     # Контракт збереження слотів та продовження
│   │   │   ├── SaveMetadata.cs         # Метадані сейву (слот, дата, розмір, герої)
│   │   │   └── GameSaveManager.cs      # Атомарне управління файлами saves/*.save.json
│   │   ├── Models/
│   │   │   └── DataEntities.cs         # DTO сутностей з підтримкою розширених характеристик
│   │   ├── Repositories/
│   │   │   ├── IRepositoryService.cs
│   │   │   └── JsonRepositoryService.cs
│   │   └── Serialization/
│   │       ├── CharacterSave.cs
│   │       └── SaveBase.cs
│   ├── Display/
│   └── Remote/
├── Core/                               # Домен та бізнес-логіка (BLL)
│   ├── Characters/
│   │   ├── Services/
│   │   │   └── Game.cs                 # Фасад гри з методами SaveGame, LoadGame, ContinueFromLatest
│   │   ├── Models/
│   │   │   ├── Character.cs
│   │   │   ├── Item.cs
│   │   │   ├── Ability.cs
│   │   │   └── StatusEffect.cs
│   │   ├── TurnBased/
│   │   ├── Combat/
│   │   └── ViewModels/
│   │       └── CharacterProfileViewModel.cs # Відображення з локальним шляхом до портрета
│   ├── FileWorkspace/
│   └── Text/
├── CLI/                                # Рівень взаємодії з користувачем
│   ├── Strategies/
│   │   ├── InfrastructureCommandStrategy.cs # Команди save, load, continue, saves, cache
│   │   ├── InformationCommandStrategy.cs    # Вивід каталогу з мітками [CACHED] / [REMOTE]
│   │   └── TransformativeCommandStrategy.cs
│   ├── Facade/
│   └── Rendering/
├── Tests/
│   ├── PersistenceTests.cs             # 10 модульних та інтеграційних тестів кешу й сейвів
│   ├── BusinessLogicTests.cs           # 13 тестів формул та покрокового рушія
│   └── ...
├── DESIGN.md                           # Архітектурна специфікація постійності інформації
└── README.md                           # Документація роботи
```

---

## 💾 Робота підсистеми постійності

### 1. Буферизація портретів (Cache-Aside Pattern)
- Під час першого запиту портрета персонажа (`GetOrDownloadPortraitAsync`) перевіряється директорія `cache/portraits/`.
- **Cache Hit:** якщо файл існує на диску, повертається локальний шлях, лічильник влучань збільшується, запит до мережі **не виконується**.
- **Cache Miss:** виконується завантаження зображення через HTTP-клієнт, файл записується на диск атомарно (запис у `.tmp` $\to$ перейменування у `.png`), повертається локальний шлях.
- **Offline Fallback:** якщо мережа недоступна, синтезується валідний бінарний PNG-файл, що гарантує безперервність роботи додатка офлайн.

### 2. Збереження та відновлення стану гри (Save / Load / Continue)
- Кожне збереження записується в окремий файл `saves/{slotName}.save.json`.
- Запис виконується атомарно, що повністю виключає пошкодження або частковий запис файлу при вимкненні живлення.
- Функція **`continue`** автоматично аналізує директорію сейвів, знаходить найновіший файл за часовою міткою UTC (`Timestamp`) та безшовно розгортає повний стан гри: персонажів, їхній поточний стан HP, навички, предмети та бафи.

---

## 🧪 Тестування (xUnit)

Файл `Tests/PersistenceTests.cs` покриває:
1. `AssetCache_CacheMiss_DownloadsAndSavesFileLocally`: завантаження при першому зверненні та збереження на диск.
2. `AssetCache_CacheHit_DoesNotMakeNetworkRequest`: повторне звернення не робить HTTP-запитів.
3. `AssetCache_OfflineFallback_GeneratesLocalAssetWhenNetworkFails`: генерація валідного PNG при збої мережі.
4. `AssetCache_Prefetch_BuffersMultiplePortraits`: пакетне попереднє кешування портретів.
5. `AssetCache_ClearCache_DeletesFilesAndResetsCounters`: очищення кешу та скидання метрик.
6. `GameSaveManager_SaveAndLoad_RestoresFullGameState`: збереження і відновлення персонажів, предметів, навичок та ефектів.
7. `GameSaveManager_ContinueFromLatest_RestoresMostRecentSave`: пошук найновішого сейву серед кількох та продовження гри.
8. `GameSaveManager_ContinueFromLatest_ThrowsWhenNoSavesExist`: коректна реакція при відсутності сейвів.
9. `GameSaveManager_ListSaves_ReturnsSortedByTimestampDescending`: сортування списку збережень за спаданням часу.
10. `Cli_SaveAndContinue_ResumesGameSessionSeamlessly`: наскрізний тест CLI-команд `save` та `continue`.

---

## 🚀 Командний рядок (CLI)

```bash
# 1. Робота зі збереженнями
dotnet run --project pk15persist/Project.csproj -- save my_adventure   # Зберегти гру в слот
dotnet run --project pk15persist/Project.csproj -- saves                 # Переглянути таблицю сейвів
dotnet run --project pk15persist/Project.csproj -- continue              # Продовжити з останнього сейву
dotnet run --project pk15persist/Project.csproj -- load my_adventure   # Завантажити конкретний слот

# 2. Робота з буфером зображень
dotnet run --project pk15persist/Project.csproj -- cache status          # Переглянути статистику кешу (Hits, Misses, MB)
dotnet run --project pk15persist/Project.csproj -- cache fetch raiden    # Забуферизувати портрет персонажа
dotnet run --project pk15persist/Project.csproj -- cache prefetch       # Завантажити портрети для всього каталогу
dotnet run --project pk15persist/Project.csproj -- cache clear          # Очистити локальний кеш портретів

# 3. Запуск тестів
dotnet test pk15persist/Tests/Tests.csproj
```
