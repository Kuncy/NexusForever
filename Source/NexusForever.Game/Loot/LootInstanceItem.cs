using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Network.World.Message.Static;
using NetworkLootItem = NexusForever.Network.World.Message.Model.Loot.LootItem;

namespace NexusForever.Game.Loot
{
    internal sealed class LootInstanceItem
    {
        public uint LootUnitId { get; }
        public LootResult Result { get; }

        public LootInstanceItem(uint lootUnitId, LootResult result)
        {
            LootUnitId = lootUnitId;
            Result = result;
        }

        public NetworkLootItem BuildNetworkItem()
        {
            uint qualityId = Result.Type == LootItemType.StaticItem
                ? ItemManager.Instance.GetItemInfo(Result.ItemId)?.Entry.ItemQualityId ?? 0u
                : 0u;

            return new NetworkLootItem
            {
                LootUnitId = LootUnitId,
                Type = Result.Type,
                ItemId = Result.ItemId,
                Amount = Result.Amount,
                CanLoot = true,
                ItemQuality2Id = qualityId
            };
        }

        public void Deliver(IPlayer player, uint ownerUnitId)
        {
            switch (Result.Type)
            {
                case LootItemType.Cash:
                    player.CurrencyManager.CurrencyAddAmount((CurrencyType)Result.ItemId, Result.Amount, true);
                    break;
                case LootItemType.StaticItem:
                    player.Inventory.ItemCreate(InventoryLocation.Inventory, Result.ItemId, Result.Amount, ItemUpdateReason.Loot);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported loot type {Result.Type}.");
            }

            player.Session.EnqueueMessageEncrypted(new ServerLootGrant
            {
                OwnerUnitId = ownerUnitId,
                LooterUnitId = player.Guid,
                LootItem = BuildNetworkItem()
            });
        }
    }
}
