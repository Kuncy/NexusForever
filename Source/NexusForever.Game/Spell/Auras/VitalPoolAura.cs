using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Spell.Auras
{
    /// <summary>
    /// Adds an amount to a pool on the target and takes the same amount back off when the effect ends.
    /// </summary>
    /// <remarks>
    /// Covers absorption and interrupt armor, both of which are plain counters that several effects contribute to,
    /// so the aura only ever subtracts what it added and never drives the pool below zero.
    /// </remarks>
    public class VitalPoolAura : Aura
    {
        private readonly Func<IUnitEntity, uint> get;
        private readonly Action<IUnitEntity, uint> set;
        private readonly uint amount;

        public VitalPoolAura(AuraKey key, IUnitEntity caster, uint castingId, uint effectId, double duration,
            uint amount, Func<IUnitEntity, uint> get, Action<IUnitEntity, uint> set)
            : base(key, caster, castingId, effectId, duration)
        {
            this.amount = amount;
            this.get    = get;
            this.set    = set;
        }

        protected override void OnApply(IUnitEntity target)
        {
            set.Invoke(target, get.Invoke(target) + amount);
        }

        protected override void OnRemove(IUnitEntity target, AuraRemoveReason reason)
        {
            uint current = get.Invoke(target);
            set.Invoke(target, current > amount ? current - amount : 0u);
        }
    }
}
