using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Spellslinger;

public sealed partial class SpellslingerClassMechanics
{
    public void OnCriticalHit(IUnitEntity attacker)
    {
        if (attacker is not IPlayer spellslinger)
            return;

        SpellslingerState.For(spellslinger).EnableFlameBurst(spellslinger);
        spellslinger.CastSpell(69706u, new SpellParameters());
    }
}
