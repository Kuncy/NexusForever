using NexusForever.Game.Static.Loot;

namespace NexusForever.Game.Loot
{
    internal sealed record LootTableEntry(uint Id, LootItemType Type, uint ItemId, uint MinAmount,
        uint MaxAmount, uint Chance);
}
