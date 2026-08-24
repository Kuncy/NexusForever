using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell.ClassMechanics.Engineer;
using NexusForever.Game.Spell.ClassMechanics.Esper;
using NexusForever.Game.Spell.ClassMechanics.Medic;
using NexusForever.Game.Spell.ClassMechanics.Spellslinger;
using NexusForever.Game.Spell.ClassMechanics.Stalker;
using NexusForever.Game.Spell.ClassMechanics.Warrior;

namespace NexusForever.Game.Spell.ClassMechanics;

public static class ClassEffectMechanics
{
    public static bool TryHandleVitalModifier(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
        => EngineerEffectMechanics.TryHandleVitalModifier(spell, target, info)
            || MedicEffectMechanics.TryHandleVitalModifier(spell, target, info)
            || StalkerEffectMechanics.TryHandleVitalModifier(spell, target, info)
            || WarriorEffectMechanics.TryHandleVitalModifier(spell, target, info);

    public static void HandleForcedMove(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (EngineerEffectMechanics.TryHandleForcedMove(spell, target, info))
            return;
        if (EsperEffectMechanics.TryHandleForcedMove(spell, target, info))
            return;
        if (MedicEffectMechanics.TryHandleForcedMove(spell, target, info))
            return;
        if (WarriorEffectMechanics.TryHandleForcedMove(spell, target, info))
            return;
        if (SpellslingerEffectMechanics.TryHandleForcedMove(spell, target, info))
            return;
        StalkerEffectMechanics.TryHandleForcedMove(spell, target, info);
    }

    public static bool TryHandleProc(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
        => WarriorEffectMechanics.TryHandleProc(spell, target, info)
            || StalkerEffectMechanics.TryHandleProc(spell, target, info);

    public static bool ShouldApplyDamage(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
        => EsperEffectMechanics.ShouldApplyDamage(spell, target, info)
            && SpellslingerEffectMechanics.ShouldApplyDamage(spell, target, info)
            && StalkerEffectMechanics.ShouldApplyDamage(spell, target, info);

    public static bool TryHandleProxy(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
        => WarriorEffectMechanics.TryHandleProxy(spell, target, info)
            || EngineerEffectMechanics.TryHandleProxy(spell, target, info)
            || EsperEffectMechanics.TryHandleProxy(spell, target, info)
            || MedicEffectMechanics.TryHandleProxy(spell, target, info)
            || StalkerEffectMechanics.TryHandleProxy(spell, target, info)
            || SpellslingerEffectMechanics.TryHandleProxy(spell, target, info);

    public static uint GetAdditionalDamageRepeatCount(ISpell spell)
    {
        if (WarriorEffectMechanics.IsMenacingStrike(spell))
            return 1u;
        uint repeats = EngineerEffectMechanics.GetAdditionalDamageRepeatCount(spell);
        if (repeats > 0u)
            return repeats;
        repeats = EsperEffectMechanics.GetAdditionalDamageRepeatCount(spell);
        return repeats > 0u ? repeats
            : StalkerEffectMechanics.GetAdditionalDamageRepeatCount(spell);
    }

    public static double GetAdditionalDamageRepeatInterval(ISpell spell)
    {
        if (WarriorEffectMechanics.IsMenacingStrike(spell))
            return 0.25d;
        if (EngineerEffectMechanics.GetAdditionalDamageRepeatCount(spell) > 0u)
            return EngineerEffectMechanics.GetAdditionalDamageRepeatInterval(spell);
        if (EsperEffectMechanics.GetAdditionalDamageRepeatCount(spell) > 0u)
            return EsperEffectMechanics.GetAdditionalDamageRepeatInterval(spell);
        return StalkerEffectMechanics.GetAdditionalDamageRepeatInterval(spell);
    }

    public static void AfterCcStateApplied(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
        => StalkerEffectMechanics.AfterCcStateApplied(spell, target, info);

    public static bool ShouldApplyProperty(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
        => WarriorEffectMechanics.ShouldApplyProperty(spell, target, info);

    public static bool ShouldEvaluatePropertyPrerequisite(ISpell spell)
        => WarriorEffectMechanics.ShouldEvaluatePropertyPrerequisite(spell);

    public static uint GetPropertyDuration(ISpell spell, uint duration)
        => WarriorEffectMechanics.GetPropertyDuration(spell, duration);

    public static void AfterPropertyApplied(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        SpellslingerEffectMechanics.AfterPropertyApplied(spell, target);
        StalkerEffectMechanics.AfterPropertyApplied(spell, target, info);
    }

    public static double GetEffectDelay(ISpell spell, ISpellTargetEffectInfo info)
        => MedicEffectMechanics.GetEffectDelay(spell, info);

    public static bool TryHandlePersonalDamageHealModifier(ISpell spell,
        IUnitEntity target)
        => MedicEffectMechanics.TryHandlePersonalDamageHealModifier(spell, target);
}
