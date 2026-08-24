using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell.ClassMechanics.Engineer;
using NexusForever.Game.Spell.ClassMechanics.Esper;
using NexusForever.Game.Spell.ClassMechanics.Medic;
using NexusForever.Game.Spell.ClassMechanics.Spellslinger;
using NexusForever.Game.Spell.ClassMechanics.Stalker;
using NexusForever.Game.Spell.ClassMechanics.Warrior;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics;

/// <summary>
/// Holds the <see cref="IClassMechanics"/> implementation for each <see cref="Class"/>.
/// </summary>
public static class ClassMechanicsRegistry
{
    /// <summary>
    /// All registered <see cref="IClassMechanics"/>.
    /// </summary>
    /// <remarks>
    /// Only for hooks that are keyed on the spell rather than on a player, everything else resolves a single
    /// implementation from the acting <see cref="IPlayer"/>.
    /// </remarks>
    public static IReadOnlyList<IClassMechanics> All { get; } =
    [
        new WarriorClassMechanics(),
        new EngineerClassMechanics(),
        new EsperClassMechanics(),
        new MedicClassMechanics(),
        new StalkerClassMechanics(),
        new SpellslingerClassMechanics()
    ];

    private static readonly Dictionary<Class, IClassMechanics> mechanics =
        All.ToDictionary(m => m.Class);

    /// <summary>
    /// Return the <see cref="IClassMechanics"/> for the supplied <see cref="Class"/>, if any.
    /// </summary>
    public static IClassMechanics For(Class @class)
    {
        return mechanics.TryGetValue(@class, out IClassMechanics classMechanics) ? classMechanics : null;
    }

    /// <summary>
    /// Return the <see cref="IClassMechanics"/> for the supplied <see cref="IUnitEntity"/>, if any.
    /// </summary>
    /// <remarks>
    /// Returns null for anything that isn't a player, non player entities have no class mechanics.
    /// </remarks>
    public static IClassMechanics For(IUnitEntity entity)
    {
        return entity is IPlayer player ? For(player.Class) : null;
    }

    /// <summary>
    /// Invoke <paramref name="action"/> on the <see cref="IClassMechanics"/> of both supplied entities.
    /// </summary>
    /// <remarks>
    /// Used by hooks where either side of an exchange can react, the implementation is invoked once even when
    /// both entities share the same <see cref="Class"/>.
    /// </remarks>
    public static void ForBoth(IUnitEntity first, IUnitEntity second, Action<IClassMechanics> action)
    {
        IClassMechanics firstMechanics = For(first);
        if (firstMechanics != null)
            action.Invoke(firstMechanics);

        IClassMechanics secondMechanics = For(second);
        if (secondMechanics != null && secondMechanics != firstMechanics)
            action.Invoke(secondMechanics);
    }
}
