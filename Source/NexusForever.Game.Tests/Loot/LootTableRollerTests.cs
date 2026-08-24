using System.Collections.Immutable;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Loot;

namespace NexusForever.Game.Tests.Loot
{
    /// <remarks>
    /// Every test drives the roller with a seeded <see cref="Random"/>, so the statistical assertions below are
    /// reproducible rather than flaky. A failure means the roller changed, not that the dice were unlucky.
    /// </remarks>
    public class LootTableRollerTests
    {
        private const uint Scale = LootTableRoller.ChanceScale;
        private const int Seed = 20260824;

        private static LootTableEntry Entry(uint id, uint chance, uint minAmount = 1u, uint maxAmount = 1u)
            => new(id, LootItemType.StaticItem, id + 1000u, minAmount, maxAmount, chance);

        private static LootTable Independent(params LootTableEntry[] entries)
            => new(1u, entries.ToImmutableList(), []);

        private static LootTable Group(params LootTableEntry[] entries)
            => new(1u, [], ImmutableList.Create(entries.ToImmutableList()));

        private static List<LootResult> RollMany(LootTable table, int iterations, out List<int> countsPerRoll)
        {
            var random = new Random(Seed);
            var results = new List<LootResult>();
            countsPerRoll = [];

            for (int i = 0; i < iterations; i++)
            {
                IReadOnlyList<LootResult> roll = LootTableRoller.Roll(table, random);
                countsPerRoll.Add(roll.Count);
                results.AddRange(roll);
            }

            return results;
        }

        [Fact]
        public void IndependentEntryWithZeroChanceNeverDrops()
        {
            List<LootResult> results = RollMany(Independent(Entry(1u, 0u)), 10_000, out _);

            Assert.Empty(results);
        }

        [Fact]
        public void IndependentEntryWithFullChanceAlwaysDrops()
        {
            List<LootResult> results = RollMany(Independent(Entry(1u, Scale)), 1_000, out List<int> counts);

            Assert.Equal(1_000, results.Count);
            Assert.All(counts, count => Assert.Equal(1, count));
        }

        [Fact]
        public void IndependentEntriesRollSeparatelyAndCanAllDropTogether()
        {
            LootTable table = Independent(Entry(1u, Scale), Entry(2u, Scale), Entry(3u, Scale));

            RollMany(table, 100, out List<int> counts);

            // Independent entries are not exclusive: a single roll may yield every one of them.
            Assert.All(counts, count => Assert.Equal(3, count));
        }

        [Fact]
        public void IndependentEntryChanceIsHonoured()
        {
            const int iterations = 200_000;
            LootTable table = Independent(Entry(1u, Scale / 4u)); // 25%

            List<LootResult> results = RollMany(table, iterations, out _);

            double rate = (double)results.Count / iterations;
            Assert.InRange(rate, 0.245d, 0.255d);
        }

        [Fact]
        public void EntryGroupYieldsAtMostOneEntry()
        {
            // Three mutually exclusive entries that together cover the whole scale.
            LootTable table = Group(Entry(1u, Scale / 3u), Entry(2u, Scale / 3u), Entry(3u, Scale / 3u));

            RollMany(table, 50_000, out List<int> counts);

            Assert.All(counts, count => Assert.InRange(count, 0, 1));
        }

        [Fact]
        public void EntryGroupBelowFullChanceCanYieldNothing()
        {
            // Half of the group's probability space is deliberately left empty.
            LootTable table = Group(Entry(1u, Scale / 4u), Entry(2u, Scale / 4u));

            List<LootResult> results = RollMany(table, 200_000, out List<int> counts);

            Assert.Contains(0, counts);
            double rate = (double)results.Count / counts.Count;
            Assert.InRange(rate, 0.495d, 0.505d);
        }

        [Fact]
        public void EntryGroupSharesAreHonoured()
        {
            const int iterations = 200_000;
            LootTableEntry common = Entry(1u, Scale / 2u);   // 50%
            LootTableEntry uncommon = Entry(2u, Scale / 10u * 3u); // 30%
            LootTableEntry rare = Entry(3u, Scale / 5u);     // 20%

            List<LootResult> results = RollMany(Group(common, uncommon, rare), iterations, out _);

            Dictionary<uint, int> byEntry = results
                .GroupBy(result => result.EntryId)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.InRange((double)byEntry[common.Id] / iterations, 0.495d, 0.505d);
            Assert.InRange((double)byEntry[uncommon.Id] / iterations, 0.295d, 0.305d);
            Assert.InRange((double)byEntry[rare.Id] / iterations, 0.195d, 0.205d);
        }

        [Fact]
        public void EntryGroupFavoursTheFirstMatchingEntryInOrder()
        {
            // A group whose first entry covers the whole scale must starve the later entries.
            LootTable table = Group(Entry(1u, Scale), Entry(2u, Scale));

            List<LootResult> results = RollMany(table, 1_000, out _);

            Assert.All(results, result => Assert.Equal(1u, result.EntryId));
        }

        [Fact]
        public void AmountStaysWithinTheConfiguredRange()
        {
            LootTable table = Independent(Entry(1u, Scale, minAmount: 5u, maxAmount: 15u));

            List<LootResult> results = RollMany(table, 50_000, out _);

            Assert.All(results, result => Assert.InRange(result.Amount, 5u, 15u));
            // Both bounds must be reachable - an exclusive upper bound is a classic off-by-one here.
            Assert.Contains(5u, results.Select(result => result.Amount));
            Assert.Contains(15u, results.Select(result => result.Amount));
        }

        [Fact]
        public void FixedAmountIsReturnedWhenMinimumEqualsMaximum()
        {
            LootTable table = Independent(Entry(1u, Scale, minAmount: 7u, maxAmount: 7u));

            List<LootResult> results = RollMany(table, 100, out _);

            Assert.All(results, result => Assert.Equal(7u, result.Amount));
        }

        [Fact]
        public void ResultCarriesEntryTypeAndItemId()
        {
            LootTableEntry entry = Entry(42u, Scale);

            LootResult result = Assert.Single(LootTableRoller.Roll(Independent(entry), new Random(Seed)));

            Assert.Equal(entry.Id, result.EntryId);
            Assert.Equal(entry.Type, result.Type);
            Assert.Equal(entry.ItemId, result.ItemId);
        }

        [Fact]
        public void SameSeedProducesTheSameSequence()
        {
            LootTable table = Independent(Entry(1u, Scale / 2u, minAmount: 1u, maxAmount: 10u));

            List<LootResult> first = RollMany(table, 500, out _);
            List<LootResult> second = RollMany(table, 500, out _);

            Assert.Equal(first, second);
        }

        [Fact]
        public void IndependentAndGroupedEntriesAreRolledTogether()
        {
            var table = new LootTable(
                1u,
                ImmutableList.Create(Entry(1u, Scale)),
                ImmutableList.Create(ImmutableList.Create(Entry(2u, Scale))));

            IReadOnlyList<LootResult> results = LootTableRoller.Roll(table, new Random(Seed));

            Assert.Equal([1u, 2u], results.Select(result => result.EntryId));
        }
    }
}
