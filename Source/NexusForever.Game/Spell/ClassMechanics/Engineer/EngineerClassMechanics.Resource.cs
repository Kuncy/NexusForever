using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Engineer;

public sealed partial class EngineerClassMechanics
{
    public void UpdateResources(IPlayer player, uint statUpdateTick, double outOfCombatTime)
    {
        if (statUpdateTick % 2u == 0u && outOfCombatTime >= 3d)
            player.ModifyVital(Vital.Volatility, -10f);
    }
}
