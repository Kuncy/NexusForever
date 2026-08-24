using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Esper;

public sealed partial class EsperClassMechanics
{
    public void UpdateResources(IPlayer player, uint statUpdateTick, double outOfCombatTime)
    {
        if (outOfCombatTime >= 10d)
            player.ModifyVital(Vital.Resource1, -player.GetVitalMaximum(Vital.Resource1));
    }
}
