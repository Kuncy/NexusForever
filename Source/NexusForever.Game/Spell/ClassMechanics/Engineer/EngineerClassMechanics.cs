using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Engineer;

/// <summary>
/// Server side mechanics for <see cref="Class.Engineer"/>.
/// </summary>
/// <remarks>
/// Split across partials by the area each hook belongs to.
/// </remarks>
public sealed partial class EngineerClassMechanics : IClassMechanics
{
    public Class Class => Class.Engineer;
}
