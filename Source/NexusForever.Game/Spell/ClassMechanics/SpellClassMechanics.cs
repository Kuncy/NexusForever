using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Static;
using NexusForever.Game.Spell.ClassMechanics.Spellslinger;
using NexusForever.Game.Spell.ClassMechanics.Warrior;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell.ClassMechanics;

public static class SpellClassMechanics
{
    public static bool TryCheckPrerequisites(Spell spell, IPlayer player, out CastResult result)
    {
        if (WarriorSpellMechanics.TryCheckPrerequisites(spell, player, out result))
            return true;
        return SpellslingerSpellMechanics.TryCheckPrerequisites(spell, player, out result);
    }

    public static void BeforeExecute(Spell spell, IPlayer player)
    {
        WarriorSpellMechanics.BeforeExecute(spell, player);
        SpellslingerSpellMechanics.BeforeExecute(spell, player);
    }

    public static Spell4Entry SelectCooldownEntry(Spell spell, Spell4Entry entry)
        => WarriorSpellMechanics.SelectCooldownEntry(spell, entry);

    public static void AfterEffects(Spell spell, IPlayer player)
    {
        WarriorSpellMechanics.AfterEffects(spell, player);
        SpellslingerSpellMechanics.AfterEffects(spell, player);
    }

    public static void AfterSpellGo(Spell spell, IPlayer player)
        => WarriorSpellMechanics.AfterSpellGo(spell, player);
}
