using Project.CLI.Rendering;
using Project.Core.Characters.Services;
using Project.Infra.Caching;
using Project.Infra.Data.Repositories;
using Project.Infra.Data.SaveSystem;
using Project.Infra.Remote;

namespace Project.CLI.Core;

/// <summary>
/// Execution environment passed to commands providing access to domain models, persistence, asset caching, and rendering.
/// </summary>
public class CommandExecutionContext
{
    public Game Game { get; }
    public IRepositoryService Repository { get; }
    public IGenshinApiClient ApiClient { get; }
    public IRenderer Renderer { get; }
    public IAssetCacheService AssetCache { get; }
    public IGameSaveManager SaveManager { get; }
    public Func<string, string> PromptReader { get; }
    public bool ShouldExit { get; set; } = false;

    public CommandExecutionContext(
        Game game,
        IRepositoryService repository,
        IGenshinApiClient apiClient,
        IRenderer renderer,
        Func<string, string>? promptReader = null,
        IAssetCacheService? assetCache = null,
        IGameSaveManager? saveManager = null)
    {
        Game = game;
        Repository = repository;
        ApiClient = apiClient;
        Renderer = renderer;
        PromptReader = promptReader ?? (_ => string.Empty);
        AssetCache = assetCache ?? game.PortraitCache;
        SaveManager = saveManager ?? game.SaveManager;
    }
}
