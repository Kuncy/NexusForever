using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Warrior;

public static class WarriorResourceMechanics
{
    public static void Update(IPlayer player, uint statUpdateTick)
    {
        if (statUpdateTick % 4u == 0u && WarriorState.For(player).CanDecayKineticEnergy)
            player.ModifyVital(Vital.Resource1, -150f);
    }

    public static void OnVitalModified(IPlayer player, Vital vital, float amount)
    {
        if (vital is Vital.Resource1 or Vital.KineticCell && amount > 0f)
            WarriorState.For(player).StartKineticEnergyGracePeriod();
    }
}
