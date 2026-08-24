using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell.ClassMechanics.Engineer;
using NexusForever.Game.Spell.ClassMechanics.Esper;
using NexusForever.Game.Spell.ClassMechanics.Medic;
using NexusForever.Network.World.Message.Static;
using NexusForever.Game.Spell.ClassMechanics.Spellslinger;
using NexusForever.Game.Spell.ClassMechanics.Stalker;
using NexusForever.Game.Spell.ClassMechanics.Warrior;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell.ClassMechanics;

public static class SpellClassMechanics
{
    public static bool TryCheckPrerequisites(Spell spell, IPlayer player, out CastResult result)
    {
        if (EngineerSpellMechanics.TryCheckPrerequisites(spell, player, out result))
            return true;
        if (EsperSpellMechanics.TryCheckPrerequisites(spell, player, out result))
            return true;
        if (MedicSpellMechanics.TryCheckPrerequisites(spell, player, out result))
            return true;
        if (WarriorSpellMechanics.TryCheckPrerequisites(spell, player, out result))
            return true;
        if (SpellslingerSpellMechanics.TryCheckPrerequisites(spell, player, out result))
            return true;
        return StalkerSpellMechanics.TryCheckPrerequisites(spell, player, out result);
    }

    public static void BeforeExecute(Spell spell, IPlayer player)
    {
        EngineerSpellMechanics.BeforeExecute(spell, player);
        EsperSpellMechanics.BeforeExecute(spell, player);
        MedicSpellMechanics.BeforeExecute(spell, player);
        WarriorSpellMechanics.BeforeExecute(spell, player);
        SpellslingerSpellMechanics.BeforeExecute(spell, player);
        StalkerSpellMechanics.BeforeExecute(spell, player);
    }

    public static Spell4Entry SelectCooldownEntry(Spell spell, IPlayer player, Spell4Entry entry)
    {
        entry = WarriorSpellMechanics.SelectCooldownEntry(spell, entry);
        return StalkerSpellMechanics.SelectCooldownEntry(spell, player, entry);
    }

    public static void AfterEffects(Spell spell, IPlayer player)
    {
        EngineerSpellMechanics.AfterEffects(spell, player);
        MedicSpellMechanics.AfterEffects(spell, player);
        WarriorSpellMechanics.AfterEffects(spell, player);
        SpellslingerSpellMechanics.AfterEffects(spell, player);
        StalkerSpellMechanics.AfterEffects(spell, player);
    }

    public static void AfterSpellGo(Spell spell, IPlayer player)
    {
        WarriorSpellMechanics.AfterSpellGo(spell, player);
        StalkerSpellMechanics.AfterSpellGo(spell, player);
    }

    public static bool ShouldFinishRoot(Spell spell)
        => EngineerSpellMechanics.ShouldFinishRoot(spell)
            || MedicSpellMechanics.ShouldFinishRoot(spell);

    public static bool TryCostResources(Spell spell, IPlayer player, Spell4Entry entry)
        => EsperSpellMechanics.TryCostResources(spell, player, entry)
            || StalkerSpellMechanics.TryCostResources(spell, player, entry);

    public static bool IsServerExecutedChannel(Spell spell)
        => EngineerSpellMechanics.IsServerExecutedChannel(spell)
            || EsperSpellMechanics.IsServerExecutedChannel(spell)
            || MedicSpellMechanics.IsServerExecutedChannel(spell)
            || SpellslingerSpellMechanics.IsServerExecutedChannel(spell)
            || StalkerSpellMechanics.IsServerExecutedChannel(spell)
            || WarriorSpellMechanics.IsServerExecutedChannel(spell);
}
