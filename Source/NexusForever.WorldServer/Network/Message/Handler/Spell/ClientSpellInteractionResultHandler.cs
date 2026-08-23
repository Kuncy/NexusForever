using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientSpellInteractionResultHandler : IMessageHandler<IWorldSession, ClientSpellInteractionResult>
    {
        public void HandleMessage(IWorldSession session, ClientSpellInteractionResult result)
        {
            if (session.Player is not UnitEntity unit)
                return;

            ISpell spell = unit.GetActiveSpell(s => s.CastingId == result.CastingId);
            if (spell == null)
                return;

            switch (result.Result)
            {
                case 1:
                    spell.SucceedClientInteraction();
                    break;
                case 0:
                    spell.FailClientInteraction();
                    break;
                case 2 when spell.IsCasting:
                    spell.CancelCast(CastResult.ClientSideInteractionFail);
                    break;
            }
        }
    }
}
