using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Engineer;

public static class EngineerEffectMechanics
{
    public static bool TryHandleVitalModifier(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Parameters.SpellInfo.Entry.Id != EngineerSpellIds.PulseBlastVolatility
            || target is not IPlayer { Class: Class.Engineer }
            || (Vital)info.Entry.DataBits00 != Vital.Resource1)
            return false;

        target.ModifyVital(Vital.Volatility, info.Entry.DataBits01);
        return true;
    }

    public static bool TryHandleProxy(ISpell spell, IUnitEntity target,
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
}
