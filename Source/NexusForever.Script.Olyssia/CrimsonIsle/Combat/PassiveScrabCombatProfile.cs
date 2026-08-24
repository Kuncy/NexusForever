using NexusForever.Script.Template.AI;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Olyssia.CrimsonIsle.Combat
{
    [ScriptFilterCreatureId(24056u, 25029u)]
    public sealed class PassiveScrabCombatProfile : ICreatureCombatProfileScript
    {
        public CombatProfile Profile { get; } = new(
            [25870u, 25871u], // Pinch, Snap
            3f,
            []);
    }
}
