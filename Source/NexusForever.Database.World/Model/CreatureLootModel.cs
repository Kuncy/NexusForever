namespace NexusForever.Database.World.Model
{
    public class CreatureLootModel
    {
        public uint CreatureId { get; set; }
        public uint LootTableId { get; set; }

        public LootTableModel LootTable { get; set; }
    }
}
