using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell.ClassMechanics.Engineer;
using NexusForever.Game.Spell.ClassMechanics.Esper;
using NexusForever.Game.Spell.ClassMechanics.Medic;
using NexusForever.Game.Spell.ClassMechanics.Spellslinger;
using NexusForever.Game.Spell.ClassMechanics.Stalker;
using NexusForever.Game.Spell.ClassMechanics.Warrior;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics;

public static class ClassResourceMechanics
{
    public static void Initialise(IPlayer player)
    {
        switch (player.Class)
        {
            case Class.Stalker:
                player.ModifyVital(Vital.Resource3, player.GetVitalMaximum(Vital.Resource3));
                break;
            case Class.Spellslinger:
                player.ModifyVital(Vital.Resource4, player.GetVitalMaximum(Vital.Resource4));
                break;
        }
    }

    public static void Update(IPlayer player, uint statUpdateTick, double outOfCombatTime)
    {
        switch (player.Class)
        {
            case Class.Warrior:
                WarriorResourceMechanics.Update(player, statUpdateTick);
                break;
            case Class.Engineer:
                EngineerResourceMechanics.Update(player, statUpdateTick, outOfCombatTime);
                break;
            case Class.Esper:
                EsperResourceMechanics.Update(player, outOfCombatTime);
                break;
            case Class.Medic:
                MedicResourceMechanics.Update(player, statUpdateTick, outOfCombatTime);
                break;
            case Class.Stalker:
                StalkerResourceMechanics.Update(player, statUpdateTick);
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
