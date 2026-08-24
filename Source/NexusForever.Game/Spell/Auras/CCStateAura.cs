using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Spell;
using NexusForever.Network.World.Message.Model.Entity;

namespace NexusForever.Game.Spell.Auras
{
    /// <summary>
    /// Holds a <see cref="CCState"/> on its target for the duration of the effect.
    /// </summary>
    /// <remarks>
    /// The client applies the crowd control itself from the effect row in ServerSpellGo, this aura is what makes
    /// the server aware of it, so that it can be taken into account and ended authoritatively.
    /// </remarks>
    public class CCStateAura : Aura, ICCStateAura
    {
        public CCState State { get; }

        public CCStateAura(AuraKey key, IUnitEntity caster, uint castingId, uint effectId, double duration,
            CCState state)
            : base(key, caster, castingId, effectId, duration)
        {
            State = state;
        }

        protected override void OnRemove(IUnitEntity target, AuraRemoveReason reason)
        {
            // the ids have to be the ones the client saw in ServerSpellGo, otherwise it can't match the removal
            target.EnqueueToVisible(new ServerEntityCCStateRemove
            {
                UnitId              = target.Guid,
                CCType              = State,
                SpellCastUniqueId   = CastingId,
                SpellEffectUniqueId = EffectId,
                Removed             = true
            }, true);
        }
    }
}
