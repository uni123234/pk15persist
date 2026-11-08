using Project.Core.Characters.CombatPipeline;
using Project.Core.Characters.Mediator;
using Project.Core.Characters.Memento;
using Project.Core.Characters.Models;
using Project.Core.Characters.Observer;
using Project.Core.Characters.Services;
using Project.Core.Characters.Strategies;
using Tests.Doubles;
using Xunit;

namespace Tests;

/// <summary>
/// Unit test suite verifying the implementation and integration of the 5 Behavioral Design Patterns:
/// 1. Mediator (BattleMediator)
/// 2. Observer (CombatLogObserver, CombatStatsObserver)
/// 3. Strategy (StandardAttackStrategy, CriticalStrikeStrategy, ArmorPiercingStrategy)
/// 4. Chain of Responsibility (DamagePipeline handlers)
/// 5. Memento (CharacterMemento, CharacterStateCaretaker)
/// </summary>
public class BehavioralPatternsTests
{
    // =========================================================================
    // 1. Observer Pattern Tests
    // =========================================================================
    [Fact]
    public void Observer_CombatStatsObserver_TracksDamageAndKillsAccurately()
    {
        // Arrange
        var statsObserver = new CombatStatsObserver();
        var mediator = new BattleMediator();
        mediator.SubscribeObserver(statsObserver);

        var attacker = new Character(1, "Warrior", 100, 40, 0.0);
        var defender = new Character(2, "Goblin", 30, 10, 0.0);

        // Act
        mediator.ExecuteAttack(attacker, defender);

        // Assert
        Assert.True(statsObserver.DamageDealt.ContainsKey("Warrior"));
        Assert.Equal(40, statsObserver.DamageDealt["Warrior"]);
        Assert.True(statsObserver.DamageTaken.ContainsKey("Goblin"));
        Assert.Equal(40, statsObserver.DamageTaken["Goblin"]);
        Assert.Equal(1, statsObserver.Kills["Warrior"]);
    }

    [Fact]
    public void Observer_Unsubscribe_StopsReceivingEvents()
    {
        // Arrange
        var statsObserver = new CombatStatsObserver();
        var mediator = new BattleMediator();
        mediator.SubscribeObserver(statsObserver);

        var attacker = new Character(1, "Hero", 100, 20, 0.0);
        var defender = new Character(2, "Dummy", 100, 5, 0.0);

        mediator.ExecuteAttack(attacker, defender);
        var initialDamage = statsObserver.DamageDealt["Hero"];

        // Act
        mediator.UnsubscribeObserver(statsObserver);
        mediator.ExecuteAttack(attacker, defender);

        // Assert
        Assert.Equal(initialDamage, statsObserver.DamageDealt["Hero"]);
    }

    [Fact]
    public void Observer_CombatLogObserver_FormatsEventsToDisplayer()
    {
        // Arrange
        var fakeDisplayer = new FakeDisplayer();
        var logObserver = new CombatLogObserver(fakeDisplayer);

        // Act
        logObserver.OnDamageDealt(new DamageDealtEvent("Ranger", "Orc", 35, 65, true));
        logObserver.OnHealingReceived(new HealingReceivedEvent("Ranger", 20, 85, 100));
        logObserver.OnCharacterDefeated(new CharacterDefeatedEvent("Ranger", "Orc"));

        // Assert
        Assert.True(fakeDisplayer.ContainsText("Ranger attacked Orc for 35 damage"));
        Assert.True(fakeDisplayer.ContainsText("CRITICAL STRIKE"));
        Assert.True(fakeDisplayer.ContainsText("received 20 healing"));
        Assert.True(fakeDisplayer.ContainsText("Orc was defeated by Ranger"));
    }

    // =========================================================================
    // 2. Chain of Responsibility Pattern Tests
    // =========================================================================
    [Fact]
    public void ChainOfResponsibility_CriticalMultiplierHandler_AmplifiesDamage()
    {
        // Arrange
        var handler = new CriticalMultiplierHandler();
        var attacker = new Character(1, "Attacker", 100, 100, 0.0);
        var target = new Character(2, "Target", 100, 10, 0.0);
        var context = new DamageContext(attacker, target, 100, isCritical: true);

        // Act
        handler.Handle(context);

        // Assert
        Assert.Equal(175, context.CurrentDamage);
        Assert.Contains(context.PipelineLog, l => l.Contains("Critical multiplier"));
    }

    [Fact]
    public void ChainOfResponsibility_ResistanceMitigationHandler_AbsorbsProportionalDamage()
    {
        // Arrange
        var handler = new ResistanceMitigationHandler();
        var attacker = new Character(1, "Attacker", 100, 100, 0.0);
        var target = new Character(2, "ArmoredTarget", 100, 10, 0.30); // 30% resistance
        var context = new DamageContext(attacker, target, 100, isCritical: false);

        // Act
        handler.Handle(context);

        // Assert
        Assert.Equal(70, context.CurrentDamage);
        Assert.Equal(30, context.TotalMitigated);
    }

    [Fact]
    public void ChainOfResponsibility_MinimumDamageClampHandler_EnforcesMinimumOneDamage()
    {
        // Arrange
        var handler = new MinimumDamageClampHandler();
        var attacker = new Character(1, "Attacker", 100, 0, 0.0);
        var target = new Character(2, "Target", 100, 10, 0.0);
        var context = new DamageContext(attacker, target, 0, isCritical: false);

        // Act
        handler.Handle(context);

        // Assert
        Assert.Equal(1, context.CurrentDamage);
    }

    [Fact]
    public void ChainOfResponsibility_FullPipeline_ProcessesInProperSequence()
    {
        // Arrange
        var crit = new CriticalMultiplierHandler();
        var resist = new ResistanceMitigationHandler();
        var clamp = new MinimumDamageClampHandler();
        crit.SetNext(resist).SetNext(clamp);

        var attacker = new Character(1, "Attacker", 100, 40, 0.0);
        var target = new Character(2, "Target", 100, 10, 0.50); // 50% resistance
        var context = new DamageContext(attacker, target, 40, isCritical: true);

        // Act
        crit.Handle(context);

        // Assert: 40 * 1.75 = 70. 70 * (1 - 0.50) = 35.
        Assert.Equal(35, context.CurrentDamage);
        Assert.Equal(35, context.TotalMitigated);
        Assert.Equal(2, context.PipelineLog.Count);
    }

    // =========================================================================
    // 3. Strategy Pattern Tests
    // =========================================================================
    [Fact]
    public void Strategy_StandardAttack_ConstructsStandardDamageContext()
    {
        // Arrange
        var strategy = new StandardAttackStrategy();
        var attacker = new Character(1, "Attacker", 100, 50, 0.0);
        var target = new Character(2, "Target", 100, 10, 0.0);

        // Act
        var context = strategy.PrepareAttackContext(attacker, target);

        // Assert
        Assert.Equal(50, context.RawDamage);
        Assert.False(context.IsCritical);
        Assert.Equal("Standard Physical Strike", strategy.StrategyName);
    }

    [Fact]
    public void Strategy_CriticalStrike_ConstructsCriticalContext()
    {
        // Arrange
        var strategy = new CriticalStrikeStrategy();
        var attacker = new Character(1, "Attacker", 100, 50, 0.0);
        var target = new Character(2, "Target", 100, 10, 0.0);

        // Act
        var context = strategy.PrepareAttackContext(attacker, target);

        // Assert
        Assert.Equal(50, context.RawDamage);
        Assert.True(context.IsCritical);
        Assert.Equal("Focused Critical Strike", strategy.StrategyName);
    }

    [Fact]
    public void Strategy_ArmorPiercing_AmplifiesRawBasePower()
    {
        // Arrange
        var strategy = new ArmorPiercingStrategy();
        var attacker = new Character(1, "Attacker", 100, 50, 0.0);
        var target = new Character(2, "Target", 100, 10, 0.0);

        // Act
        var context = strategy.PrepareAttackContext(attacker, target);

        // Assert
        Assert.Equal(60, context.RawDamage); // 50 * 1.2 = 60
        Assert.False(context.IsCritical);
    }

    // =========================================================================
    // 4. Mediator Pattern Tests
    // =========================================================================
    [Fact]
    public void Mediator_BattleMediator_OrchestratesFullCombatRound()
    {
        // Arrange
        var fakeDisplayer = new FakeDisplayer();
        var mediator = new BattleMediator();
        var logObserver = new CombatLogObserver(fakeDisplayer);
        var statsObserver = new CombatStatsObserver();
        mediator.SubscribeObserver(logObserver);
        mediator.SubscribeObserver(statsObserver);

        var knight = new Character(1, "Knight", 120, 30, 0.1);
        var bandit = new Character(2, "Bandit", 80, 20, 0.0);
        mediator.RegisterCharacter(knight);
        mediator.RegisterCharacter(bandit);

        // Act
        mediator.ExecuteAttack(knight, bandit, new CriticalStrikeStrategy());

        // Assert
        // Knight Power: 30 -> Critical multiplier: 30 * 1.75 = 53 -> Bandit resistance 0 -> 53 damage.
        // Bandit HP: 80 - 53 = 27.
        Assert.Equal(27, bandit.Health);
        Assert.Equal(53, statsObserver.DamageDealt["Knight"]);
        Assert.True(fakeDisplayer.ContainsText("CRITICAL STRIKE"));
    }

    [Fact]
    public void Mediator_BattleMediator_ResolvesOffensiveAndHealingAbilities()
    {
        // Arrange
        var fakeDisplayer = new FakeDisplayer();
        var mediator = new BattleMediator();
        mediator.SubscribeObserver(new CombatLogObserver(fakeDisplayer));

        var mage = new Character(1, "Mage", 80, 20, 0.0);
        var target = new Character(2, "Target", 100, 10, 0.0);
        var fireball = new Ability(1, "Fireball", 30, 0, "Explosive fire");
        var healSpell = new Ability(2, "HolyLight", 0, 25, "Restorative light");

        // Act
        mediator.ExecuteAbility(mage, target, fireball);
        var hpAfterFireball = target.Health;

        mediator.ExecuteAbility(mage, target, healSpell);
        var hpAfterHeal = target.Health;

        // Assert
        // Fireball deals mage.Power (20) + 30 = 50 damage -> HP: 100 - 50 = 50.
        Assert.Equal(50, hpAfterFireball);
        // HolyLight heals 25 HP -> HP: 50 + 25 = 75.
        Assert.Equal(75, hpAfterHeal);
        Assert.True(fakeDisplayer.ContainsText("Fireball"));
        Assert.True(fakeDisplayer.ContainsText("HolyLight"));
    }

    // =========================================================================
    // 5. Memento Pattern Tests
    // =========================================================================
    [Fact]
    public void Memento_CaretakerUndo_RestoresHealthAfterDamage()
    {
        // Arrange
        var caretaker = new CharacterStateCaretaker();
        var warrior = new Character(1, "Warrior", 100, 25, 0.0);

        // Act: Save snapshot before combat
        caretaker.PushSnapshot(warrior);
        warrior.TakeDamage(60);
        Assert.Equal(40, warrior.Health);

        // Act: Undo combat damage
        var restored = caretaker.Undo(warrior);

        // Assert
        Assert.True(restored);
        Assert.Equal(100, warrior.Health);
    }

    [Fact]
    public void Memento_NamedCheckpoints_AllowsSpecificStateRollback()
    {
        // Arrange
        var caretaker = new CharacterStateCaretaker();
        var hero = new Character(1, "Hero", 150, 30, 0.1);
        var sword = new Item(1, "Excalibur", 0, 50);

        // Act: Checkpoint 1 - Baseline
        caretaker.SaveCheckpoint("Checkpoint1", hero);

        // Act: Equip sword and modify stats
        hero.AddItem(sword);
        hero.TakeDamage(50);
        caretaker.SaveCheckpoint("Checkpoint2", hero);

        // Act: Deal fatal damage
        hero.TakeDamage(200);
        Assert.Equal(0, hero.Health);
        Assert.False(hero.IsAlive);

        // Act: Restore to Checkpoint 2
        var restoredToCp2 = caretaker.RestoreCheckpoint("Checkpoint2", hero);

        // Assert
        Assert.True(restoredToCp2);
        Assert.Equal(100, hero.Health);
        Assert.True(hero.IsAlive);
        Assert.Single(hero.ActiveItems);

        // Act: Restore to Checkpoint 1
        var restoredToCp1 = caretaker.RestoreCheckpoint("Checkpoint1", hero);
        Assert.True(restoredToCp1);
        Assert.Equal(150, hero.Health);
        Assert.Empty(hero.ActiveItems);
    }

    // =========================================================================
    // 6. Game Facade End-to-End Integration
    // =========================================================================
    [Fact]
    public void GameFacade_IntegratesAllBehavioralPatternsCohesively()
    {
        // Arrange
        var fakeDisplayer = new FakeDisplayer();
        var game = new Game(fakeDisplayer);

        var paladin = game.CreateCharacter("Paladin", 100, 30, 0.2);
        var rogue = game.CreateCharacter("Rogue", 80, 25, 0.1);

        // Act: Create snapshot using Memento facade
        game.CreateSnapshot("Paladin", "PreDuel");

        // Act: Perform Attack using Strategy through Facade -> Mediator -> Chain -> Observer
        game.Attack("Paladin", "Rogue", new CriticalStrikeStrategy());

        // Assert: Damage occurred and recorded by StatsObserver
        Assert.True(rogue.Health < 80);
        Assert.True(game.StatsObserver.DamageDealt["Paladin"] > 0);

        // Act: Restore state from Memento
        var restored = game.RestoreSnapshot("Paladin", "PreDuel");
        Assert.True(restored);
        Assert.Equal(100, paladin.Health);
    }
}
