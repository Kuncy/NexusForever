using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Spell.ClassMechanics;

public static class PlayerClassMechanics
{
    public static void Update(IPlayer player, double lastTick)
    {
        player.ClassState?.Update(player, lastTick);
    }
}
