using Project.CLI.Presenter;
using Project.Core.Characters.Services;
using Project.Core.Text.Services;
using Project.Infra.Data.Repositories;
using Project.Infra.Display;
using Project.Infra.Remote;

namespace Project;

/// <summary>
/// Application entry point configuring dependency injection and starting presentation loop.
/// Integrates Remote REST API client and Structured Text Profile rendering.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        // 1. Configure Infrastructure layer (Display, Persistence, Remote API)
        IDisplayer consoleDisplayer = new ConsoleDisplayer();
        IDisplayer silentDisplayer = new NullDisplayer();
        IRepositoryService repository = new JsonRepositoryService("repository.json");
        IGenshinApiClient apiClient = new GenshinApiClient(new HttpClient());

        // 2. Configure Core domain facades
        var game = new Game(silentDisplayer);
        var textManager = new TextDocumentManager(silentDisplayer, "OOP_Architecture_Guide");

        // 3. Seed initial baseline demo data silently
        SeedInitialData(game, textManager);

        // 4. Connect live console displayer
        game.SetDisplayer(consoleDisplayer);
        textManager.SetDisplayer(consoleDisplayer);

        // 5. Configure CLI layer (Presenter and Commands)
        IPresenter presenter = new ConsolePresenter(consoleDisplayer, repository, game, textManager, apiClient);

        // 6. Run application
        presenter.Run(args);
    }

    private static void SeedInitialData(Game game, TextDocumentManager textManager)
    {
        // Seed RPG entities
        var sword = game.CreateItem("Excalibur", 20, 35);
        var shield = game.CreateItem("Aegis_Shield", 60, 5);
        var potion = game.CreateItem("Elixir_of_Vitality", 100, 0);

        var firestrike = game.CreateAbility("Firestrike", 45, 0, "Unleashes blazing flames on the enemy");
        var holylight = game.CreateAbility("HolyLight", 0, 40, "Blesses target with divine healing energy");

        var aragorn = game.CreateCharacter("Aragorn", 120, 30, 0.20, [firestrike], [sword]);
        var gandalf = game.CreateCharacter("Gandalf", 90, 20, 0.10, [firestrike, holylight], [shield]);

        // Seed Structured Text Document
        var sec1 = textManager.AddHeading("1. Architectural Layers", 1);
        textManager.Cd("1. Architectural Layers");
        textManager.AddParagraph("Modern software architecture strictly separates domain logic from infrastructural concerns.");
        textManager.AddLink("Clean Architecture Reference", "https://martinfowler.com/articles/dipInTheWild.html");

        var subSec1 = textManager.AddHeading("1.1 Data Transfer Objects", 2);
        textManager.Cd("1.1 Data Transfer Objects");
        textManager.AddParagraph("DTOs encapsulate formatting and transport concerns, preventing presentation leaks into Core.");
        textManager.AddImage("DTO Architecture Diagram", "https://example.org/dto-diagram.png");

        textManager.Cd("/");
        var sec2 = textManager.AddHeading("2. Persistence with Repository", 1);
        textManager.Cd("2. Persistence with Repository");
        textManager.AddParagraph("The repository pattern abstracts JSON serialization away from business operations.");

        textManager.Cd("/");
    }
}
