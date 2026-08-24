using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Medic;

public sealed partial class MedicClassMechanics
{
    public void UpdateResources(IPlayer player, uint statUpdateTick, double outOfCombatTime)
    {
        if (outOfCombatTime >= 3d && statUpdateTick % 8u == 0u)
            player.ModifyVital(Vital.MedicCore, 1f);
    }
}
