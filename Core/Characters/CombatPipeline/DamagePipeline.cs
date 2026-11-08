using Project.Core.Characters.Models;

namespace Project.Core.Characters.CombatPipeline;

/// <summary>
/// Execution payload passed through the Chain of Responsibility handlers.
/// </summary>
public class DamageContext(Character attacker, Character target, int rawDamage, bool isCritical = false)
{
    public Character Attacker { get; } = attacker;
    public Character Target { get; } = target;
    public int RawDamage { get; set; } = rawDamage;
    public int CurrentDamage { get; set; } = rawDamage;
    public bool IsCritical { get; set; } = isCritical;
    public int TotalMitigated { get; set; } = 0;
    public List<string> PipelineLog { get; } = [];
}

/// <summary>
/// [Behavioral Pattern: Chain of Responsibility - Handler]
/// Contract for sequentially processing and mitigating damage through a pipeline of handlers.
/// </summary>
public interface IDamageStepHandler
{
    IDamageStepHandler SetNext(IDamageStepHandler nextHandler);
    void Handle(DamageContext context);
}

/// <summary>
/// Base class implementing boilerplate next-handler chaining.
/// </summary>
public abstract class BaseDamageHandler : IDamageStepHandler
{
    private IDamageStepHandler? _next;

    public IDamageStepHandler SetNext(IDamageStepHandler nextHandler)
    {
        _next = nextHandler;
        return nextHandler;
    }

    public virtual void Handle(DamageContext context)
    {
        _next?.Handle(context);
    }
}

/// <summary>
/// Chain Step 1: Applies critical strike multiplier if triggered.
/// </summary>
public class CriticalMultiplierHandler : BaseDamageHandler
{
    public override void Handle(DamageContext context)
    {
        if (context.IsCritical)
        {
            var amplified = (int)Math.Round(context.CurrentDamage * 1.75);
            context.PipelineLog.Add($"Critical multiplier applied: {context.CurrentDamage} -> {amplified}");
            context.CurrentDamage = amplified;
        }

        base.Handle(context);
    }
}

/// <summary>
/// Chain Step 2: Mitigates damage according to target's resistance percentage.
/// </summary>
public class ResistanceMitigationHandler : BaseDamageHandler
{
    public override void Handle(DamageContext context)
    {
        var res = context.Target.Resistance;
        if (res > 0.0)
        {
            var mitigated = (int)Math.Round(context.CurrentDamage * res);
            context.CurrentDamage = Math.Max(0, context.CurrentDamage - mitigated);
            context.TotalMitigated += mitigated;
            context.PipelineLog.Add($"Resistance ({(int)(res * 100)}%) absorbed {mitigated} damage. Remaining: {context.CurrentDamage}");
        }

        base.Handle(context);
    }
}

/// <summary>
/// Chain Step 3: Ensures lethal clamp rules and minimum damage bound.
/// </summary>
public class MinimumDamageClampHandler : BaseDamageHandler
{
    public override void Handle(DamageContext context)
    {
        if (context.CurrentDamage < 1 && context.Target.IsAlive)
        {
            context.CurrentDamage = 1;
            context.PipelineLog.Add("Clamped to minimum 1 damage.");
        }

        base.Handle(context);
    }
}
