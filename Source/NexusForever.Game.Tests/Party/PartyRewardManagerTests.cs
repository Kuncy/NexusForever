using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Party;
using NSubstitute;

namespace NexusForever.Game.Tests.Party
{
    /// <remarks>
    /// The geometric range filter itself is covered by <see cref="Map.SearchCheckRangeTests"/>. These tests pin
    /// down what the reward grouping does with a search result: who forms a group, who is deduplicated, and which
    /// contributors are dropped because no party member is nearby.
    /// </remarks>
    public class PartyRewardManagerTests
    {
        private static IPlayer CreatePlayer(ulong characterId, ulong groupAssociation = 0ul)
        {
            var player = Substitute.For<IPlayer>();
            player.CharacterId.Returns(characterId);
            player.GroupAssociation.Returns(groupAssociation);
            return player;
        }

        /// <summary>
        /// Build a kill source whose map search returns <paramref name="nearby"/>.
        /// </summary>
        private static IWorldEntity CreateSource(params IPlayer[] nearby)
        {
            var map = Substitute.For<IBaseMap>();
            map.VisionRange.Returns(150f);
            map.Search(Arg.Any<Vector3>(), Arg.Any<float?>(), Arg.Any<ISearchCheck<IPlayer>>())
                .Returns(nearby);

            var source = Substitute.For<IWorldEntity>();
            source.Map.Returns(map);
            source.Position.Returns(Vector3.Zero);
            return source;
        }

        [Fact]
        public void SoloParticipantFormsItsOwnGroupWithoutASearch()
        {
            IPlayer solo = CreatePlayer(1ul);
            IWorldEntity source = CreateSource();

            PartyRewardGroup group = Assert.Single(
                PartyRewardManager.Instance.GetRewardGroups(source, [solo]));

            Assert.Equal(0ul, group.PartyId);
            Assert.Equal([solo], group.Contributors);
            Assert.Equal([solo], group.Members);
        }

        [Fact]
        public void EachSoloParticipantGetsASeparateGroup()
        {
            IPlayer first = CreatePlayer(1ul);
            IPlayer second = CreatePlayer(2ul);
            IWorldEntity source = CreateSource();

            IReadOnlyList<PartyRewardGroup> groups =
                PartyRewardManager.Instance.GetRewardGroups(source, [first, second]);

            Assert.Equal(2, groups.Count);
            Assert.All(groups, group => Assert.Single(group.Members));
        }

        [Fact]
        public void DuplicateParticipantsAreCountedOnce()
        {
            IPlayer solo = CreatePlayer(1ul);
            IWorldEntity source = CreateSource();

            // The same player hitting a creature with several spells must not be rewarded twice.
            PartyRewardGroup group = Assert.Single(
                PartyRewardManager.Instance.GetRewardGroups(source, [solo, solo, solo]));

            Assert.Single(group.Members);
        }

        [Fact]
        public void DistinctInstancesOfTheSameCharacterAreCountedOnce()
        {
            IWorldEntity source = CreateSource();

            IReadOnlyList<PartyRewardGroup> groups = PartyRewardManager.Instance
                .GetRewardGroups(source, [CreatePlayer(1ul), CreatePlayer(1ul)]);

            Assert.Single(groups);
        }

        [Fact]
        public void PartyMembersInRangeShareTheGroupEvenWithoutContributing()
        {
            IPlayer contributor = CreatePlayer(1ul, groupAssociation: 99ul);
            IPlayer bystander = CreatePlayer(2ul, groupAssociation: 99ul);
            IWorldEntity source = CreateSource(contributor, bystander);

            PartyRewardGroup group = Assert.Single(
                PartyRewardManager.Instance.GetRewardGroups(source, [contributor]));

            Assert.Equal(99ul, group.PartyId);
            Assert.Equal([contributor], group.Contributors);
            Assert.Equal([contributor, bystander], group.Members);
        }

        [Fact]
        public void MembersOfAnotherPartyInRangeAreExcluded()
        {
            IPlayer contributor = CreatePlayer(1ul, groupAssociation: 99ul);
            IPlayer stranger = CreatePlayer(2ul, groupAssociation: 100ul);
            IPlayer unaffiliated = CreatePlayer(3ul);
            IWorldEntity source = CreateSource(contributor, stranger, unaffiliated);

            PartyRewardGroup group = Assert.Single(
                PartyRewardManager.Instance.GetRewardGroups(source, [contributor]));

            Assert.Equal([contributor], group.Members);
        }

        [Fact]
        public void PartyWithNoMemberInRangeIsSkipped()
        {
            // The contributor left the area (or changed map) before the creature died.
            IPlayer contributor = CreatePlayer(1ul, groupAssociation: 99ul);
            IWorldEntity source = CreateSource();

            Assert.Empty(PartyRewardManager.Instance.GetRewardGroups(source, [contributor]));
        }

        [Fact]
        public void MembersAreOrderedDeterministically()
        {
            IPlayer third = CreatePlayer(3ul, groupAssociation: 99ul);
            IPlayer first = CreatePlayer(1ul, groupAssociation: 99ul);
            IPlayer second = CreatePlayer(2ul, groupAssociation: 99ul);
            IWorldEntity source = CreateSource(third, first, second);

            PartyRewardGroup group = Assert.Single(
                PartyRewardManager.Instance.GetRewardGroups(source, [first]));

            // Currency shares are handed out by member index, so the order must not depend on search order.
            Assert.Equal([1ul, 2ul, 3ul], group.Members.Select(member => member.CharacterId));
        }

        [Fact]
        public void DuplicatePartyMembersFromTheSearchAreCountedOnce()
        {
            IPlayer contributor = CreatePlayer(1ul, groupAssociation: 99ul);
            IWorldEntity source = CreateSource(contributor, contributor);

            PartyRewardGroup group = Assert.Single(
                PartyRewardManager.Instance.GetRewardGroups(source, [contributor]));

            Assert.Single(group.Members);
        }

        [Fact]
        public void SoloAndPartyParticipantsFormSeparateGroups()
        {
            IPlayer solo = CreatePlayer(1ul);
            IPlayer partyMember = CreatePlayer(2ul, groupAssociation: 99ul);
            IWorldEntity source = CreateSource(partyMember);

            IReadOnlyList<PartyRewardGroup> groups =
                PartyRewardManager.Instance.GetRewardGroups(source, [solo, partyMember]);

            Assert.Equal(2, groups.Count);
            Assert.Single(groups, group => group.PartyId == 0ul);
            Assert.Single(groups, group => group.PartyId == 99ul);
        }

        [Fact]
        public void TwoPartiesOnTheSameKillEachGetTheirOwnGroup()
        {
            IPlayer alliance = CreatePlayer(1ul, groupAssociation: 99ul);
            IPlayer rival = CreatePlayer(2ul, groupAssociation: 100ul);
            IWorldEntity source = CreateSource(alliance, rival);

            IReadOnlyList<PartyRewardGroup> groups =
                PartyRewardManager.Instance.GetRewardGroups(source, [alliance, rival]);

            Assert.Equal(2, groups.Count);
            Assert.All(groups, group => Assert.Single(group.Members));
        }

        [Fact]
        public void NullArgumentsAreRejected()
        {
            IWorldEntity source = CreateSource();

            Assert.Throws<ArgumentNullException>(() => PartyRewardManager.Instance.GetRewardGroups(null, []));
            Assert.Throws<ArgumentNullException>(() => PartyRewardManager.Instance.GetRewardGroups(source, null));
        }
    }
}
