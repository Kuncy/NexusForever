using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell.ClassMechanics.Engineer;
using NexusForever.Game.Spell.ClassMechanics.Esper;
using NexusForever.Game.Spell.ClassMechanics.Medic;
using NexusForever.Game.Spell.ClassMechanics.Spellslinger;
using NexusForever.Game.Spell.ClassMechanics.Stalker;
using NexusForever.Game.Spell.ClassMechanics.Warrior;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics;

public static class ClassStates
{
    /// <summary>
    /// Create a new <see cref="IClassState"/> for the supplied <see cref="Class"/>.
    /// </summary>
    /// <remarks>
    /// Returns null for classes without any server side class mechanics.
    /// </remarks>
    public static IClassState Create(Class @class)
    {
        return @class switch
        {
            Class.Warrior      => new WarriorState(),
            Class.Engineer     => new EngineerState(),
            Class.Esper        => new EsperState(),
            Class.Medic        => new MedicState(),
            Class.Stalker      => new StalkerState(),
            Class.Spellslinger => new SpellslingerState(),
            _                  => null
        };
    }

    /// <summary>
    /// Return the <typeparamref name="TState"/> of the supplied <see cref="IPlayer"/>.
    /// </summary>
    /// <remarks>
    /// Call sites are expected to have already checked <see cref="IPlayer.Class"/>, a mismatch is a programming error.
    /// </remarks>
    internal static TState For<TState>(IPlayer player) where TState : class, IClassState
    {
        if (player.ClassState is not TState state)
            throw new InvalidOperationException(
                $"{player.Class} player {player.CharacterId} doesn't have a {typeof(TState).Name}!");

        return state;
    }
}
