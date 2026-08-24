using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Loot;
using NSubstitute;

namespace NexusForever.Game.Tests.Loot
{
    /// <remarks>
    /// Covers the 35 unit loot leash: a player who walked away from the corpse must be refused, and loot whose
    /// owner has despawned (or who is standing on a different map) must not be reachable at all.
    /// </remarks>
    public class LootRangeTests
    {
        private const uint OwnerUnitId = 4711u;

        private static LootInstance CreateInstance()
            => new(OwnerUnitId, 1ul,
                [new LootInstanceItem(1u, new LootResult(1u, LootItemType.StaticItem, 123u, 1u))],
                TimeSpan.FromMinutes(30d));

        /// <summary>
        /// Build a player standing <paramref name="distance"/> away from the loot owner.
        /// </summary>
        private static IPlayer CreatePlayer(float distance, bool ownerOnMap = true, bool onMap = true)
        {
            var owner = Substitute.For<IWorldEntity>();
            owner.Position.Returns(Vector3.Zero);

            var map = Substitute.For<IBaseMap>();
            map.GetEntity<IWorldEntity>(OwnerUnitId).Returns(ownerOnMap ? owner : null);

            var player = Substitute.For<IPlayer>();
            player.Map.Returns(onMap ? map : null);
            player.Position.Returns(new Vector3(distance, 0f, 0f));
            return player;
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(10f)]
        [InlineData(34.9f)]
        [InlineData(35f)]
        public void LootIsReachableWithinRange(float distance)
        {
            Assert.True(LootManager.IsInLootRange(CreatePlayer(distance), CreateInstance()));
        }

        [Theory]
        [InlineData(35.1f)]
        [InlineData(50f)]
        [InlineData(1000f)]
        public void LootIsRefusedBeyondRange(float distance)
        {
            Assert.False(LootManager.IsInLootRange(CreatePlayer(distance), CreateInstance()));
        }

        /// <remarks>
        /// Pins down current behaviour rather than endorsing it: <c>MathsExtensions.GetDistance</c> measures on the
        /// X/Z plane only, so the loot leash ignores height entirely. A player far above or below a corpse can still
        /// loot it. Note this differs from <c>SearchCheckRange</c>, which measures in full 3D - if the loot leash is
        /// ever meant to account for height, this test is the one that should change.
        /// </remarks>
        [Fact]
        public void LootRangeIgnoresHeight()
        {
            var owner = Substitute.For<IWorldEntity>();
            owner.Position.Returns(Vector3.Zero);

            var map = Substitute.For<IBaseMap>();
            map.GetEntity<IWorldEntity>(OwnerUnitId).Returns(owner);

            var player = Substitute.For<IPlayer>();
            player.Map.Returns(map);
            player.Position.Returns(new Vector3(0f, 500f, 0f));

            Assert.True(LootManager.IsInLootRange(player, CreateInstance()));
        }

        [Fact]
        public void LootRangeIsMeasuredOnBothHorizontalAxes()
        {
            var owner = Substitute.For<IWorldEntity>();
            owner.Position.Returns(Vector3.Zero);

            var map = Substitute.For<IBaseMap>();
            map.GetEntity<IWorldEntity>(OwnerUnitId).Returns(owner);

            var player = Substitute.For<IPlayer>();
            player.Map.Returns(map);
            // 30 out on X and 30 out on Z is roughly 42 units away.
            player.Position.Returns(new Vector3(30f, 0f, 30f));

            Assert.False(LootManager.IsInLootRange(player, CreateInstance()));
        }

        [Fact]
        public void DespawnedOwnerMakesLootUnreachable()
        {
            Assert.False(LootManager.IsInLootRange(CreatePlayer(0f, ownerOnMap: false), CreateInstance()));
        }

        [Fact]
        public void PlayerWithoutAMapCannotLoot()
        {
            // Happens while a player is transferring between maps.
            Assert.False(LootManager.IsInLootRange(CreatePlayer(0f, onMap: false), CreateInstance()));
        }
    }
}
