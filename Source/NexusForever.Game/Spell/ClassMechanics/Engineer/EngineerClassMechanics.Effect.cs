using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using System.Numerics;

namespace NexusForever.Game.Spell.ClassMechanics.Engineer;

public sealed partial class EngineerClassMechanics
{
    public bool TryHandleForcedMove(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Engineer }
            )
            return false;

        if (spell.Parameters.RootSpellInfo.BaseInfo.Entry.Id == 63071u)
        {
            Vector3 center = spell.Parameters.Position?.Vector ?? spell.Caster.Position;
            Vector3 offset = target.Position - center;
            Vector3 destination = offset.LengthSquared() < 0.01f
                ? center
                : center + Vector3.Normalize(offset) * 2f;
            target.MovementManager.SetPosition(destination, false);
            return true;
        }
        if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id
            != EngineerSpellIds.UrgentWithdrawalBase)
            return false;

        float distance = BitConverter.UInt32BitsToSingle(info.Entry.DataBits01);
        float yaw = -target.Rotation.X;
        Vector3 forward = new(MathF.Cos(yaw), 0f, MathF.Sin(yaw));
        spell.ScheduleAction(info.Entry.DelayTime / 1000d,
            () => target.MovementManager.SetPosition(target.Position - forward * distance, false));
        return true;
    }

    public bool TryHandleVitalModifier(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Parameters.SpellInfo.Entry.Id != EngineerSpellIds.PulseBlastVolatility
            || target is not IPlayer { Class: Class.Engineer }
            || (Vital)info.Entry.DataBits00 != Vital.Resource1)
            return false;

        target.ModifyVital(Vital.Volatility, info.Entry.DataBits01);
        return true;
    }

    public bool TryHandleProxy(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Engineer })
            return false;

        uint parentSpellId = spell.Parameters.SpellInfo.Entry.Id;
        uint proxySpellId = info.Entry.DataBits00;
        if (parentSpellId == EngineerSpellIds.PulseBlast
            && proxySpellId == EngineerSpellIds.PulseBlastImpact)
        {
            spell.CastProxySpell(proxySpellId, target);
            spell.CastProxySpell(EngineerSpellIds.PulseBlastVolatility, spell.Caster);
            return true;
        }

        if (parentSpellId == EngineerSpellIds.ModeEradicate
            && proxySpellId == 0u
            && info.Entry.DataBits01 == EngineerSpellIds.ModeEradicateVolatility
            && info.Entry.TickTime > 0u)
        {
            uint tickCount = info.Entry.DurationTime / info.Entry.TickTime;
            double tickDuration = info.Entry.TickTime / 1000d;
            for (uint tick = 1u; tick <= tickCount; tick++)
                spell.CastProxySpell(info.Entry.DataBits01, spell.Caster, tick * tickDuration);
            return true;
        }

        return false;
    }

    public bool TryGetAdditionalDamageRepeat(ISpell spell, out uint count, out double interval)
    {
        count    = spell.Parameters.RootSpellInfo.BaseInfo.Entry.Id == EngineerSpellIds.BoltCasterBase
            ? 4u
            : 0u;
        interval = 0.08d;
        return count > 0u;
    }
}
