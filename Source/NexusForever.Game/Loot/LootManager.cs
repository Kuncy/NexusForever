using System.Collections.Immutable;
using System.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game.Loot
{
    public sealed class LootManager : Singleton<LootManager>
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private const float LootRange = 35f;
        private static readonly TimeSpan LootLifetime = TimeSpan.FromMinutes(30d);

        private ImmutableDictionary<uint, ImmutableList<LootTable>> creatureLoot =
            ImmutableDictionary<uint, ImmutableList<LootTable>>.Empty;
        private readonly Dictionary<uint, LootInstance> lootByUnitId = [];
        private readonly HashSet<LootInstance> lootInstances = [];
        private long nextLootUnitId;

        public void Initialise()
        {
            var sw = Stopwatch.StartNew();
            log.Info("Initialise loot tables...");

            ImmutableList<CreatureLootModel> models = DatabaseManager.Instance
                .GetDatabase<WorldDatabase>()
                .GetCreatureLoot();

            Dictionary<uint, LootTable> tables = models
                .Select(model => model.LootTable)
                .DistinctBy(model => model.Id)
                .ToDictionary(model => model.Id, BuildTable);

            creatureLoot = models
                .GroupBy(model => model.CreatureId)
                .ToImmutableDictionary(
                    group => group.Key,
                    group => group.Select(model => tables[model.LootTableId]).ToImmutableList());

            log.Info($"Cached {tables.Count} loot tables for {creatureLoot.Count} creatures in {sw.ElapsedMilliseconds}ms.");
        }

        public IReadOnlyList<LootResult> GenerateLoot(uint creatureId, Random random = null)
        {
            creatureLoot.TryGetValue(0u, out ImmutableList<LootTable> defaultTables);
            creatureLoot.TryGetValue(creatureId, out ImmutableList<LootTable> creatureTables);

            IEnumerable<LootTable> tables = (defaultTables ?? [])
                .Concat(creatureId == 0u ? [] : creatureTables ?? []);
            if (!tables.Any())
                return [];

            random ??= Random.Shared;
            var results = new List<LootResult>();
            foreach (LootTable table in tables)
                results.AddRange(LootTableRoller.Roll(table, random));

            return results;
        }

        public void DropLoot(IPlayer player, IWorldEntity owner)
        {
            ArgumentNullException.ThrowIfNull(player);
            ArgumentNullException.ThrowIfNull(owner);

            RemoveExpiredLoot();

            IReadOnlyList<LootResult> results = GenerateLoot(owner.CreatureId);
            if (results.Count == 0)
                return;

            List<LootInstanceItem> items = results
                .Select(result => new LootInstanceItem(NextLootUnitId(), result))
                .ToList();
            var instance = new LootInstance(owner.Guid, player.CharacterId, items, LootLifetime);

            lootInstances.Add(instance);
            foreach (LootInstanceItem item in items)
                lootByUnitId.Add(item.LootUnitId, instance);

            instance.SendNotify(player);
        }

        public bool RequestLoot(IPlayer player, uint ownerUnitId, uint lootUnitId)
        {
            if (!TryGetLoot(player, ownerUnitId, lootUnitId, out _, out _))
                return false;

            player.Session.EnqueueMessageEncrypted(new ServerLootCanLoot
            {
                LootUnitId = lootUnitId
            });
            return true;
        }

        public bool GiveLoot(IPlayer player, uint ownerUnitId, uint lootUnitId)
        {
            if (!TryGetLoot(player, ownerUnitId, lootUnitId, out LootInstance instance, out LootInstanceItem item))
                return false;

            item.Deliver(player, instance.OwnerUnitId);
            RemoveLootItem(instance, item);
            return true;
        }

        public void GiveAllLootInRange(IPlayer player)
        {
            ArgumentNullException.ThrowIfNull(player);
            RemoveExpiredLoot();

            foreach (LootInstance instance in lootInstances
                .Where(instance => instance.CharacterId == player.CharacterId)
                .ToList())
            {
                if (!IsInLootRange(player, instance))
                    continue;

                foreach (LootInstanceItem item in instance.Items.ToList())
                {
                    item.Deliver(player, instance.OwnerUnitId);
                    RemoveLootItem(instance, item);
                }
            }
        }

        private bool TryGetLoot(IPlayer player, uint ownerUnitId, uint lootUnitId,
            out LootInstance instance, out LootInstanceItem item)
        {
            instance = null;
            item = null;
            RemoveExpiredLoot();

            if (!lootByUnitId.TryGetValue(lootUnitId, out instance)
                || instance.OwnerUnitId != ownerUnitId
                || instance.CharacterId != player.CharacterId
                || !instance.TryGetItem(lootUnitId, out item))
                return false;

            return IsInLootRange(player, instance);
        }

        private static bool IsInLootRange(IPlayer player, LootInstance instance)
        {
            IWorldEntity owner = player.Map?.GetEntity<IWorldEntity>(instance.OwnerUnitId);
            return owner != null && owner.Position.GetDistance(player.Position) <= LootRange;
        }

        private void RemoveLootItem(LootInstance instance, LootInstanceItem item)
        {
            instance.RemoveItem(item.LootUnitId);
            lootByUnitId.Remove(item.LootUnitId);

            if (!instance.IsEmpty)
                return;

            lootInstances.Remove(instance);
        }

        private void RemoveExpiredLoot()
        {
            foreach (LootInstance instance in lootInstances.Where(instance => instance.IsExpired).ToList())
            {
                foreach (LootInstanceItem item in instance.Items)
                    lootByUnitId.Remove(item.LootUnitId);
                lootInstances.Remove(instance);
            }
        }

        private uint NextLootUnitId()
        {
            uint id;
            do
            {
                id = unchecked((uint)Interlocked.Increment(ref nextLootUnitId));
            }
            while (id == 0u || lootByUnitId.ContainsKey(id));

            return id;
        }

        private static LootTable BuildTable(LootTableModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            List<LootTableEntryModel> entries = model.Entries
                .OrderBy(entry => entry.Id)
                .ToList();

            foreach (LootTableEntryModel entry in entries)
                ValidateEntry(model.Id, entry);

            foreach (IGrouping<uint, LootTableEntryModel> group in entries.Where(entry => entry.GroupId != 0u).GroupBy(entry => entry.GroupId))
            {
                ulong totalChance = group.Aggregate<LootTableEntryModel, ulong>(0u, (total, entry) => total + entry.Chance);
                if (totalChance > LootTableRoller.ChanceScale)
                    throw new InvalidOperationException($"Loot table {model.Id}, group {group.Key} has a total chance above {LootTableRoller.ChanceScale}.");
            }

            return new LootTable(
                model.Id,
                entries.Where(entry => entry.GroupId == 0u).Select(BuildEntry).ToImmutableList(),
                entries.Where(entry => entry.GroupId != 0u)
                    .GroupBy(entry => entry.GroupId)
                    .OrderBy(group => group.Key)
                    .Select(group => group.Select(BuildEntry).ToImmutableList())
                    .ToImmutableList());
        }

        private static LootTableEntry BuildEntry(LootTableEntryModel entry)
        {
            return new LootTableEntry(entry.Id, entry.Type, entry.ItemId, entry.MinAmount, entry.MaxAmount, entry.Chance);
        }

        private static void ValidateEntry(uint tableId, LootTableEntryModel entry)
        {
            if (entry.MinAmount == 0u || entry.MaxAmount < entry.MinAmount)
                throw new InvalidOperationException($"Loot table {tableId}, entry {entry.Id} has an invalid amount range.");
            if (entry.Chance > LootTableRoller.ChanceScale)
                throw new InvalidOperationException($"Loot table {tableId}, entry {entry.Id} has an invalid chance.");
            if (entry.ObservedDrops.HasValue != entry.ObservedAttempts.HasValue)
                throw new InvalidOperationException($"Loot table {tableId}, entry {entry.Id} has incomplete observation data.");
            if (entry.ObservedAttempts == 0u)
                throw new InvalidOperationException($"Loot table {tableId}, entry {entry.Id} has no observed attempts.");
            if (entry.ObservedDrops > entry.ObservedAttempts)
                throw new InvalidOperationException($"Loot table {tableId}, entry {entry.Id} has more observed drops than attempts.");
            if (entry.ObservedDrops.HasValue)
            {
                uint observedChance = (uint)Math.Round(
                    (double)entry.ObservedDrops.Value / entry.ObservedAttempts.Value * LootTableRoller.ChanceScale);
                if (entry.Chance != observedChance)
                    log.Warn($"Loot table {tableId}, entry {entry.Id} uses chance {entry.Chance}, observed source chance is {observedChance}.");
            }

            switch (entry.Type)
            {
                case LootItemType.StaticItem when entry.ItemId == 0u:
                    throw new InvalidOperationException($"Loot table {tableId}, entry {entry.Id} has no item id.");
                case LootItemType.StaticItem when ItemManager.Instance.GetItemInfo(entry.ItemId) == null:
                    throw new InvalidOperationException($"Loot table {tableId}, entry {entry.Id} references unknown item {entry.ItemId}.");
                case LootItemType.StaticItem:
                    break;
                case LootItemType.Cash:
                    if (entry.ItemId == (uint)CurrencyType.None || !Enum.IsDefined((CurrencyType)entry.ItemId))
                        throw new InvalidOperationException($"Loot table {tableId}, entry {entry.Id} references unknown currency {entry.ItemId}.");
                    break;
                default:
                    throw new InvalidOperationException($"Loot table {tableId}, entry {entry.Id} uses unsupported loot type {entry.Type}.");
            }
        }
    }
}
