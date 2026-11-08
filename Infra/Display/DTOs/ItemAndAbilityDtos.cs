using Project.CLI.Rendering;
using Project.Core.Characters.Models;

namespace Project.Infra.Display.DTOs;

/// <summary>
/// Data Transfer Object for Item presentation with ANSI styling.
/// </summary>
public class ItemDto(Item item, IEnumerable<string>? usableBy = null) : IDisplayable, IRenderable
{
    public int Id { get; init; } = item.Id;
    public string Name { get; init; } = item.Name;
    public double HpMod { get; init; } = item.HpMod;
    public double PowerMod { get; init; } = item.PowerMod;
    public List<string> UsableBy { get; init; } = usableBy?.ToList() ?? [];

    public string Render()
    {
        var users = UsableBy.Count > 0 ? string.Join(", ", UsableBy) : "Unassigned / All";
        var hpPart = HpMod >= 0 ? $"\u001b[1;32m+{HpMod} HP\u001b[0m" : $"\u001b[1;31m{HpMod} HP\u001b[0m";
        var pwrPart = PowerMod >= 0 ? $"\u001b[1;33m+{PowerMod} PWR\u001b[0m" : $"\u001b[1;31m{PowerMod} PWR\u001b[0m";

        return $"  \u001b[1;33m[Item #{Id}]\u001b[0m \u001b[1;37m{Name,-18}\u001b[0m ({hpPart}, {pwrPart}) \u001b[90m| Equipped by: {users}\u001b[0m";
    }

    public void Render(IRenderer renderer)
    {
        renderer.RenderLine(Render());
    }
}

/// <summary>
/// Data Transfer Object for Ability presentation with ANSI styling.
/// </summary>
public class AbilityDto(Ability ability, IEnumerable<string>? usableBy = null) : IDisplayable, IRenderable
{
    public int Id { get; init; } = ability.Id;
    public string Name { get; init; } = ability.Name;
    public double Damage { get; init; } = ability.Damage;
    public double Heal { get; init; } = ability.Heal;
    public string Description { get; init; } = ability.Description;
    public List<string> UsableBy { get; init; } = usableBy?.ToList() ?? [];

    public string Render()
    {
        var users = UsableBy.Count > 0 ? string.Join(", ", UsableBy) : "Unassigned / All";
        var effect = Damage > 0 && Heal > 0 ? $"\u001b[1;31m{Damage} DMG\u001b[0m / \u001b[1;32m+{Heal} HEAL\u001b[0m"
            : Damage > 0 ? $"\u001b[1;31m{Damage} DMG\u001b[0m"
            : $"\u001b[1;32m+{Heal} HEAL\u001b[0m";

        return $"  \u001b[1;35m[Ability #{Id}]\u001b[0m \u001b[1;37m{Name,-16}\u001b[0m ({effect}) - \"\u001b[3m{Description}\u001b[0m\" \u001b[90m| Usable: {users}\u001b[0m";
    }

    public void Render(IRenderer renderer)
    {
        renderer.RenderLine(Render());
    }
}
