using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Spell.Auras
{
    /// <summary>
    /// Holds a <see cref="ISpellPropertyModifier"/> on a target for the duration of the effect.
    /// </summary>
    public class PropertyModifierAura : Aura
    {
        private readonly ISpellPropertyModifier modifier;
        private readonly uint spell4Id;

        public PropertyModifierAura(AuraKey key, IUnitEntity caster, uint castingId, uint effectId, double duration,
            ISpellPropertyModifier modifier, uint spell4Id)
            : base(key, caster, castingId, effectId, duration)
        {
            this.modifier = modifier;
            this.spell4Id = spell4Id;
        }

        protected override void OnApply(IUnitEntity target)
        {
            target.AddSpellModifierProperty(modifier, spell4Id);
        }

        protected override void OnRemove(IUnitEntity target, AuraRemoveReason reason)
        {
            target.RemoveSpellProperty(modifier.Property, spell4Id);
        }
    }
}
