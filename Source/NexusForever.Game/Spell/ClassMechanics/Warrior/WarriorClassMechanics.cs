using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Warrior;

/// <summary>
/// Server side mechanics for <see cref="Class.Warrior"/>.
/// </summary>
/// <remarks>
/// Split across partials by the area each hook belongs to.
/// </remarks>
public sealed partial class WarriorClassMechanics : IClassMechanics
{
    public Class Class => Class.Warrior;
}
