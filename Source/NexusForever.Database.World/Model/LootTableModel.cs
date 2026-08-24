using System.Collections.Generic;

namespace NexusForever.Database.World.Model
{
    public class LootTableModel
    {
        public uint Id { get; set; }
        public string Description { get; set; }

        public ICollection<CreatureLootModel> CreatureLoot { get; set; } = [];
        public ICollection<LootTableEntryModel> Entries { get; set; } = [];
    }
}
