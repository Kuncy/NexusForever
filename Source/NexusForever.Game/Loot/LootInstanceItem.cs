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
        public LootResult Result { get; private set; }

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

        /// <summary>
        /// Hand this loot to the supplied <see cref="IPlayer"/>.
        /// </summary>
        /// <remarks>
        /// A full bag only consumes what actually fit. The remaining amount stays on this item so the
        /// loot entry survives and can be picked up again once the player has made room.
        /// </remarks>
        /// <returns><c>true</c> when the whole amount was delivered.</returns>
        public bool Deliver(IPlayer player, uint ownerUnitId)
        {
            uint delivered;
            switch (Result.Type)
            {
                case LootItemType.Cash:
                    // Currencies are capped rather than rejected, so cash is always fully consumed.
                    player.CurrencyManager.CurrencyAddAmount((CurrencyType)Result.ItemId, Result.Amount, true);
                    delivered = Result.Amount;
                    break;
                case LootItemType.StaticItem:
                    uint remaining = player.Inventory.ItemCreate(
                        InventoryLocation.Inventory, Result.ItemId, Result.Amount, ItemUpdateReason.Loot);
                    delivered = Result.Amount - remaining;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported loot type {Result.Type}.");
            }

            if (delivered == 0u)
                return false;

            // Report only what was actually granted, otherwise the client removes the whole stack
            // from the loot window while part of it is still pending.
            NetworkLootItem grantedItem = BuildNetworkItem();
            grantedItem.Amount = delivered;

            player.Session.EnqueueMessageEncrypted(new ServerLootGrant
            {
                OwnerUnitId = ownerUnitId,
                LooterUnitId = player.Guid,
                LootItem = grantedItem
            });

            Result = Result with { Amount = Result.Amount - delivered };
            return Result.Amount == 0u;
        }
    }
}
