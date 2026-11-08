using Project.Core.Text.Builders;
using Project.Core.Text.Factories;
using Project.Core.Text.Prototypes;
using Project.Core.Text.Services;

namespace Project.CLI.Commands;

/// <summary>
/// Sets up and registers commands dedicated to the Text structured document subsystem.
/// Incorporates Generative Patterns: Builder, Prototype, Abstract Factory, and Singleton.
/// </summary>
public static class TextCommands
{
    private static readonly SectionTemplateRegistry _templateRegistry = new();

    public static void Register(CommandRegistry registry, Func<string, string> promptReader)
    {
        // 1. pwd
        registry.Register(
            "pwd",
            "Prints current hierarchical path in the document.",
            "pwd",
            (cmd, ctx) =>
            {
                ctx.Displayer.Display(ctx.TextManager.Pwd());
            });

        // 2. cd <path>|[--id <id>]
        registry.Register(
            "cd",
            "Navigates to specified heading section by path or ID.",
            "cd <path> or cd --id <container_id>",
            (cmd, ctx) =>
            {
                var idStr = cmd.GetOption("id");
                if (int.TryParse(idStr, out var id))
                {
                    ctx.TextManager.CdById(id);
                    return;
                }

                var target = cmd.Arguments.Count > 0 ? cmd.Arguments[0] : promptReader("Target path: ");
                ctx.TextManager.Cd(target);
            });

        // 3. up
        registry.Register(
            "up",
            "Moves current position up to parent heading container.",
            "up",
            (cmd, ctx) =>
            {
                ctx.TextManager.Up();
            });

        // 4. add <container|leaf> <type>
        registry.Register(
            "add",
            "Adds a container (heading) or leaf (paragraph, link, image) to current position.",
            "add <container|leaf> <type> [params...]",
            (cmd, ctx) =>
            {
                var kind = cmd.Arguments.Count > 0 ? cmd.Arguments[0].ToLowerInvariant() : "";
                if (string.IsNullOrEmpty(kind))
                {
                    ctx.Displayer.Display("Select addition category: 1) Container (Heading), 2) Leaf (Paragraph/Link/Image)");
                    var choice = promptReader("Choice (1-2): ");
                    kind = choice == "1" ? "container" : "leaf";
                }

                if (kind == "container" || kind == "heading" || kind == "section")
                {
                    var name = cmd.Arguments.Count > 1 ? cmd.Arguments[1] : promptReader("Heading Title: ");
                    var rankStr = cmd.Arguments.Count > 2 ? cmd.Arguments[2] : null;
                    int? rank = int.TryParse(rankStr, out var r) ? r : null;
                    ctx.TextManager.AddHeading(name, rank);
                }
                else if (kind == "leaf" || kind == "item")
                {
                    var leafType = cmd.Arguments.Count > 1 ? cmd.Arguments[1].ToLowerInvariant() : "";
                    if (string.IsNullOrEmpty(leafType))
                    {
                        ctx.Displayer.Display("Select leaf type: 1) Paragraph, 2) Link, 3) Image");
                        var lc = promptReader("Choice (1-3): ");
                        leafType = lc switch { "1" => "paragraph", "2" => "link", "3" => "image", _ => lc.ToLowerInvariant() };
                    }

                    switch (leafType)
                    {
                        case "p":
                        case "paragraph":
                        {
                            var content = cmd.Arguments.Count > 2
                                ? string.Join(" ", cmd.Arguments.Skip(2))
                                : promptReader("Paragraph Content: ");
                            ctx.TextManager.AddParagraph(content);
                            break;
                        }
                        case "link":
                        {
                            var text = cmd.Arguments.Count > 2 ? cmd.Arguments[2] : promptReader("Link Text: ");
                            var url = cmd.Arguments.Count > 3 ? cmd.Arguments[3] : promptReader("URL: ");
                            ctx.TextManager.AddLink(text, url);
                            break;
                        }
                        case "img":
                        case "image":
                        {
                            var alt = cmd.Arguments.Count > 2 ? cmd.Arguments[2] : promptReader("Alt Text: ");
                            var src = cmd.Arguments.Count > 3 ? cmd.Arguments[3] : promptReader("Source URL: ");
                            ctx.TextManager.AddImage(alt, src);
                            break;
                        }
                        default:
                            ctx.Displayer.Display($"Unknown leaf type '{leafType}'. Valid types: paragraph, link, image.");
                            break;
                    }
                }
                else
                {
                    ctx.Displayer.Display($"Unknown kind '{kind}'. Valid options: container, leaf.");
                }
            });

        // 5. rm [<elem_name>] [--id <id>]
        registry.Register(
            "rm",
            "Removes child element from current heading or removes current heading and moves up.",
            "rm [<elem_name>] or rm --id <id>",
            (cmd, ctx) =>
            {
                var force = cmd.HasOption("force") || cmd.HasOption("y") || cmd.HasOption("yes");
                var idStr = cmd.GetOption("id");
                if (int.TryParse(idStr, out var id))
                {
                    if (force || promptReader($"Confirm removal of element ID #{id}? (y/n): ").StartsWith("y", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.TextManager.Remove(id: id);
                    }
                    return;
                }

                var name = cmd.Arguments.Count > 0 ? cmd.Arguments[0] : null;
                var targetDesc = name ?? "current container";
                if (force || promptReader($"Confirm removal of {targetDesc}? (y/n): ").StartsWith("y", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.TextManager.Remove(name: name);
                }
            });

        // 6. print [--whole] [--id] [--toc]
        registry.Register(
            "print",
            "Renders the current node or entire document structure.",
            "print [--whole] [--id] [--toc]",
            (cmd, ctx) =>
            {
                var whole = cmd.HasOption("whole");
                var showId = cmd.HasOption("id");
                var tocOnly = cmd.HasOption("toc");
                ctx.TextManager.Print(whole, showId, tocOnly);
            });

        // 7. save [name]
        registry.Register(
            "save",
            "Saves current document snapshot into the JSON repository.",
            "save [save_name]",
            (cmd, ctx) =>
            {
                var name = cmd.Arguments.Count > 0 ? cmd.Arguments[0] : $"Doc_{DateTime.Now:yyyyMMdd_HHmmss}";
                var save = ctx.TextManager.ExportSave(name);
                ctx.Repository.SaveTextDocument(name, save);
                ctx.Displayer.Display($"Document state successfully saved to repository as '{name}'.");
            });

        // 8. load <name>
        registry.Register(
            "load",
            "Loads a saved document state from the JSON repository.",
            "load [save_name]",
            (cmd, ctx) =>
            {
                var name = cmd.Arguments.Count > 0 ? cmd.Arguments[0] : null;
                if (string.IsNullOrEmpty(name))
                {
                    var saves = ctx.Repository.GetTextSaveNames().ToList();
                    if (saves.Count == 0)
                    {
                        ctx.Displayer.Display("No saved documents found in repository.");
                        return;
                    }
                    ctx.Displayer.Display("Available document saves: " + string.Join(", ", saves));
                    name = promptReader("Enter save name to load: ");
                }

                var loaded = ctx.Repository.LoadTextDocument(name);
                if (loaded == null)
                {
                    ctx.Displayer.Display($"Save '{name}' not found in repository.");
                    return;
                }

                ctx.TextManager.ImportSave(loaded);
            });

        // 9. build (Generative Pattern: Builder & Abstract Factory)
        registry.Register(
            "build",
            "[Builder Pattern] Constructs a full multi-level document using DocumentTreeBuilder and ITextElementFactory.",
            "build [title] [--format plain|markdown|html]",
            (cmd, ctx) =>
            {
                var title = cmd.Arguments.Count > 0 ? cmd.Arguments[0] : "Architectural_Specification";
                var format = cmd.GetOption("format", "markdown")?.ToLowerInvariant();

                ITextElementFactory factory = format switch
                {
                    "html" => new HtmlElementFactory(),
                    "plain" => new PlainTextElementFactory(),
                    _ => new MarkdownElementFactory()
                };

                ctx.Displayer.Display($"[Builder Pattern] Building document '{title}' using {factory.FormatName} Factory...");

                var builder = new DocumentTreeBuilder(title)
                    .WithFactory(factory)
                    .AddParagraph("Executive Summary: This document was assembled via the DocumentTreeBuilder creational pattern.")
                    .AddSection("1. System Design", 1, sec =>
                    {
                        sec.AddParagraph("Core domain models are decoupled from output presentation formats.");
                        sec.AddSubsection("1.1 Creational Patterns", sub =>
                        {
                            sub.AddParagraph("Builder abstracts tree construction, while Abstract Factory controls syntax rendering.");
                            sub.AddLink("GoF Patterns Reference", "https://refactoring.guru/design-patterns");
                        });
                    })
                    .AddSection("2. Architecture Verification", 1, sec =>
                    {
                        sec.AddParagraph("All generative components are validated via automated xUnit tests.");
                        sec.AddImage("Architecture Diagram", "https://example.org/diagram.png");
                    });

                var builtRoot = builder.Build();

                // Re-initialize manager with new root
                var newManager = new TextDocumentManager(ctx.Displayer, builtRoot.Name);
                newManager.ImportSave(new Project.Infra.Data.Serialization.TextSave
                {
                    SlotName = title,
                    Nodes = builtRoot.Children.Count > 0 ? null : []
                });

                // Set in Singleton context
                ActiveDocumentContext.Instance.SetActiveDocument(newManager);

                // Add built children into active manager's root
                foreach (var child in builtRoot.Children)
                {
                    newManager.Root.AddChild(child);
                }

                ctx.Displayer.Display($"[✓] Document '{title}' successfully built and activated via Builder pattern.");
                newManager.Print(whole: true);
            });

        // 10. clone (Generative Pattern: Prototype)
        registry.Register(
            "clone",
            "[Prototype Pattern] Clones a template section from the prototype registry into the current document node.",
            "clone <template_key> [new_title]",
            (cmd, ctx) =>
            {
                if (cmd.Arguments.Count == 0)
                {
                    ctx.Displayer.Display($"Available prototype templates: {string.Join(", ", _templateRegistry.TemplateKeys)}");
                    ctx.Displayer.Display("Usage: clone <methodology|architecture|conclusion> [Custom Title]");
                    return;
                }

                var templateKey = cmd.Arguments[0];
                var customTitle = cmd.Arguments.Count > 1 ? cmd.Arguments[1] : null;

                try
                {
                    var clonedSection = _templateRegistry.CreateFromTemplate(templateKey, customTitle);
                    ctx.TextManager.CurrentNode.AddChild(clonedSection);
                    ctx.Displayer.Display($"[Prototype Pattern] Cloned prototype '{templateKey}' into '{ctx.TextManager.CurrentNode.Name}' as '{clonedSection.Name}'.");
                }
                catch (KeyNotFoundException ex)
                {
                    ctx.Displayer.Display($"[Error] {ex.Message}");
                }
            });

        // 11. switch
        registry.Register(
            "switch",
            "Switches the active CLI mode to Characters game subsystem.",
            "switch",
            (cmd, ctx) =>
            {
                ctx.SwitchToMode = "chars";
            });

        // 12. help
        registry.Register(
            "help",
            "Displays reference of available Text mode commands.",
            "help",
            (cmd, ctx) =>
            {
                ctx.Displayer.Display("=== Text Mode Commands ===");
                foreach (var c in registry.GetAllCommands())
                {
                    ctx.Displayer.Display($"  {c.Usage,-50} - {c.Description}");
                }
            });

        // 13. exit / quit
        Action<ParsedCommand, CommandContext> exitAction = (cmd, ctx) =>
        {
            ctx.ShouldExit = true;
            ctx.Displayer.Display("Exiting interactive session.");
        };
        registry.Register("exit", "Exits the application.", "exit", exitAction);
        registry.Register("quit", "Exits the application.", "quit", exitAction);
    }
}
