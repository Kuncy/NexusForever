using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Stalker;

public static class StalkerResourceMechanics
{
    public static void Update(IPlayer player, uint statUpdateTick)
    {
        if (statUpdateTick % 2u != 0u)
            return;

        float amount = player.GetVitalMaximum(Vital.Resource3)
            * player.GetPropertyValue(Property.ResourceRegenMultiplier3);
        player.ModifyVital(Vital.Resource3, amount);
    }
}
