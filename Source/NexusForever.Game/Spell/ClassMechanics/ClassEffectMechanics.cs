using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;

namespace NexusForever.Game.Spell.ClassMechanics;

/// <summary>
/// Entry point for the spell effect related <see cref="IClassMechanics"/> hooks.
/// </summary>
/// <remarks>
/// An effect has two sides that can react, the caster and the target, both are dispatched to and each
/// implementation decides which side it cares about. Hooks that are keyed on the spell rather than on a player
/// are offered to every implementation.
/// </remarks>
public static class ClassEffectMechanics
{
    public static bool TryHandleVitalModifier(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
    {
        return Any(spell, target, m => m.TryHandleVitalModifier(spell, target, info));
    }

    public static void HandleForcedMove(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
    {
        Any(spell, target, m => m.TryHandleForcedMove(spell, target, info));
    }

    public static bool TryHandleProc(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
    {
        return Any(spell, target, m => m.TryHandleProc(spell, target, info));
    }

    public static bool ShouldApplyDamage(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
    {
        return All(spell, target, m => m.ShouldApplyDamage(spell, target, info));
    }

    public static bool TryHandleProxy(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
    {
        return Any(spell, target, m => m.TryHandleProxy(spell, target, info));
    }

    /// <summary>
    /// Returns additional damage repeats for spells the client executes in a repeated phase.
    /// </summary>
    public static bool TryGetAdditionalDamageRepeat(ISpell spell, out uint count, out double interval)
    {
        foreach (IClassMechanics mechanics in ClassMechanicsRegistry.All)
            if (mechanics.TryGetAdditionalDamageRepeat(spell, out count, out interval))
                return true;

        count    = 0u;
        interval = 0d;
        return false;
    }

    public static void AfterCcStateApplied(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
    {
        ClassMechanicsRegistry.ForBoth(spell.Caster, target, m => m.AfterCcStateApplied(spell, target, info));
    }

    public static bool ShouldApplyProperty(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
    {
        return All(spell, target, m => m.ShouldApplyProperty(spell, target, info));
    }

    public static bool ShouldEvaluatePropertyPrerequisite(ISpell spell)
    {
        return ClassMechanicsRegistry.All.All(m => m.ShouldEvaluatePropertyPrerequisite(spell));
    }

    public static uint GetPropertyDuration(ISpell spell, uint duration)
    {
        foreach (IClassMechanics mechanics in ClassMechanicsRegistry.All)
        {
            uint classDuration = mechanics.GetPropertyDuration(spell, duration);
            if (classDuration != duration)
                return classDuration;
        }

        return duration;
    }

    public static void AfterPropertyApplied(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
    {
        ClassMechanicsRegistry.ForBoth(spell.Caster, target, m => m.AfterPropertyApplied(spell, target, info));
    }

    public static double GetEffectDelay(ISpell spell, ISpellTargetEffectInfo info)
    {
        foreach (IClassMechanics mechanics in ClassMechanicsRegistry.All)
        {
            double delay = mechanics.GetEffectDelay(spell, info);
            if (delay > 0d)
                return delay;
        }

        return 0d;
    }

    public static bool TryHandlePersonalDamageHealModifier(ISpell spell, IUnitEntity target)
    {
        return Any(spell, target, m => m.TryHandlePersonalDamageHealModifier(spell, target));
    }

    /// <summary>
    /// Returns whether the caster or the target class handled the effect.
    /// </summary>
    private static bool Any(ISpell spell, IUnitEntity target, Func<IClassMechanics, bool> handler)
    {
        IClassMechanics casterMechanics = ClassMechanicsRegistry.For(spell.Caster);
        if (casterMechanics != null && handler.Invoke(casterMechanics))
            return true;

        IClassMechanics targetMechanics = ClassMechanicsRegistry.For(target);
        return targetMechanics != null
            && targetMechanics != casterMechanics
            && handler.Invoke(targetMechanics);
    }

    /// <summary>
    /// Returns whether neither the caster nor the target class vetoed the effect.
    /// </summary>
    private static bool All(ISpell spell, IUnitEntity target, Func<IClassMechanics, bool> handler)
    {
        IClassMechanics casterMechanics = ClassMechanicsRegistry.For(spell.Caster);
        if (casterMechanics != null && !handler.Invoke(casterMechanics))
            return false;

        IClassMechanics targetMechanics = ClassMechanicsRegistry.For(target);
        return targetMechanics == null
            || targetMechanics == casterMechanics
            || handler.Invoke(targetMechanics);
    }
}
