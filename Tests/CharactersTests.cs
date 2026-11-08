using Project.Core.Characters.Models;
using Project.Core.Characters.Services;
using Tests.Doubles;
using Xunit;

namespace Tests;

/// <summary>
/// Unit test suite for the Core.Characters domain models and Game domain facade.
/// Adheres strictly to the AAA (Arrange, Act, Assert) testing pattern and Black-box testing approach.
/// </summary>
public class CharactersTests
{
    [Fact]
    public void Character_Stats_CalculateCorrectlyWithEquippedItems()
    {
        // ==========================================
        // Arrange: Setup character and equipment items
        // ==========================================
        var hero = new Character(1, "Hero", 100, 20, 0.2);
        var sword = new Item(1, "Sword", 10, 15);
        var ring = new Item(2, "Ring", 20, 5);

        // ==========================================
        // Act: Equip items onto the character
        // ==========================================
        hero.AddItem(sword);
        hero.AddItem(ring);

        // ==========================================
        // Assert: Verify aggregated stats match item bonuses
        // ==========================================
        Assert.Equal(130, hero.MaxHealth); // 100 base + 10 + 20
        Assert.Equal(40, hero.Power);      // 20 base + 15 + 5
        Assert.Equal(0.2, hero.Resistance);
    }

    [Fact]
    public void Combat_DamageMitigation_RespectsResistancePercentage()
    {
        // ==========================================
        // Arrange: Character with 50% damage resistance
        // ==========================================
        var defender = new Character(2, "Defender", 100, 10, 0.5);

        // ==========================================
        // Act: Apply 40 incoming raw damage
        // ==========================================
        var damageTaken = defender.TakeDamage(40);

        // ==========================================
        // Assert: 50% mitigated, exactly 20 damage applied
        // ==========================================
        Assert.Equal(20, damageTaken);
        Assert.Equal(80, defender.Health);
        Assert.True(defender.IsAlive);
    }

    [Fact]
    public void Combat_LethalDamage_TransitionsIsAliveToFalse()
    {
        // ==========================================
        // Arrange: Setup target with 30 health and 0 resistance
        // ==========================================
        var target = new Character(3, "Minion", 30, 5, 0.0);

        // ==========================================
        // Act: Apply fatal blow exceeding current health
        // ==========================================
        var damageTaken = target.TakeDamage(50);

        // ==========================================
        // Assert: Target health clamps to 0 and IsAlive becomes false
        // ==========================================
        Assert.Equal(30, damageTaken);
        Assert.Equal(0, target.Health);
        Assert.False(target.IsAlive);
    }

    [Fact]
    public void Healing_CannotExceedMaxHealth()
    {
        // ==========================================
        // Arrange: Damage hero down to 50 health
        // ==========================================
        var hero = new Character(1, "Hero", 100, 20, 0.0);
        hero.TakeDamage(50);

        // ==========================================
        // Act: Attempt to heal for 200 points
        // ==========================================
        var healed = hero.ReceiveHealing(200);

        // ==========================================
        // Assert: Healing is capped at missing health
        // ==========================================
        Assert.Equal(50, healed);
        Assert.Equal(100, hero.Health);
    }

    [Fact]
    public void Ability_DamageApplication_ReducesTargetHealth()
    {
        // ==========================================
        // Arrange: Setup caster, offensive ability and target
        // ==========================================
        var spell = new Ability(1, "Fireball", 40, 0, "Deals massive fire damage");
        var caster = new Character(1, "Mage", 80, 10, 0.05);
        var target = new Character(2, "Goblin", 60, 5, 0.0);
        caster.AddAbility(spell);

        // ==========================================
        // Act: Execute ability against the target
        // ==========================================
        var damageDealt = target.TakeDamage(spell.PowerModifier);

        // ==========================================
        // Assert: Target received full ability damage
        // ==========================================
        Assert.Equal(40, damageDealt);
        Assert.Equal(20, target.Health);
    }

    [Fact]
    public void GameFacade_ExecuteAttack_EmitsEventsToMockDisplayer()
    {
        // ==========================================
        // Arrange: Use hand-written MockDisplayer spy
        // ==========================================
        var mockDisplayer = new MockDisplayer();
        var game = new Game(mockDisplayer);

        var attacker = game.CreateCharacter("Attacker", 100, 120, 0.0);
        var victim = game.CreateCharacter("Victim", 50, 10, 0.0);

        // ==========================================
        // Act: Perform lethal attack
        // ==========================================
        game.Attack("Attacker", "Victim");

        // ==========================================
        // Assert: Target defeated and output verified on MockDisplayer
        // ==========================================
        Assert.False(victim.IsAlive);
        Assert.Equal(0, victim.Health);
        mockDisplayer.VerifyContains("was defeated");
        Assert.True(mockDisplayer.TotalCallCount >= 2);
    }

    [Fact]
    public void GameFacade_CreateAndEquip_UpdatesCharacterCatalog()
    {
        // ==========================================
        // Arrange: Setup game with FakeDisplayer
        // ==========================================
        var fakeDisplayer = new FakeDisplayer();
        var game = new Game(fakeDisplayer);

        // ==========================================
        // Act: Create item, ability and character in sequence
        // ==========================================
        var bow = game.CreateItem("Longbow", 5, 25);
        var shot = game.CreateAbility("PowerShot", 35, 0, "Sniper shot");
        var hero = game.CreateCharacter("Ranger", 90, 20, 0.1, [shot], [bow]);

        // ==========================================
        // Assert: Entity relations and stats match expectations
        // ==========================================
        Assert.Single(game.Characters);
        Assert.Single(game.Items);
        Assert.Single(game.Abilities);
        Assert.Equal(45, hero.Power); // 20 base + 25 bow
    }
}
