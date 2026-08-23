using System.Linq;
using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Quest;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    public class ClientActivateUnitCastHandler : IMessageHandler<IWorldSession, ClientActivateUnitCast>
    {
        public void HandleMessage(IWorldSession session, ClientActivateUnitCast activateUnitCast)
        {
            IWorldEntity entity = session.Player.GetVisible<IWorldEntity>(activateUnitCast.ActivateUnitId);
            if (entity == null)
                throw new InvalidPacketValueException();

            float maximumRange = entity.CreatureEntry.ActivateSpellMaxRange > 1f
                ? entity.CreatureEntry.ActivateSpellMaxRange
                : 1f;
            // Client movement and the authoritative world position can differ
            // by several metres. Allow a small activation tolerance and never
            // disconnect a player for a stale/out-of-range interaction.
            maximumRange += 6f;
            if (Vector3.DistanceSquared(session.Player.Position, entity.Position) > maximumRange * maximumRange)
                return;

            uint[] spells =
            [
                entity.CreatureEntry.Spell4IdActivate00,
                entity.CreatureEntry.Spell4IdActivate01,
                entity.CreatureEntry.Spell4IdActivate02,
                entity.CreatureEntry.Spell4IdActivate03
            ];
            uint[] prerequisites =
            [
                entity.CreatureEntry.PrerequisiteIdActivateSpell00,
                entity.CreatureEntry.PrerequisiteIdActivateSpell01,
                entity.CreatureEntry.PrerequisiteIdActivateSpell02,
                entity.CreatureEntry.PrerequisiteIdActivateSpell03
            ];

            uint spell4Id = 0u;
            for (int i = 0; i < spells.Length; i++)
            {
                if (spells[i] == 0u)
                    continue;
                if (prerequisites[i] != 0u && !PrerequisiteManager.Instance.Meets(session.Player, prerequisites[i]))
                    continue;

                spell4Id = spells[i];
                break;
            }

            if (spell4Id == 0u)
                throw new InvalidPacketValueException();

            bool questEntityActivation = session.Player.QuestManager.GetActiveQuests()
                .Where(q => q.State == QuestState.Accepted)
                .SelectMany(q => q)
                .Any(o => !o.IsComplete()
                    && o.ObjectiveInfo.Entry.Data == entity.CreatureId
                    && o.ObjectiveInfo.Type is QuestObjectiveType.ActivateEntity
                        or QuestObjectiveType.ActivateEntity2
                        or QuestObjectiveType.SucceedCSI);
            if (questEntityActivation && !session.Player.TryBeginQuestEntityActivation(entity.Guid))
                return;

            session.Player.CastSpell(spell4Id, new SpellParameters
            {
                PrimaryTargetId        = entity.Guid,
                ActivationTargetGuid   = entity.Guid,
                QuestEntityActivation  = questEntityActivation,
                ClientUniqueId         = activateUnitCast.ClientUniqueId,
                CastTimeOverride       = (int)entity.CreatureEntry.ActivateSpellCastTime,
                UserInitiatedSpellCast = true
            });
        }
    }
}
