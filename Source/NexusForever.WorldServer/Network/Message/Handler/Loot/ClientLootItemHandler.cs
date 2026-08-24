using NexusForever.Game.Loot;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Loot;

namespace NexusForever.WorldServer.Network.Message.Handler.Loot
{
    public class ClientLootItemHandler : IMessageHandler<IWorldSession, ClientLootItem>
    {
        public void HandleMessage(IWorldSession session, ClientLootItem message)
        {
            if (message.Request)
                LootManager.Instance.RequestLoot(session.Player, message.OwnerUnitId, message.LootUnitId);
            else
                LootManager.Instance.GiveLoot(session.Player, message.OwnerUnitId, message.LootUnitId);
        }
    }
}
