using System.Collections.Immutable;

namespace NexusForever.Game.Loot
{
    internal sealed record LootTable(uint Id, ImmutableList<LootTableEntry> IndependentEntries,
        ImmutableList<ImmutableList<LootTableEntry>> EntryGroups);
}
