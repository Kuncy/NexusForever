using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Esper;

public static class EsperEffectMechanics
{
    public static bool ShouldApplyDamage(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id != EsperSpellIds.MindBurst
            || spell.Caster is not IPlayer { Class: Class.Esper } esper)
            return true;

        uint psiPoints = (uint)Math.Clamp(
            (int)MathF.Floor(esper.GetVitalValue(Vital.Resource1)), 1, 5);
        bool matchingRow = info.Entry.OrderIndex == psiPoints - 1u;
        if (!matchingRow)
            info.DropEffect = true;
        return matchingRow;
    }

    public static bool TryHandleProxy(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Esper }
            || spell.Parameters.SpellInfo.Entry.Id != EsperSpellIds.TelekineticStrike
            || info.Entry.DataBits00 != EsperSpellIds.TelekineticStrikePsiPoint)
            return false;

        if (spell.TryConsumeSuccessfulHit())
            spell.CastProxySpell(info.Entry.DataBits00, spell.Caster);
        return true;
    }
}
