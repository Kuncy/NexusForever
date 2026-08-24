using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Stalker;

/// <summary>
/// Server side mechanics for <see cref="Class.Stalker"/>.
/// </summary>
/// <remarks>
/// Split across partials by the area each hook belongs to.
/// </remarks>
public sealed partial class StalkerClassMechanics : IClassMechanics
{
    public Class Class => Class.Stalker;
}
