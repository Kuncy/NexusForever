using NexusForever.Game.Loot;

namespace NexusForever.Game.Tests.Loot
{
    public class LootCurrencySplitterTests
    {
        [Fact]
        public void SoloPlayerReceivesTheWholePool()
        {
            Assert.Equal([1337ul], LootCurrencySplitter.Split(1337ul, 1));
        }

        [Fact]
        public void EvenPoolIsSplitEvenly()
        {
            Assert.Equal([25ul, 25ul, 25ul, 25ul], LootCurrencySplitter.Split(100ul, 4));
        }

        [Fact]
        public void RemainderGoesToTheLeadingShares()
        {
            // 10 / 4 = 2 remainder 2, so the first two members get one extra unit each.
            Assert.Equal([3ul, 3ul, 2ul, 2ul], LootCurrencySplitter.Split(10ul, 4));
        }

        [Theory]
        [InlineData(0ul, 5)]
        [InlineData(1ul, 5)]
        [InlineData(7ul, 3)]
        [InlineData(99ul, 5)]
        [InlineData(1_000_003ul, 7)]
        [InlineData(ulong.MaxValue, 5)]
        public void SharesAlwaysSumBackToTheTotal(ulong total, int memberCount)
        {
            ulong[] shares = LootCurrencySplitter.Split(total, memberCount);

            Assert.Equal(memberCount, shares.Length);
            Assert.Equal(total, shares.Aggregate(0ul, (sum, share) => sum + share));
        }

        [Theory]
        [InlineData(1ul, 5)]
        [InlineData(7ul, 3)]
        [InlineData(1_000_003ul, 7)]
        public void SharesDifferByAtMostOne(ulong total, int memberCount)
        {
            ulong[] shares = LootCurrencySplitter.Split(total, memberCount);

            Assert.InRange(shares.Max() - shares.Min(), 0ul, 1ul);
        }

        [Fact]
        public void EmptyPoolYieldsEmptyShares()
        {
            Assert.All(LootCurrencySplitter.Split(0ul, 5), share => Assert.Equal(0ul, share));
        }

        [Fact]
        public void ASmallPoolLeavesTrailingMembersEmptyRatherThanRounding()
        {
            // One unit cannot be shared five ways, and inventing four more units would be worse.
            Assert.Equal([1ul, 0ul, 0ul, 0ul, 0ul], LootCurrencySplitter.Split(1ul, 5));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void NonPositiveMemberCountIsRejected(int memberCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LootCurrencySplitter.Split(100ul, memberCount));
        }
    }
}
