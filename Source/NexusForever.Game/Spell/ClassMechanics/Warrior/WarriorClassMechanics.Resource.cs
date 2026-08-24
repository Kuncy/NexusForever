using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Warrior;

public sealed partial class WarriorClassMechanics
{
    public void UpdateResources(IPlayer player, uint statUpdateTick, double outOfCombatTime)
    {
        if (statUpdateTick % 4u == 0u && WarriorState.For(player).CanDecayKineticEnergy)
            player.ModifyVital(Vital.Resource1, -150f);
    }

    /// <remarks>
    /// The grace period is driven by the requested <paramref name="amount"/> rather than the applied
    /// <paramref name="delta"/>, a warrior at maximum kinetic energy that keeps generating must not start decaying.
    /// </remarks>
    public void OnVitalModified(IPlayer player, Vital vital, float amount, float delta)
    {
        if (vital is Vital.Resource1 or Vital.KineticCell && amount > 0f)
            WarriorState.For(player).StartKineticEnergyGracePeriod();
    }
}
