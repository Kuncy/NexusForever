using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Medic;

/// <summary>
/// Server side mechanics for <see cref="Class.Medic"/>.
/// </summary>
/// <remarks>
/// Split across partials by the area each hook belongs to.
/// </remarks>
public sealed partial class MedicClassMechanics : IClassMechanics
{
    public Class Class => Class.Medic;
}
