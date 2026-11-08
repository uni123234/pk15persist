using System.Text;
using Project.CLI.Rendering;
using Project.Core.Characters.Models;

namespace Project.Infra.Display.DTOs;

/// <summary>
/// Data Transfer Object encapsulating Character state with stylized ANSI card presentation.
/// Implements both IDisplayable and IRenderable.
/// </summary>
public class CharacterDto(Character character) : IDisplayable, IRenderable
{
    public int Id { get; init; } = character.Id;
    public string Name { get; init; } = character.Name;
    public int Health { get; init; } = character.Health;
    public int MaxHealth { get; init; } = character.MaxHealth;
    public int Power { get; init; } = character.Power;
    public double Resistance { get; init; } = character.Resistance;

    public List<(int, string)> ItemNames { get; init; } = character.ActiveItems
        .Select(item => (item.Id, item.Name)).ToList();

    public List<(int, string)> AbilityNames { get; init; } = character.AvailableAbilities
        .Select(ability => (ability.Id, ability.Name)).ToList();

    public string Render()
    {
        if (Health <= 0)
        {
            return $"\u001b[1;31m[DEFEATED]\u001b[0m Char: {Name} is out of fight.";
        }

        var sb = new StringBuilder();
        var healthPercent = MaxHealth > 0 ? (double)Health / MaxHealth : 0;
        var barTotal = 12;
        var filled = Math.Clamp((int)Math.Round(healthPercent * barTotal), 0, barTotal);
        var healthBar = new string('█', filled) + new string('░', barTotal - filled);

        var hpColor = healthPercent > 0.5 ? "\u001b[1;32m" : healthPercent > 0.25 ? "\u001b[1;33m" : "\u001b[1;31m";

        sb.AppendLine("\u001b[1;36m┌────────────────────────────────────────────────────────────────────────┐\u001b[0m");
        sb.AppendLine($"\u001b[1;36m│\u001b[0m  \u001b[1;37m#{Id} {Name,-18}\u001b[0m HP: {hpColor}{healthBar}\u001b[0m \u001b[1;37m{Health}/{MaxHealth}\u001b[0m | PWR: \u001b[1;33m{Power,-3}\u001b[0m | RES: \u001b[1;34m{Resistance,-4:P0}\u001b[0m \u001b[1;36m│\u001b[0m");
        sb.AppendLine("\u001b[1;36m├────────────────────────────────────────────────────────────────────────┤\u001b[0m");

        sb.AppendLine("\u001b[1;36m│\u001b[0m  \u001b[1;33m🎒 Active Items:\u001b[0m                                                      \u001b[1;36m│\u001b[0m");
        if (ItemNames.Count == 0)
        {
            sb.AppendLine("\u001b[1;36m│\u001b[0m     \u001b[90m(None equipped)\u001b[0m                                                    \u001b[1;36m│\u001b[0m");
        }
        else
        {
            foreach (var (itemId, itemName) in ItemNames)
            {
                var entry = $"#{itemId}: {itemName}";
                sb.AppendLine($"\u001b[1;36m│\u001b[0m     \u001b[32m▸\u001b[0m \u001b[37m{entry,-64}\u001b[0m \u001b[1;36m│\u001b[0m");
            }
        }

        sb.AppendLine("\u001b[1;36m│\u001b[0m  \u001b[1;35m✨ Available Abilities:\u001b[0m                                                 \u001b[1;36m│\u001b[0m");
        if (AbilityNames.Count == 0)
        {
            sb.AppendLine("\u001b[1;36m│\u001b[0m     \u001b[90m(None learned)\u001b[0m                                                     \u001b[1;36m│\u001b[0m");
        }
        else
        {
            foreach (var (abId, abName) in AbilityNames)
            {
                var entry = $"#{abId}: {abName}";
                sb.AppendLine($"\u001b[1;36m│\u001b[0m     \u001b[35m▸\u001b[0m \u001b[37m{entry,-64}\u001b[0m \u001b[1;36m│\u001b[0m");
            }
        }

        sb.Append("\u001b[1;36m└────────────────────────────────────────────────────────────────────────┘\u001b[0m");
        return sb.ToString();
    }

    public void Render(IRenderer renderer)
    {
        renderer.RenderRaw(Render());
    }
}
