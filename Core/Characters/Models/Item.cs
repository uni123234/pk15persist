namespace Project.Core.Characters.Models;

/// <summary>
/// Domain model for an equipment item that modifies character attributes (Health, Power, Armor, Speed).
/// </summary>
public class Item
{
    public int Id { get; }
    public string Name { get; }
    public double HpMod { get; }
    public double PowerMod { get; }
    public double ArmorMod { get; }
    public double SpeedMod { get; }

    public Item(int id, string name, double hpMod, double powerMod, double armorMod = 0, double speedMod = 0)
    {
        Id = id;
        Name = name;
        HpMod = hpMod;
        PowerMod = powerMod;
        ArmorMod = armorMod;
        SpeedMod = speedMod;
    }
}
