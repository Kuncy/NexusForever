using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Esper;

public static class EsperResourceMechanics
{
    public static void Update(IPlayer player, double outOfCombatTime)
    {
        if (outOfCombatTime >= 10d)
            player.ModifyVital(Vital.Resource1, -player.GetVitalMaximum(Vital.Resource1));
    }
}
