using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Map.Search;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;

namespace NexusForever.Game.Party
{
    /// <summary>
    /// Resolves party reward eligibility and applies rewards shared by nearby party members.
    /// </summary>
    public sealed class PartyRewardManager : Singleton<PartyRewardManager>
    {
        public IReadOnlyList<PartyRewardGroup> GetRewardGroups(
            IWorldEntity source,
            IEnumerable<IPlayer> participants)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(participants);

            IPlayer[] participantArray = participants
                .DistinctBy(player => player.CharacterId)
                .ToArray();
            var rewardGroups = new List<PartyRewardGroup>();

            foreach (IPlayer player in participantArray.Where(player => player.GroupAssociation == 0u))
                rewardGroups.Add(new PartyRewardGroup(0u, [player], [player]));

            foreach (IGrouping<ulong, IPlayer> party in participantArray
                .Where(player => player.GroupAssociation != 0u)
                .GroupBy(player => player.GroupAssociation))
            {
                IPlayer[] members = source.Map.Search(
                        source.Position,
                        source.Map.VisionRange,
                        new SearchCheckRange<IPlayer>(source.Position, source.Map.VisionRange))
                    .Where(player => player.GroupAssociation == party.Key)
                    .DistinctBy(player => player.CharacterId)
                    .OrderBy(player => player.CharacterId)
                    .ToArray();

                if (members.Length == 0)
                    continue;

                rewardGroups.Add(new PartyRewardGroup(party.Key, party.ToArray(), members));
            }

            return rewardGroups;
        }

        /// <summary>
        /// Divide each player's level-adjusted kill experience by the number of nearby party members.
        /// </summary>
        public void RewardKillExperience(
            IWorldEntity source,
            IEnumerable<IPlayer> participants,
            Func<IPlayer, uint> calculateExperience)
        {
            ArgumentNullException.ThrowIfNull(calculateExperience);

            foreach (PartyRewardGroup rewardGroup in GetRewardGroups(source, participants))
            {
                uint divisor = (uint)rewardGroup.Members.Count;
                foreach (IPlayer member in rewardGroup.Members)
                {
                    uint experience = calculateExperience(member);
                    if (experience > 0u)
                        experience = Math.Max(1u, experience / divisor);

                    member.XpManager.GrantXp(experience, ExpReason.KillCreature);
                }
            }
        }
    }
}
