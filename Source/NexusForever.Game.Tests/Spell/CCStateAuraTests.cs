using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Auras;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Spell;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Entity;
using NSubstitute;

namespace NexusForever.Game.Tests.Spell
{
    public class CCStateAuraTests
    {
        private const uint CastingId = 4242u;
        private const uint EffectId  = 77u;

        private static (AuraManager manager, IUnitEntity target, CCStateAura aura, List<IWritable> sent) Create(
            CCState state = CCState.Stun, double duration = 4d)
        {
            var target = Substitute.For<IUnitEntity>();
            target.Guid.Returns(12u);

            // expression trees cannot hold a pattern match, so capture what was sent and assert on it afterwards
            var sent = new List<IWritable>();
            target.WhenForAnyArgs(t => t.EnqueueToVisible(null, false))
                .Do(call => sent.Add(call.Arg<IWritable>()));

            var aura = new CCStateAura(new AuraKey(1u, 1u, 9u), Substitute.For<IUnitEntity>(),
                CastingId, EffectId, duration, state);

            return (new AuraManager(target), target, aura, sent);
        }

        [Fact]
        public void ExpiryTellsTheClientWhichEffectEnded()
        {
            (AuraManager manager, _, CCStateAura aura, List<IWritable> sent) = Create(CCState.Stun, 4d);
            manager.Apply(aura);

            manager.Update(4d);

            // the ids have to match the ones the client saw in ServerSpellGo
            ServerEntityCCStateRemove remove = Assert.IsType<ServerEntityCCStateRemove>(Assert.Single(sent));
            Assert.Equal(12u, remove.UnitId);
            Assert.Equal(CCState.Stun, remove.CCType);
            Assert.Equal(CastingId, remove.SpellCastUniqueId);
            Assert.Equal(EffectId, remove.SpellEffectUniqueId);
            Assert.True(remove.Removed);
        }

        [Fact]
        public void ApplyingCrowdControlInterruptsACastInProgress()
        {
            (AuraManager manager, IUnitEntity target, CCStateAura aura, _) = Create(CCState.Stun, 4d);

            manager.Apply(aura);

            // the condition check only guards the start of a cast, the aura has to deal with one already running
            target.Received(1).CancelCastsBlockedByCCState();
        }

        [Fact]
        public void NothingIsSentWhileTheStateIsStillRunning()
        {
            (AuraManager manager, _, CCStateAura aura, List<IWritable> sent) = Create(CCState.Stun, 4d);
            manager.Apply(aura);

            manager.Update(3d);

            Assert.Empty(sent);
        }

        [Fact]
        public void RemovingEarlyAlsoTellsTheClient()
        {
            (AuraManager manager, _, CCStateAura aura, List<IWritable> sent) = Create(CCState.Silence, 10d);
            manager.Apply(aura);

            manager.Remove(aura.Key, AuraRemoveReason.Dispelled);

            ServerEntityCCStateRemove remove = Assert.IsType<ServerEntityCCStateRemove>(Assert.Single(sent));
            Assert.Equal(CCState.Silence, remove.CCType);
        }

        /// <remarks>
        /// Guards the mapping the cast check relies on: every <see cref="CCState"/> has a CC prefixed
        /// <see cref="Network.World.Message.Static.CastResult"/> naming it, so a blocked cast can tell the player
        /// which state stopped them.
        /// </remarks>
        [Fact]
        public void EveryCCStateHasACastResult()
        {
            foreach (CCState state in Enum.GetValues<CCState>())
                Assert.True(
                    Enum.TryParse($"CC{state}", out Network.World.Message.Static.CastResult _),
                    $"CCState {state} has no matching CastResult");
        }
    }
}
