using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Esper;

/// <summary>
/// Server side mechanics for <see cref="Class.Esper"/>.
/// </summary>
/// <remarks>
/// Split across partials by the area each hook belongs to.
/// </remarks>
public sealed partial class EsperClassMechanics : IClassMechanics
{
    public Class Class => Class.Esper;
}
