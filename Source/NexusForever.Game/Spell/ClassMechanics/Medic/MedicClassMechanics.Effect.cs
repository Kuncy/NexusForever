using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using System.Numerics;

namespace NexusForever.Game.Spell.ClassMechanics.Medic;

public sealed partial class MedicClassMechanics
{
    public bool TryHandleVitalModifier(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Medic } medic
            || target != medic
            || spell.Parameters.RootSpellInfo.BaseInfo.Entry.Id
                != MedicSpellIds.RechargeBase)
            return false;

        uint actuators = Math.Clamp(spell.Parameters.ClassResourceSnapshot, 1u, 4u);
        if ((Vital)info.Entry.DataBits00 == Vital.Focus)
        {
            if (info.Entry.OrderIndex != actuators)
                return true;
            float percent = BitConverter.UInt32BitsToSingle(info.Entry.DataBits05);
            medic.ModifyVital(Vital.Focus,
                medic.GetVitalMaximum(Vital.Focus) * percent);
            medic.ModifyVital(Vital.MedicCore, -(float)actuators);
        }
        // Suppress the table's fixed -4 row; the actual cost is dynamic.
        return true;
    }

    public bool TryHandleForcedMove(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Medic })
            return false;
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (baseId == MedicSpellIds.ExtricateBase)
        {
            Vector3 offset = target.Position - spell.Caster.Position;
            Vector3 destination = offset.LengthSquared() < 0.01f
                ? spell.Caster.Position
                : spell.Caster.Position + Vector3.Normalize(offset) * 2f;
            spell.ScheduleAction(info.Entry.DelayTime / 1000d,
                () => target.MovementManager.SetPosition(destination, false));
            return true;
        }
        if (baseId is not (MedicSpellIds.UrgencyBase or MedicSpellIds.RestrictorBase))
            return false;

        float distance = BitConverter.UInt32BitsToSingle(info.Entry.DataBits01);
        float yaw = -target.Rotation.X;
        Vector3 forward = new(MathF.Cos(yaw), 0f, MathF.Sin(yaw));
        float direction = baseId == MedicSpellIds.RestrictorBase ? -1f : 1f;
        spell.ScheduleAction(info.Entry.DelayTime / 1000d,
            () => target.MovementManager.SetPosition(
                target.Position + forward * distance * direction, false));
        return true;
    }

    public bool TryHandleProxy(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Medic } medic)
            return false;

        uint parentSpellId = spell.Parameters.SpellInfo.Entry.Id;
        uint proxySpellId = info.Entry.DataBits00;
        if (spell.Parameters.RootSpellInfo.BaseInfo.Entry.Id == 63410u
            && parentSpellId == spell.Parameters.RootSpellInfo.Entry.Id)
        {
            uint selectedOrder = spell.Parameters.ClassResourceSnapshot == 2u ? 2u : 0u;
            if (info.Entry.OrderIndex == selectedOrder)
                spell.CastProxySpell(proxySpellId, target,
                    info.Entry.DelayTime / 1000d);
            return true;
        }

        if (parentSpellId is MedicSpellIds.Discharge or MedicSpellIds.Emission
            && proxySpellId == MedicSpellIds.DischargePowerCharge)
        {
            spell.CastProxySpell(proxySpellId, target,
                parentSpellSuccessfulHit: spell.TryConsumeSuccessfulHit());
            return true;
        }

        if (parentSpellId == MedicSpellIds.DischargePowerCharge)
        {
            if (proxySpellId == MedicSpellIds.PowerChargeActuator
                && spell.Parameters.ParentSpellSuccessfulHit)
                MedicState.For(medic).AddPowerCharge(medic);
            return true;
        }

        return false;
    }

    public double GetEffectDelay(ISpell spell, ISpellTargetEffectInfo info)
        => spell.Parameters.RootSpellInfo.BaseInfo.Entry.Id == 26038u
            && info.Entry.OrderIndex <= 2u
            ? info.Entry.OrderIndex * 0.25d
            : 0d;

    public bool TryHandlePersonalDamageHealModifier(ISpell spell,
        IUnitEntity target)
    {
        if (spell.Caster is not IPlayer { Class: Class.Medic } medic
            || spell.Parameters.RootSpellInfo.BaseInfo.Entry.Id != 63087u)
            return false;
        MedicState.For(medic).MarkNerveInduction(target.Guid);
        return true;
    }
}
