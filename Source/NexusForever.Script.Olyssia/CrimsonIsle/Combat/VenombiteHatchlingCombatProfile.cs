using NexusForever.Script.Template.AI;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Olyssia.CrimsonIsle.Combat
{
    [ScriptFilterCreatureId(24051u)]
    public sealed class VenombiteHatchlingCombatProfile : ICreatureCombatProfileScript
    {
        public CombatProfile Profile { get; } = new(
            [6651u, 6652u], // Pinch, Slice
            3f,
            []);
    }
}
