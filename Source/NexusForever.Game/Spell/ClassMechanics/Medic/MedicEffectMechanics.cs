using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Medic;

public static class MedicEffectMechanics
{
    public static bool TryHandleProxy(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Medic } medic)
            return false;

        uint parentSpellId = spell.Parameters.SpellInfo.Entry.Id;
        uint proxySpellId = info.Entry.DataBits00;
        if (parentSpellId == MedicSpellIds.Discharge
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
}
