using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Abstract.Spell
{
    /// <summary>
    /// Holds the <see cref="IAura"/> currently affecting an <see cref="IUnitEntity"/>.
    /// </summary>
    public interface IAuraManager : IEnumerable<IAura>
    {
        /// <summary>
        /// Apply <paramref name="aura"/> to the target.
        /// </summary>
        /// <remarks>
        /// An aura already present under the same <see cref="IAura.Key"/> is refreshed instead, in which case the
        /// existing aura is returned and <paramref name="aura"/> is discarded without ever being applied.
        /// </remarks>
        IAura Apply(IAura aura);

        /// <summary>
        /// Return the <see cref="IAura"/> under <paramref name="key"/>, if any.
        /// </summary>
        IAura Get(AuraKey key);

        /// <summary>
        /// Remove the <see cref="IAura"/> under <paramref name="key"/>, returning whether one was present.
        /// </summary>
        bool Remove(AuraKey key, AuraRemoveReason reason);

        /// <summary>
        /// Remove every <see cref="IAura"/> applied by the supplied caster.
        /// </summary>
        void RemoveByCaster(uint casterGuid, AuraRemoveReason reason);

        /// <summary>
        /// Remove every <see cref="IAura"/>.
        /// </summary>
        void RemoveAll(AuraRemoveReason reason);

        void Update(double lastTick);
    }
}
