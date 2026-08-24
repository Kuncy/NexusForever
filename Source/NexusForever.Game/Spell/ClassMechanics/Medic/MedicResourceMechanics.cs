using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Medic;

public static class MedicResourceMechanics
{
    public static void Update(IPlayer player, double outOfCombatTime)
    {
        if (outOfCombatTime >= 3d)
            player.ModifyVital(Vital.MedicCore, player.GetVitalMaximum(Vital.MedicCore));
    }
}
