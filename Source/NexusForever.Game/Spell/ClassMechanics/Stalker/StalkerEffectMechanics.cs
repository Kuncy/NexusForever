using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Spell.ClassMechanics.Stalker;

public static class StalkerEffectMechanics
{
    public static bool TryHandleVitalModifier(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Stalker } stalker
            || target != stalker
            || spell.Parameters.SpellInfo.BaseInfo.Entry.Id != StalkerSpellIds.PunishBase
            || (Vital)info.Entry.DataBits00 != Vital.Resource3)
            return false;

        if (stalker.GetVitalValue(Vital.Resource3) < 35f)
            stalker.ModifyVital(Vital.Resource3, info.Entry.DataBits01);
        return true;
    }

    public static bool TryHandleForcedMove(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Stalker } stalker)
            return false;

        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (baseId is StalkerSpellIds.FalseRetreatBase or StalkerSpellIds.TacticalRetreatBase)
        {
            float distance = BitConverter.UInt32BitsToSingle(info.Entry.DataBits02);
            float yaw = -target.Rotation.X;
            Vector3 forward = new(MathF.Cos(yaw), 0f, MathF.Sin(yaw));
            spell.ScheduleAction(info.Entry.DelayTime / 1000d,
                () => target.MovementManager.SetPosition(target.Position - forward * distance, false));
            return true;
        }

        if (spell.Parameters.SpellInfo.Entry.Id == StalkerSpellIds.PounceMove)
        {
            IUnitEntity primaryTarget = stalker.Map.GetEntity<IUnitEntity>(spell.Parameters.PrimaryTargetId);
            if (primaryTarget == null)
                return true;
            Vector3 offset = stalker.Position - primaryTarget.Position;
            if (offset.LengthSquared() < 0.01f)
                offset = Vector3.UnitX;
            Vector3 destination = primaryTarget.Position + Vector3.Normalize(offset) * 2f;
            stalker.MovementManager.SetPosition(destination, false);
            return true;
        }

        return false;
    }

    public static bool TryHandleProc(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Stalker } stalker
            || spell.Parameters.SpellInfo.Entry.Id != StalkerSpellIds.AnalyzeWeaknessMark)
            return false;

        StalkerState.For(stalker).MarkAnalyzeWeaknessTarget(target.Guid);
        return true;
    }

    public static bool ShouldApplyDamage(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Stalker } stalker)
            return true;

        StalkerState state = StalkerState.For(stalker);
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (baseId == StalkerSpellIds.NeutralizeBase)
        {
            bool matchingRow = state.StealthActive
                ? info.Entry.OrderIndex == 1u
                : info.Entry.OrderIndex == 0u;
            if (!matchingRow)
                info.DropEffect = true;
            return matchingRow;
        }

        if (baseId == StalkerSpellIds.RuinBase
            && info.Entry.OrderIndex is 0u or 2u)
        {
            bool matchingRow = state.StealthActive
                ? info.Entry.OrderIndex == 2u
                : info.Entry.OrderIndex == 0u;
            if (!matchingRow)
                info.DropEffect = true;
            return matchingRow;
        }

        return true;
    }

    public static bool TryHandleProxy(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Stalker })
            return false;

        uint parentSpellId = spell.Parameters.SpellInfo.Entry.Id;
        uint proxySpellId = info.Entry.DataBits00;
        if ((parentSpellId, proxySpellId) is
            (StalkerSpellIds.Shred, StalkerSpellIds.ShredImpact) or
            (StalkerSpellIds.ShredStealth, StalkerSpellIds.ShredStealthImpact))
        {
            spell.CastProxySpell(proxySpellId, target);
            spell.CastProxySpell(proxySpellId, target, 0.14d);
            spell.CastProxySpell(proxySpellId, target, 0.28d);
            return true;
        }

        if (parentSpellId == StalkerSpellIds.Impale)
        {
            bool bonusDamage = StalkerState.For((IPlayer)spell.Caster).StealthActive
                || IsBehind(spell.Caster, target);
            uint selectedImpact = bonusDamage ? 39427u : StalkerSpellIds.ImpaleNormalImpact;
            if (proxySpellId == selectedImpact
                && (selectedImpact == StalkerSpellIds.ImpaleNormalImpact
                    || info.Entry.OrderIndex == 1u))
                spell.CastProxySpell(selectedImpact, target);
            return true;
        }

        if (parentSpellId == StalkerSpellIds.Stagger
            && proxySpellId == StalkerSpellIds.StaggerImpact)
        {
            spell.CastProxySpell(proxySpellId, target, info.Entry.OrderIndex * 0.12d);
            return true;
        }

        if (parentSpellId == StalkerSpellIds.Whiplash
            && proxySpellId is StalkerSpellIds.WhiplashFirstImpact
                or StalkerSpellIds.WhiplashSecondImpact)
        {
            double delay = proxySpellId == StalkerSpellIds.WhiplashFirstImpact ? 0d : 0.25d;
            spell.CastProxySpell(proxySpellId, target, delay);
            return true;
        }

        if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id == StalkerSpellIds.NanoFieldBase
            && proxySpellId == 0u
            && info.Entry.DataBits01 == StalkerSpellIds.NanoFieldPulse
            && info.Entry.TickTime > 0u)
        {
            uint tickCount = info.Entry.DurationTime / info.Entry.TickTime;
            StalkerState state = StalkerState.For((IPlayer)spell.Caster);
            if (state.NanoFieldActive)
                spell.CastProxySpell(StalkerSpellIds.NanoFieldPulse, spell.Caster);
            for (uint tick = 1u; tick < tickCount; tick++)
            {
                spell.ScheduleAction(tick, () =>
                {
                    if (state.NanoFieldActive)
                        spell.CastProxySpell(StalkerSpellIds.NanoFieldPulse, spell.Caster);
                });
            }
            return true;
        }

        return false;
    }

    public static uint GetAdditionalDamageRepeatCount(ISpell spell)
    {
        return spell.Parameters.SpellInfo.BaseInfo.Entry.Id switch
        {
            StalkerSpellIds.RazorStormBase => 2u,
            StalkerSpellIds.ConcussiveKicksBase => 1u,
            _ => 0u
        };
    }

    public static double GetAdditionalDamageRepeatInterval(ISpell spell)
        => spell.Parameters.SpellInfo.BaseInfo.Entry.Id == StalkerSpellIds.RazorStormBase
            ? 0.18d
            : 0.22d;

    public static void AfterCcStateApplied(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Stalker }
            || spell.Parameters.SpellInfo.BaseInfo.Entry.Id != StalkerSpellIds.CollapseBase
            || info.Entry.DataBits00 != 18u)
            return;

        Vector3 offset = target.Position - spell.Caster.Position;
        if (offset.LengthSquared() < 0.01f)
            return;
        Vector3 destination = spell.Caster.Position + Vector3.Normalize(offset) * 2f;
        target.MovementManager.SetPosition(destination, false);
    }

    public static void AfterPropertyApplied(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id != StalkerSpellIds.AmplificationSpikeBase
            || info.Entry.OrderIndex != 0u
            || target is not IPlayer { Class: Class.Stalker })
            return;

        target.ModifyHealth(Math.Max(1u, target.MaxHealth / 5u), DamageType.Heal, spell.Caster);
    }

    private static bool IsBehind(IUnitEntity attacker, IUnitEntity target)
    {
        Vector3 targetToAttacker = attacker.Position - target.Position;
        if (targetToAttacker.LengthSquared() < 0.01f)
            return false;
        targetToAttacker = Vector3.Normalize(targetToAttacker);
        float yaw = -target.Rotation.X;
        Vector3 targetForward = new(MathF.Cos(yaw), 0f, MathF.Sin(yaw));
        return Vector3.Dot(targetForward, targetToAttacker) < -0.35f;
    }
}
