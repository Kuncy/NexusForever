using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell.ClassMechanics.Esper;

public static class EsperSpellMechanics
{
    public static bool IsServerExecutedChannel(Spell spell)
        => spell.Parameters.SpellInfo.BaseInfo.Entry.Id is 23012u or 34536u
            or 19166u or 19025u or 23216u;

    public static bool TryCostResources(Spell spell, IPlayer player, Spell4Entry entry)
    {
        if (player.Class != Class.Esper
            || !spell.Parameters.UserInitiatedSpellCast
            || !EsperSpellIds.IsPsiFinisher(
                spell.Parameters.RootSpellInfo.BaseInfo.Entry.Id))
            return false;

        float psiPoints = spell.Parameters.ClassResourceSnapshot > 0u
            ? spell.Parameters.ClassResourceSnapshot
            : Math.Clamp(player.GetVitalValue(Vital.Resource1), 1f, 5f);
        player.ModifyVital(Vital.Resource1, -psiPoints);
        spell.CostResource(entry.InnateCostType1, entry.InnateCost1);
        return true;
    }

    public static bool TryCheckPrerequisites(Spell spell, IPlayer player,
        out NexusForever.Network.World.Message.Static.CastResult result)
    {
        result = NexusForever.Network.World.Message.Static.CastResult.Ok;
        if (player.Class != Class.Esper)
            return false;
        if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id == EsperSpellIds.ReapBase)
        {
            result = player.GetVitalValue(Vital.Resource1) > 2f ? result
                : NexusForever.Network.World.Message.Static.CastResult.CasterVitalCostResource1;
            return true;
        }
        return false;
    }

    public static void BeforeExecute(Spell spell, IPlayer player)
    {
        if (player.Class != Class.Esper)
            return;
        uint rootBaseId = spell.Parameters.RootSpellInfo.BaseInfo.Entry.Id;
        if (EsperSpellIds.IsPsiFinisher(rootBaseId)
            && spell.Parameters.ClassResourceSnapshot == 0u)
            spell.Parameters.ClassResourceSnapshot = EsperState.For(player)
                .SnapshotPsiPoints(player);
        if (rootBaseId == EsperSpellIds.FixationBase
            && spell.Parameters.UserInitiatedSpellCast)
            player.SpellManager.ResetAllSpellCooldowns();
    }
}
