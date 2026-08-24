using NexusForever.Game.Static.Loot;

namespace NexusForever.Database.World.Model
{
    public class LootTableEntryModel
    {
        public uint Id { get; set; }
        public uint LootTableId { get; set; }
        public LootItemType Type { get; set; }
        public uint ItemId { get; set; }
        public uint MinAmount { get; set; }
        public uint MaxAmount { get; set; }
        public uint Chance { get; set; }
        public uint GroupId { get; set; }
        public string Source { get; set; }
        public string SourceVersion { get; set; }
        public uint? ObservedDrops { get; set; }
        public uint? ObservedAttempts { get; set; }

        public LootTableModel LootTable { get; set; }
    }
}
