using NexusForever.Game.Static.Loot;

namespace NexusForever.Game.Loot
{
    public sealed record LootResult(uint EntryId, LootItemType Type, uint ItemId, uint Amount);
}
