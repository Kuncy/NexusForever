using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Spellslinger;

public static class SpellslingerCombatMechanics
{
    public static void OnCriticalHit(IUnitEntity attacker)
    {
        if (attacker is not IPlayer { Class: Class.Spellslinger } spellslinger)
            return;

        SpellslingerState.For(spellslinger).EnableFlameBurst(spellslinger);
        spellslinger.CastSpell(69706u, new SpellParameters());
    }
}
