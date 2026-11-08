using Project.Core.Characters.Combat;
using Project.Core.Characters.Models;
using Project.Core.Characters.Services;
using Project.Core.Characters.TurnBased;
using Project.Core.Characters.ViewModels;
using Project.Infra.Remote.Models;
using Tests.Doubles;
using Xunit;

namespace Tests;

/// <summary>
/// Unit test suite verifying the Core Business Logic Layer (Practical Work 14):
/// - CombatCalculator physical and magic attack formulas
/// - Flat Armor mitigation vs Resistance and Resistance Penetration
/// - Status Effects (Buffs, Debuffs, DoT, Stun)
/// - TurnBasedBattleEngine speed-based action queues and timelines
/// - BattleHistory recording and tabular formatting
/// - Full turn-based battle simulation lifecycle
/// - CharacterProfileViewModel mapping
/// </summary>
public class BusinessLogicTests
{
    // =========================================================================
    // 1. Physical Combat Formula Tests
    // =========================================================================
    [Fact]
    public void PhysicalCombat_ArmorReducesDamageDirectly()
    {
        // Arrange: Attacker Power: 50, Target Armor: 20, Target Res: 0%
        var calc = new CombatCalculator();
        var attacker = new Character(1, "Attacker", 100, 50, 0.0, armor: 0);
        var defender = new Character(2, "Defender", 100, 10, 0.0, armor: 20);

        // Act: 50 - 20 = 30
        var dmg = calc.CalculatePhysicalDamage(attacker, defender);

        // Assert
        Assert.Equal(30, dmg);
    }

    [Fact]
    public void PhysicalCombat_ArmorAndResistanceCombineAccurately()
    {
        // Arrange: Attacker Power: 60, Target Armor: 20 -> 40. Target Res: 25% -> 40 * (1 - 0.25) = 30.
        var calc = new CombatCalculator();
        var attacker = new Character(1, "Warrior", 100, 60, 0.0);
        var defender = new Character(2, "Knight", 100, 10, 0.25, armor: 20);

        // Act
        var dmg = calc.CalculatePhysicalDamage(attacker, defender);

        // Assert
        Assert.Equal(30, dmg);
    }

    [Fact]
    public void PhysicalCombat_ResistancePenetrationBypassesDefense()
    {
        // Arrange: Attacker Power: 50, ResPen: 20%. Defender: Armor: 0, Res: 30% -> Net Res = 10%.
        // Dmg: 50 * (1 - 0.10) = 45.
        var calc = new CombatCalculator();
        var attacker = new Character(1, "Ranger", 100, 50, 0.0, resistPenetration: 0.20);
        var defender = new Character(2, "Golem", 100, 10, 0.30, armor: 0);

        // Act
        var dmg = calc.CalculatePhysicalDamage(attacker, defender);

        // Assert
        Assert.Equal(45, dmg);
    }

    [Fact]
    public void PhysicalCombat_CriticalStrike_AppliesOnePointSevenFiveMultiplier()
    {
        // Arrange: Attacker Power: 40 -> Critical: 40 * 1.75 = 70. Defender Armor: 10 -> 60.
        var calc = new CombatCalculator();
        var attacker = new Character(1, "Assassin", 100, 40, 0.0);
        var defender = new Character(2, "Target", 100, 10, 0.0, armor: 10);

        // Act
        var dmg = calc.CalculatePhysicalDamage(attacker, defender, isCritical: true);

        // Assert
        Assert.Equal(60, dmg);
    }

    // =========================================================================
    // 2. Magic Ability Combat Formula Tests
    // =========================================================================
    [Fact]
    public void MagicCombat_IgnoresFlatArmorAndUsesMathematicalFormula()
    {
        // Formula: dmg_magic = (dmg_flat * max(0, 1 - res_tgt + res_pen) + bonus_flat) * max(0.1, 1 + bonus_prof - debuff)
        // Ability: dmg_flat = 40, res_pen = 0.10, bonus_prof = 0.20
        // Defender: armor = 50 (must be ignored!), res = 0.30
        // Attacker items bonus = +10 Power
        // Calculations:
        // resistFactor = 1 - 0.30 + 0.10 = 0.80
        // (40 * 0.80) + 10 = 32 + 10 = 42
        // profMultiplier = 1 + 0.20 = 1.20
        // Final: 42 * 1.20 = 50.4 -> 50
        var calc = new CombatCalculator();
        var attacker = new Character(1, "Mage", 100, 20, 0.0);
        attacker.AddItem(new Item(1, "MagicStaff", hpMod: 0, powerMod: 10));

        var defender = new Character(2, "ArmoredTank", 100, 10, resistance: 0.30, armor: 50);

        var spell = new Ability(
            id: 1,
            name: "ArcaneBolt",
            damage: 40,
            heal: 0,
            description: "Concentrated arcane force",
            isMagic: true,
            resistPenetration: 0.10,
            bonusProficiency: 0.20
        );

        // Act
        var magicDmg = calc.CalculateMagicDamage(attacker, defender, spell);

        // Assert
        Assert.Equal(50, magicDmg);
    }

    // =========================================================================
    // 3. Status Effects Tests (Buff, Debuff, DoT, Stun)
    // =========================================================================
    [Fact]
    public void StatusEffects_BuffAndDoT_AppliesAndExpiresOverTurns()
    {
        // Arrange
        var hero = new Character(1, "Hero", 100, 20, 0.10);
        var buff = new StatusEffect("Might", StatusEffectType.Buff, durationTurns: 2, powerBonus: 15);
        var poison = new StatusEffect("Poison", StatusEffectType.DamageOverTime, durationTurns: 2, dotDamage: 10);

        // Act: Apply
        hero.ApplyStatusEffect(buff);
        hero.ApplyStatusEffect(poison);

        // Assert: Stat increased
        Assert.Equal(35, hero.Power); // 20 + 15
        Assert.Equal(2, hero.StatusEffects.Count);

        // Act: Turn 1 tick
        var dotRound1 = hero.TickStatusEffects();
        Assert.Equal(10, dotRound1);
        Assert.Equal(90, hero.Health); // 100 - 10
        Assert.Equal(1, hero.StatusEffects[0].DurationTurns);

        // Act: Turn 2 tick (expires)
        var dotRound2 = hero.TickStatusEffects();
        Assert.Equal(10, dotRound2);
        Assert.Equal(80, hero.Health);
        Assert.Empty(hero.StatusEffects); // All expired
        Assert.Equal(20, hero.Power); // Restored to baseline
    }

    [Fact]
    public void StatusEffects_Stun_SkipsTurnExecution()
    {
        // Arrange
        var engine = new TurnBasedBattleEngine();
        var hero = new Character(1, "Hero", 100, 30, 0.0);
        var boss = new Character(2, "Boss", 200, 40, 0.0);

        hero.ApplyStatusEffect(new StatusEffect("Paralyze", StatusEffectType.Stun, durationTurns: 1));
        Assert.True(hero.IsStunned);

        // Act
        var turn = engine.ExecuteTurn(1, hero, boss, TurnActionType.PhysicalAttack);

        // Assert
        Assert.Equal(TurnActionType.SkipStunned, turn.ActionType);
        Assert.Equal(0, turn.DamageDealt);
        Assert.Contains("STUNNED", turn.LogMessage);
        Assert.Equal(200, boss.Health); // Boss undamaged
    }

    // =========================================================================
    // 4. Turn Queue & Speed Advantage Tests
    // =========================================================================
    [Fact]
    public void TurnBasedEngine_DetermineTurnOrder_OrdersBySpeedAndGrantsFastestBonusTurn()
    {
        // Arrange: Fast Rogue (Speed 180) vs Slow Knight (Speed 100).
        // Since 180 >= 100 * 1.5, Rogue gains an extra action slot!
        var engine = new TurnBasedBattleEngine();
        var rogue = new Character(1, "Rogue", 80, 25, 0.10, speed: 180);
        var knight = new Character(2, "Knight", 120, 30, 0.20, speed: 100);

        // Act
        var order = engine.DetermineTurnOrder([knight, rogue]);

        // Assert
        Assert.Equal(3, order.Count); // [Rogue, Knight, Rogue]
        Assert.Equal("Rogue", order[0].Name);
        Assert.Equal("Knight", order[1].Name);
        Assert.Equal("Rogue", order[2].Name);
    }

    // =========================================================================
    // 5. Battle History Recording Tests
    // =========================================================================
    [Fact]
    public void BattleHistory_RecordsAndFormatsTurnTable()
    {
        // Arrange
        var engine = new TurnBasedBattleEngine();
        var c1 = new Character(1, "Aragorn", 100, 30, 0.10);
        var c2 = new Character(2, "Orc", 80, 20, 0.05);

        // Act
        engine.ExecuteTurn(1, c1, c2, TurnActionType.PhysicalAttack, isCritical: false);
        engine.ExecuteTurn(1, c2, c1, TurnActionType.PhysicalAttack, isCritical: true);

        // Assert
        Assert.Equal(2, engine.History.TotalTurns);
        var table = engine.History.FormatHistoryTable();
        Assert.Contains("Aragorn", table);
        Assert.Contains("Orc", table);
        Assert.Contains("PhysicalAttack", table);
        Assert.Contains("R1", table);
    }

    // =========================================================================
    // 6. Full Battle Simulation Lifecycle
    // =========================================================================
    [Fact]
    public void TurnBasedEngine_RunBattle_ExecutesToDecisiveConclusion()
    {
        // Arrange
        var engine = new TurnBasedBattleEngine();
        var gladiator = new Character(1, "Spartacus", 100, 40, 0.15, speed: 120);
        var beast = new Character(2, "Tiger", 60, 25, 0.05, speed: 90);

        // Act
        var result = engine.RunBattle(gladiator, beast);

        // Assert
        Assert.True(result.TotalRounds >= 1);
        Assert.True(result.TotalTurns >= 1);
        Assert.Equal(gladiator, result.Winner);
        Assert.Equal(beast, result.Loser);
        Assert.False(beast.IsAlive);
        Assert.True(gladiator.IsAlive);
    }

    // =========================================================================
    // 7. CharacterProfileViewModel Mapping Tests
    // =========================================================================
    [Fact]
    public void CharacterProfileViewModel_MapsAllDomainAndApiAttributes()
    {
        // Arrange
        var ch = new Character(10, "Raiden Shogun", 120, 45, 0.20, armor: 15, speed: 130);
        ch.AddItem(new Item(1, "Engulfing_Lightning", hpMod: 10, powerMod: 30));
        ch.AddAbility(new Ability(1, "Musou_no_Hitotachi", 60, 0, "Ultimate lightning strike"));

        var remoteData = new RemoteCharacterData
        {
            Name = "Raiden Shogun",
            Vision = "Electro",
            Weapon = "Polearm",
            Nation = "Inazuma",
            Rarity = 5
        };

        // Act
        var vm = new CharacterProfileViewModel(ch, remoteData);

        // Assert
        Assert.Equal("Raiden Shogun", vm.Name);
        Assert.Equal("Electro", vm.Element);
        Assert.Equal("Polearm", vm.WeaponType);
        Assert.Equal("Inazuma", vm.Nation);
        Assert.Equal("★★★★★", vm.RarityStars);
        Assert.Equal("https://genshin.jmp.blue/characters/raiden-shogun/icon", vm.AvatarUrl);
        Assert.Equal(15, vm.Armor);
        Assert.Equal(130, vm.Speed);
        Assert.Single(vm.EquippedItemNames);
        Assert.Single(vm.LearnedAbilityNames);
    }
}
