using NexusForever.Script.Template.AI;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Olyssia.CrimsonIsle.Combat
{
    [ScriptFilterCreatureId(24030u, 24216u)]
    public sealed class RifleCombatProfile : ICreatureCombatProfileScript
    {
        public CombatProfile Profile { get; } = new(
            [2764u], // Plasma Shot
            14f,
            [new CombatAbility(51828u, 9d, 4d)]); // Shell Storm
    }
}
