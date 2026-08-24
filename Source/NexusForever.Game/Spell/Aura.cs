using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Spell
{
    /// <summary>
    /// Base implementation of <see cref="IAura"/> that owns the countdown.
    /// </summary>
    /// <remarks>
    /// Effects derive from this and override <see cref="OnApply"/>, <see cref="OnTick"/> and <see cref="OnRemove"/>.
    /// </remarks>
    public abstract class Aura : IAura
    {
        public AuraKey Key { get; }
        public uint CastingId { get; }
        public uint EffectId { get; }
        public IUnitEntity Caster { get; }
        public double Remaining { get; private set; }
        public bool IsActive { get; private set; }

        private readonly double duration;

        /// <param name="duration">
        /// Duration in seconds, anything at or below zero produces an aura that never expires on its own and has to
        /// be removed explicitly.
        /// </param>
        protected Aura(AuraKey key, IUnitEntity caster, uint castingId, uint effectId, double duration)
        {
            Key       = key;
            Caster    = caster;
            CastingId = castingId;
            EffectId  = effectId;

            this.duration = duration > 0d ? duration : double.PositiveInfinity;
            Remaining     = this.duration;
        }

        public void Refresh()
        {
            Remaining = duration;
        }

        public void Apply(IUnitEntity target)
        {
            IsActive = true;
            OnApply(target);
        }

        public bool Update(IUnitEntity target, double lastTick)
        {
            OnTick(target, lastTick);

            if (double.IsPositiveInfinity(Remaining))
                return true;

            Remaining -= lastTick;
            return Remaining > 0d;
        }

        public void Remove(IUnitEntity target, AuraRemoveReason reason)
        {
            IsActive = false;
            OnRemove(target, reason);
        }

        protected virtual void OnApply(IUnitEntity target)
        {
        }

        protected virtual void OnTick(IUnitEntity target, double lastTick)
        {
        }

        protected virtual void OnRemove(IUnitEntity target, AuraRemoveReason reason)
        {
        }
    }
}
