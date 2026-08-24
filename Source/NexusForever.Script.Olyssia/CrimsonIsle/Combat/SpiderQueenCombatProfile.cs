using NexusForever.Script.Template.AI;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Olyssia.CrimsonIsle.Combat
{
    [ScriptFilterCreatureId(24059u)]
    public sealed class SpiderQueenCombatProfile : ICreatureCombatProfileScript
    {
        public CombatProfile Profile { get; } = new(
            [6651u, 6652u], // Pinch, Slice
            3f,
            [
                new CombatAbility(39574u, 10d, 3d), // Corrosive Venom + Lingering Venom
                new CombatAbility(55328u, 13d, 8d)  // Venomous Mandible, tutorial tier 3
            ]);
    }
}
