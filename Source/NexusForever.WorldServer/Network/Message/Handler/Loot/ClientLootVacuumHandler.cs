using NexusForever.Game.Loot;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Loot;

namespace NexusForever.WorldServer.Network.Message.Handler.Loot
{
    public class ClientLootVacuumHandler : IMessageHandler<IWorldSession, ClientLootVacuum>
    {
        public void HandleMessage(IWorldSession session, ClientLootVacuum message)
        {
            LootManager.Instance.GiveAllLootInRange(session.Player);
        }
    }
}
