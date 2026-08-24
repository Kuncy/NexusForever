using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Static;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Loot;
using NSubstitute;

namespace NexusForever.Game.Tests.Loot
{
    /// <remarks>
    /// Covers the delivery contract that keeps loot from vanishing into a full bag: only what the inventory
    /// actually accepted may be reported to the client, and whatever did not fit stays on the loot entry.
    /// </remarks>
    public class LootInstanceItemTests
    {
        private const uint OwnerUnitId = 4711u;
        private const uint LootUnitId = 42u;
        private const uint ItemId = 12345u;

        private static IPlayer CreatePlayer(out IInventory inventory, out IGameSession session)
        {
            inventory = Substitute.For<IInventory>();
            session = Substitute.For<IGameSession>();

            var player = Substitute.For<IPlayer>();
            player.Guid.Returns(7u);
            player.Inventory.Returns(inventory);
            player.Session.Returns(session);
            player.CurrencyManager.Returns(Substitute.For<ICurrencyManager>());
            return player;
        }

        private static LootInstanceItem ItemLoot(uint amount)
            => new(LootUnitId, new LootResult(1u, LootItemType.StaticItem, ItemId, amount));

        private static List<ServerLootGrant> CapturedGrants(IGameSession session)
            => session.ReceivedCalls()
                .Where(call => call.GetMethodInfo().Name == nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(call => call.GetArguments()[0])
                .OfType<ServerLootGrant>()
                .ToList();

        [Fact]
        public void FullDeliveryConsumesTheEntryAndGrantsTheWholeStack()
        {
            IPlayer player = CreatePlayer(out IInventory inventory, out IGameSession session);
            inventory.ItemCreate(InventoryLocation.Inventory, ItemId, 5u, ItemUpdateReason.Loot).Returns(0u);
            LootInstanceItem item = ItemLoot(5u);

            Assert.True(item.Deliver(player, OwnerUnitId));

            Assert.Equal(0u, item.Result.Amount);
            ServerLootGrant grant = Assert.Single(CapturedGrants(session));
            Assert.Equal(5u, grant.LootItem.Amount);
            Assert.Equal(OwnerUnitId, grant.OwnerUnitId);
            Assert.Equal(player.Guid, grant.LooterUnitId);
        }

        [Fact]
        public void PartialDeliveryKeepsTheRemainderOnTheEntry()
        {
            IPlayer player = CreatePlayer(out IInventory inventory, out IGameSession session);
            // The bag only had room for three of the five items.
            inventory.ItemCreate(InventoryLocation.Inventory, ItemId, 5u, ItemUpdateReason.Loot).Returns(2u);
            LootInstanceItem item = ItemLoot(5u);

            Assert.False(item.Deliver(player, OwnerUnitId));

            Assert.Equal(2u, item.Result.Amount);
            // Reporting 5 here would make the client drop the whole stack from the loot window.
            Assert.Equal(3u, Assert.Single(CapturedGrants(session)).LootItem.Amount);
        }

        [Fact]
        public void FullBagLeavesTheEntryUntouchedAndGrantsNothing()
        {
            IPlayer player = CreatePlayer(out IInventory inventory, out IGameSession session);
            inventory.ItemCreate(InventoryLocation.Inventory, ItemId, 5u, ItemUpdateReason.Loot).Returns(5u);
            LootInstanceItem item = ItemLoot(5u);

            Assert.False(item.Deliver(player, OwnerUnitId));

            Assert.Equal(5u, item.Result.Amount);
            Assert.Empty(CapturedGrants(session));
        }

        [Fact]
        public void RetryAfterMakingRoomDeliversTheRemainder()
        {
            IPlayer player = CreatePlayer(out IInventory inventory, out IGameSession session);
            LootInstanceItem item = ItemLoot(5u);

            inventory.ItemCreate(InventoryLocation.Inventory, ItemId, 5u, ItemUpdateReason.Loot).Returns(5u);
            Assert.False(item.Deliver(player, OwnerUnitId));

            inventory.ItemCreate(InventoryLocation.Inventory, ItemId, 5u, ItemUpdateReason.Loot).Returns(0u);
            Assert.True(item.Deliver(player, OwnerUnitId));

            Assert.Equal(0u, item.Result.Amount);
            Assert.Equal(5u, Assert.Single(CapturedGrants(session)).LootItem.Amount);
        }

        [Fact]
        public void CurrencyIsAlwaysFullyConsumed()
        {
            IPlayer player = CreatePlayer(out _, out IGameSession session);
            var item = new LootInstanceItem(
                LootUnitId, new LootResult(1u, LootItemType.Cash, (uint)CurrencyType.Credits, 250u));

            Assert.True(item.Deliver(player, OwnerUnitId));

            player.CurrencyManager.Received(1).CurrencyAddAmount(CurrencyType.Credits, 250ul, true);
            Assert.Equal(0u, item.Result.Amount);
            Assert.Equal(250u, Assert.Single(CapturedGrants(session)).LootItem.Amount);
        }

        [Fact]
        public void UnsupportedLootTypeIsRejected()
        {
            IPlayer player = CreatePlayer(out _, out _);
            var item = new LootInstanceItem(LootUnitId, new LootResult(1u, LootItemType.Spell, ItemId, 1u));

            Assert.Throws<InvalidOperationException>(() => item.Deliver(player, OwnerUnitId));
        }
    }
}
