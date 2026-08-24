using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell.ClassMechanics.Spellslinger;
using NexusForever.Game.Spell.ClassMechanics.Warrior;

namespace NexusForever.Game.Spell.ClassMechanics;

public static class CharacterSpellClassMechanics
{
    public static bool CanBeginCast(IPlayer owner, ICharacterSpell characterSpell)
        => WarriorCharacterSpellMechanics.CanBeginCast(owner, characterSpell)
            && SpellslingerCharacterSpellMechanics.CanBeginCast(owner, characterSpell);

    public static ISpellInfo SelectSpell(IPlayer owner, ICharacterSpell characterSpell, ISpellInfo current)
    {
        current = WarriorCharacterSpellMechanics.SelectSpell(owner, characterSpell, current);
        return SpellslingerCharacterSpellMechanics.SelectSpell(owner, characterSpell, current);
    }
}
