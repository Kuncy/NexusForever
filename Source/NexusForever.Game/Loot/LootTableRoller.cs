namespace NexusForever.Game.Loot
{
    internal static class LootTableRoller
    {
        public const uint ChanceScale = 1_000_000u;

        public static IReadOnlyList<LootResult> Roll(LootTable table, Random random)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(random);

            var results = new List<LootResult>();

            foreach (LootTableEntry entry in table.IndependentEntries)
                if (RollChance(entry.Chance, random))
                    results.Add(CreateResult(entry, random));

            foreach (IReadOnlyList<LootTableEntry> group in table.EntryGroups)
            {
                uint roll = (uint)random.NextInt64(ChanceScale);
                uint cumulativeChance = 0u;

                foreach (LootTableEntry entry in group)
                {
                    cumulativeChance += entry.Chance;
                    if (roll >= cumulativeChance)
                        continue;

                    results.Add(CreateResult(entry, random));
                    break;
                }
            }

            return results;
        }

        private static bool RollChance(uint chance, Random random)
        {
            if (chance == 0u)
                return false;
            if (chance == ChanceScale)
                return true;

            return random.NextInt64(ChanceScale) < chance;
        }

        private static LootResult CreateResult(LootTableEntry entry, Random random)
        {
            uint amount = entry.MinAmount == entry.MaxAmount
                ? entry.MinAmount
                : (uint)random.NextInt64(entry.MinAmount, (long)entry.MaxAmount + 1L);

            return new LootResult(entry.Id, entry.Type, entry.ItemId, amount);
        }
    }
}
