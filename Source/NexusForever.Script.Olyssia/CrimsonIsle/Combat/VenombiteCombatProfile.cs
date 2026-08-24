using NexusForever.Script.Template.AI;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Olyssia.CrimsonIsle.Combat
{
    [ScriptFilterCreatureId(24057u)]
    public sealed class VenombiteCombatProfile : ICreatureCombatProfileScript
    {
        public CombatProfile Profile { get; } = new(
            [6651u, 6652u], // Pinch, Slice
            3f,
            [new CombatAbility(55327u, 10d, 5d)]); // Venomous Mandible, tutorial tier 2
    }
}
