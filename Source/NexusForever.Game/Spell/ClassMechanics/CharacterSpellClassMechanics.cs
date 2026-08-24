using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;

namespace NexusForever.Game.Spell.ClassMechanics;

/// <summary>
/// Entry point for the action bar related <see cref="IClassMechanics"/> hooks.
/// </summary>
public static class CharacterSpellClassMechanics
{
    public static bool CanBeginCast(IPlayer owner, ICharacterSpell characterSpell)
    {
        return ClassMechanicsRegistry.For(owner.Class)?.CanBeginCast(owner, characterSpell) ?? true;
    }

    public static ISpellInfo SelectSpell(IPlayer owner, ICharacterSpell characterSpell, ISpellInfo current)
    {
        IClassMechanics mechanics = ClassMechanicsRegistry.For(owner.Class);
        return mechanics != null ? mechanics.SelectSpell(owner, characterSpell, current) : current;
    }
}
