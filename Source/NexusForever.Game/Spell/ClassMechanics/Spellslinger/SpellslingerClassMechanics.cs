using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Spellslinger;

/// <summary>
/// Server side mechanics for <see cref="Class.Spellslinger"/>.
/// </summary>
/// <remarks>
/// Split across partials by the area each hook belongs to.
/// </remarks>
public sealed partial class SpellslingerClassMechanics : IClassMechanics
{
    public Class Class => Class.Spellslinger;
}
