using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Map.Search;
using NSubstitute;

namespace NexusForever.Game.Tests.Map
{
    /// <remarks>
    /// This is the filter that decides which party members are close enough to share a kill, so its boundary
    /// behaviour is part of the reward rules rather than an implementation detail.
    /// </remarks>
    public class SearchCheckRangeTests
    {
        private static IPlayer PlayerAt(float x, float y = 0f, float z = 0f)
        {
            var player = Substitute.For<IPlayer>();
            player.Position.Returns(new Vector3(x, y, z));
            return player;
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(50f)]
        [InlineData(99.9f)]
        public void EntitiesInsideTheRadiusPass(float distance)
        {
            var check = new SearchCheckRange<IPlayer>(Vector3.Zero, 100f);

            Assert.True(check.CheckEntity(PlayerAt(distance)));
        }

        [Theory]
        [InlineData(100f)]
        [InlineData(150f)]
        public void EntitiesAtOrBeyondTheRadiusFail(float distance)
        {
            var check = new SearchCheckRange<IPlayer>(Vector3.Zero, 100f);

            // The radius is exclusive, a member exactly on the edge is out.
            Assert.False(check.CheckEntity(PlayerAt(distance)));
        }

        [Fact]
        public void HeightCountsTowardsTheDistance()
        {
            var check = new SearchCheckRange<IPlayer>(Vector3.Zero, 100f);

            Assert.True(check.CheckEntity(PlayerAt(60f, 60f)));   // ~85 units
            Assert.False(check.CheckEntity(PlayerAt(80f, 80f)));  // ~113 units
        }

        [Fact]
        public void AMissingRadiusMatchesEverything()
        {
            var check = new SearchCheckRange<IPlayer>(Vector3.Zero, null);

            Assert.True(check.CheckEntity(PlayerAt(100_000f)));
        }

        [Fact]
        public void TheExcludedEntityNeverPasses()
        {
            IPlayer excluded = PlayerAt(0f);
            var check = new SearchCheckRange<IPlayer>(Vector3.Zero, 100f, excluded);

            Assert.False(check.CheckEntity(excluded));
            Assert.True(check.CheckEntity(PlayerAt(1f)));
        }
    }
}
