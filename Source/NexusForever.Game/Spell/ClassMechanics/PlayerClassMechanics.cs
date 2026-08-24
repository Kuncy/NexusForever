using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell.ClassMechanics.Spellslinger;
using NexusForever.Game.Spell.ClassMechanics.Stalker;
using NexusForever.Game.Spell.ClassMechanics.Warrior;
using NexusForever.Game.Spell.ClassMechanics.Engineer;
using NexusForever.Game.Spell.ClassMechanics.Medic;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics;

public static class PlayerClassMechanics
{
    public static void Update(IPlayer player, double lastTick)
    {
        switch (player.Class)
        {
            case Class.Engineer:
                EngineerState.For(player).Update(lastTick);
                break;
            case Class.Warrior:
                WarriorState.For(player).Update(player, lastTick);
                break;
            case Class.Spellslinger:
                SpellslingerState.For(player).Update(player, lastTick);
                break;
            case Class.Stalker:
                StalkerState.For(player).Update(player, lastTick);
                break;
            case Class.Medic:
                MedicState.For(player).Update(lastTick);
                break;
        }
    }
}
