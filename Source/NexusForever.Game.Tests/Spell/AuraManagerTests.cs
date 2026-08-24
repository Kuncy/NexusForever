using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Spell;
using NSubstitute;

namespace NexusForever.Game.Tests.Spell
{
    /// <remarks>
    /// The refresh behaviour below cannot be reached from the client: no player ability up to level 20 has a
    /// cooldown shorter than the duration of the buff it applies, so a buff can never be recast while it is still
    /// running. These tests are the only place the path is exercised.
    /// </remarks>
    public class AuraManagerTests
    {
        /// <summary>
        /// Aura that records what the manager did to it.
        /// </summary>
        private sealed class RecordingAura : Aura
        {
            public int Applied { get; private set; }
            public int Ticks { get; private set; }
            public int Removed { get; private set; }
            public AuraRemoveReason? LastReason { get; private set; }

            public RecordingAura(AuraKey key, IUnitEntity caster, double duration)
                : base(key, caster, castingId: 1u, effectId: 2u, duration)
            {
            }

            protected override void OnApply(IUnitEntity target) => Applied++;

            protected override void OnTick(IUnitEntity target, double lastTick) => Ticks++;

            protected override void OnRemove(IUnitEntity target, AuraRemoveReason reason)
            {
                Removed++;
                LastReason = reason;
            }
        }

        private static readonly AuraKey Key      = new(Spell4Id: 100u, EffectEntryId: 1u, CasterGuid: 7u);
        private static readonly AuraKey OtherKey = new(Spell4Id: 100u, EffectEntryId: 2u, CasterGuid: 7u);

        private static (AuraManager manager, IUnitEntity owner) Create()
        {
            var owner = Substitute.For<IUnitEntity>();
            return (new AuraManager(owner), owner);
        }

        [Fact]
        public void AppliedAuraIsActiveAndAppliedOnce()
        {
            (AuraManager manager, IUnitEntity owner) = Create();
            var aura = new RecordingAura(Key, owner, 10d);

            manager.Apply(aura);

            Assert.Same(aura, manager.Get(Key));
            Assert.True(aura.IsActive);
            Assert.Equal(1, aura.Applied);
        }

        [Fact]
        public void AuraExpiresAfterItsDuration()
        {
            (AuraManager manager, IUnitEntity owner) = Create();
            var aura = new RecordingAura(Key, owner, 10d);
            manager.Apply(aura);

            manager.Update(9d);
            Assert.True(aura.IsActive);
            Assert.NotNull(manager.Get(Key));

            manager.Update(1d);
            Assert.False(aura.IsActive);
            Assert.Null(manager.Get(Key));
            Assert.Equal(1, aura.Removed);
            Assert.Equal(AuraRemoveReason.Expired, aura.LastReason);
        }

        /// <remarks>
        /// The bug this guards against: the previous implementation queued a second removal on recast, so a buff
        /// recast at 8s of a 10s duration died at 10s instead of at 18s.
        /// </remarks>
        [Fact]
        public void RecastRefreshesInsteadOfExpiringEarly()
        {
            (AuraManager manager, IUnitEntity owner) = Create();
            var first = new RecordingAura(Key, owner, 10d);
            manager.Apply(first);

            manager.Update(8d);
            manager.Apply(new RecordingAura(Key, owner, 10d));

            // the original 10 seconds would have run out here
            manager.Update(2d);
            Assert.True(first.IsActive);
            Assert.Equal(0, first.Removed);

            manager.Update(8d);
            Assert.False(first.IsActive);
            Assert.Equal(AuraRemoveReason.Expired, first.LastReason);
        }

        [Fact]
        public void RefreshKeepsTheOriginalAuraAndDoesNotReapply()
        {
            (AuraManager manager, IUnitEntity owner) = Create();
            var first = new RecordingAura(Key, owner, 10d);
            var second = new RecordingAura(Key, owner, 10d);
            manager.Apply(first);

            IAura result = manager.Apply(second);

            Assert.Same(first, result);
            Assert.Equal(1, first.Applied);
            Assert.Equal(0, first.Removed);
            Assert.Equal(0, second.Applied);
            Assert.False(second.IsActive);
        }

        [Fact]
        public void AurasWithDifferentKeysCoexist()
        {
            (AuraManager manager, IUnitEntity owner) = Create();
            var first = new RecordingAura(Key, owner, 10d);
            var second = new RecordingAura(OtherKey, owner, 4d);
            manager.Apply(first);
            manager.Apply(second);

            manager.Update(5d);

            Assert.True(first.IsActive);
            Assert.False(second.IsActive);
            Assert.Equal(AuraRemoveReason.Expired, second.LastReason);
        }

        [Fact]
        public void AuraWithoutDurationNeverExpiresOnItsOwn()
        {
            (AuraManager manager, IUnitEntity owner) = Create();
            var aura = new RecordingAura(Key, owner, 0d);
            manager.Apply(aura);

            manager.Update(10_000d);

            Assert.True(aura.IsActive);
            Assert.Equal(0, aura.Removed);

            Assert.True(manager.Remove(Key, AuraRemoveReason.Dispelled));
            Assert.False(aura.IsActive);
            Assert.Equal(AuraRemoveReason.Dispelled, aura.LastReason);
        }

        [Fact]
        public void RemoveReportsWhetherAnAuraWasPresent()
        {
            (AuraManager manager, IUnitEntity owner) = Create();

            Assert.False(manager.Remove(Key, AuraRemoveReason.Dispelled));

            manager.Apply(new RecordingAura(Key, owner, 10d));
            Assert.True(manager.Remove(Key, AuraRemoveReason.Dispelled));
            Assert.False(manager.Remove(Key, AuraRemoveReason.Dispelled));
        }

        [Fact]
        public void RemoveByCasterOnlyRemovesThatCastersAuras()
        {
            (AuraManager manager, IUnitEntity owner) = Create();
            var mine = Substitute.For<IUnitEntity>();
            mine.Guid.Returns(7u);
            var theirs = Substitute.For<IUnitEntity>();
            theirs.Guid.Returns(9u);

            var ours = new RecordingAura(Key, mine, 10d);
            var others = new RecordingAura(new AuraKey(100u, 1u, 9u), theirs, 10d);
            manager.Apply(ours);
            manager.Apply(others);

            manager.RemoveByCaster(7u, AuraRemoveReason.CasterGone);

            Assert.False(ours.IsActive);
            Assert.Equal(AuraRemoveReason.CasterGone, ours.LastReason);
            Assert.True(others.IsActive);
        }

        [Fact]
        public void ExpiredAuraStopsTicking()
        {
            (AuraManager manager, IUnitEntity owner) = Create();
            var aura = new RecordingAura(Key, owner, 1d);
            manager.Apply(aura);

            manager.Update(1d);
            int ticksAtExpiry = aura.Ticks;

            manager.Update(1d);
            Assert.Equal(ticksAtExpiry, aura.Ticks);
        }
    }
}
