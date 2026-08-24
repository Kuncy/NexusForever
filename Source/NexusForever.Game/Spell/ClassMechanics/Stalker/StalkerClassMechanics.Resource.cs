using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Stalker;

public sealed partial class StalkerClassMechanics
{
    public void InitialiseResources(IPlayer player)
    {
        player.ModifyVital(Vital.Resource3, player.GetVitalMaximum(Vital.Resource3));
    }

    public void UpdateResources(IPlayer player, uint statUpdateTick, double outOfCombatTime)
    {
        if (statUpdateTick % 2u != 0u)
            return;

        float amount = player.GetVitalMaximum(Vital.Resource3)
            * player.GetPropertyValue(Property.ResourceRegenMultiplier3);
        player.ModifyVital(Vital.Resource3, amount);
    }
}
