using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Spellslinger;

public sealed partial class SpellslingerClassMechanics
{
    public void InitialiseResources(IPlayer player)
    {
        player.ModifyVital(Vital.Resource4, player.GetVitalMaximum(Vital.Resource4));
    }

    public void UpdateResources(IPlayer player, uint statUpdateTick, double outOfCombatTime)
    {
        if (statUpdateTick % 4u == 0u)
            player.ModifyVital(Vital.Resource4, 4f);
    }
}
