using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Model.Loot;

namespace NexusForever.Game.Loot
{
    internal sealed class LootInstance
    {
        public uint OwnerUnitId { get; }
        public ulong CharacterId { get; }
        public DateTime ExpiresAt { get; }
        public IReadOnlyCollection<LootInstanceItem> Items => items.Values;
        public bool IsEmpty => items.Count == 0;
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        private readonly Dictionary<uint, LootInstanceItem> items;

        public LootInstance(uint ownerUnitId, ulong characterId, IEnumerable<LootInstanceItem> items, TimeSpan lifetime)
        {
            OwnerUnitId = ownerUnitId;
            CharacterId = characterId;
            ExpiresAt = DateTime.UtcNow.Add(lifetime);
            this.items = items.ToDictionary(item => item.LootUnitId);
        }

        public bool TryGetItem(uint lootUnitId, out LootInstanceItem item)
        {
            return items.TryGetValue(lootUnitId, out item);
        }

        public bool RemoveItem(uint lootUnitId)
        {
            return items.Remove(lootUnitId);
        }

        public void SendNotify(IPlayer player)
        {
            player.Session.EnqueueMessageEncrypted(new ServerLootNotify
            {
                OwnerUnitId = OwnerUnitId,
                LootItems = items.Values.Select(item => item.BuildNetworkItem()).ToList()
            });
        }
    }
}
