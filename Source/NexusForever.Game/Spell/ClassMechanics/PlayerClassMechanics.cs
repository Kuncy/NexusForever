using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell.ClassMechanics.Spellslinger;
using NexusForever.Game.Spell.ClassMechanics.Warrior;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics;

public static class PlayerClassMechanics
{
    public static void Update(IPlayer player, double lastTick)
    {
        switch (player.Class)
        {
            case Class.Warrior:
                WarriorState.For(player).Update(player, lastTick);
                break;
            case Class.Spellslinger:
                SpellslingerState.For(player).Update(player, lastTick);
                break;
        }
    }
}
