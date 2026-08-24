using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Engineer;

public static class EngineerResourceMechanics
{
    public static void Update(IPlayer player, uint statUpdateTick, double outOfCombatTime)
    {
        if (statUpdateTick % 2u == 0u && outOfCombatTime >= 3d)
            player.ModifyVital(Vital.Volatility, -10f);
    }
}
