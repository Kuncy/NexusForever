using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell.ClassMechanics.Esper;

public static class EsperSpellMechanics
{
    public static bool IsServerExecutedChannel(Spell spell)
        => spell.Parameters.SpellInfo.BaseInfo.Entry.Id is 23012u or 34536u;

    public static bool TryCostResources(Spell spell, IPlayer player, Spell4Entry entry)
    {
        if (player.Class != Class.Esper
            || spell.Parameters.SpellInfo.BaseInfo.Entry.Id != EsperSpellIds.MindBurst)
            return false;

        float psiPoints = Math.Clamp(player.GetVitalValue(Vital.Resource1), 1f, 5f);
        player.ModifyVital(Vital.Resource1, -psiPoints);
        spell.CostResource(entry.InnateCostType1, entry.InnateCost1);
        return true;
    }
}
