using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Abstract.Spell
{
    /// <summary>
    /// A timed effect applied to an <see cref="IUnitEntity"/> by a spell.
    /// </summary>
    /// <remarks>
    /// An aura outlives the <see cref="ISpell"/> that created it, the spell applies it and is then free to finish.
    /// This interface is the contract with <see cref="IAuraManager"/>, effects extend the behaviour by overriding
    /// the hooks on the base implementation rather than by implementing this directly.
    /// </remarks>
    public interface IAura
    {
        /// <summary>
        /// Identity of the aura on its target.
        /// </summary>
        /// <remarks>
        /// An aura applied again under the same key refreshes the existing one rather than stacking beside it.
        /// </remarks>
        AuraKey Key { get; }

        /// <summary>
        /// Unique id of the cast that applied the aura, matches the id sent in ServerSpellGo.
        /// </summary>
        /// <remarks>
        /// Required together with <see cref="EffectId"/> to tell the client which effect ended.
        /// </remarks>
        uint CastingId { get; }

        /// <summary>
        /// Unique id of the effect that applied the aura, matches the id sent in ServerSpellGo.
        /// </summary>
        uint EffectId { get; }

        /// <summary>
        /// <see cref="IUnitEntity"/> that applied the aura.
        /// </summary>
        IUnitEntity Caster { get; }

        /// <summary>
        /// Seconds until the aura expires, infinite for an aura that only ends when something removes it.
        /// </summary>
        double Remaining { get; }

        /// <summary>
        /// Returns whether the aura is currently affecting a target.
        /// </summary>
        /// <remarks>
        /// False before it is applied and after it is removed, the spell that created it uses this to know
        /// whether it still has work outstanding.
        /// </remarks>
        bool IsActive { get; }

        /// <summary>
        /// Extend the aura back to its full duration.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Invoked once when the aura starts affecting <paramref name="target"/>.
        /// </summary>
        void Apply(IUnitEntity target);

        /// <summary>
        /// Advance the aura, returning false once it has expired.
        /// </summary>
        bool Update(IUnitEntity target, double lastTick);

        /// <summary>
        /// Invoked once when the aura stops affecting <paramref name="target"/>.
        /// </summary>
        void Remove(IUnitEntity target, AuraRemoveReason reason);
    }
}
