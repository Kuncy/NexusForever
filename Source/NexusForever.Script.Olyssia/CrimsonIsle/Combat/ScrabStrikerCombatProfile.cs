using NexusForever.Script.Template.AI;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Olyssia.CrimsonIsle.Combat
{
    [ScriptFilterCreatureId(24054u, 24364u)]
    public sealed class ScrabStrikerCombatProfile : ICreatureCombatProfileScript
    {
        public CombatProfile Profile { get; } = new(
            [25870u, 25871u], // Pinch, Snap
            3f,
            [new CombatAbility(55324u, 9d, 4d)]); // Poison Strike, tutorial tier 2
    }
}
