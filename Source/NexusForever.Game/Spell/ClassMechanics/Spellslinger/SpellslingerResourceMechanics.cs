using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Spellslinger;

public static class SpellslingerResourceMechanics
{
    public static void Update(IPlayer player, uint statUpdateTick)
    {
        if (statUpdateTick % 4u == 0u)
            player.ModifyVital(Vital.Resource4, 4f);
    }
}
