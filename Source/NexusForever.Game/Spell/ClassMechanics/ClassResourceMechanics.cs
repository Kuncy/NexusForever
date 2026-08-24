using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell.ClassMechanics.Spellslinger;
using NexusForever.Game.Spell.ClassMechanics.Warrior;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics;

public static class ClassResourceMechanics
{
    public static void Update(IPlayer player, uint statUpdateTick)
    {
        switch (player.Class)
        {
            case Class.Warrior:
                WarriorResourceMechanics.Update(player, statUpdateTick);
                break;
            case Class.Spellslinger:
                SpellslingerResourceMechanics.Update(player, statUpdateTick);
                break;
        }
    }

    public static void OnVitalModified(IPlayer player, Vital vital, float amount)
    {
        if (player.Class == Class.Warrior)
            WarriorResourceMechanics.OnVitalModified(player, vital, amount);
    }
}
