using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell.ClassMechanics.Spellslinger;
using NexusForever.Game.Spell.ClassMechanics.Warrior;

namespace NexusForever.Game.Spell.ClassMechanics;

public static class ClassCombatMechanics
{
    public static void OnDeflect(IUnitEntity attacker, IUnitEntity victim)
        => WarriorCombatMechanics.OnDeflect(attacker, victim);

    public static void OnCriticalHit(IUnitEntity attacker)
    {
        WarriorCombatMechanics.OnCriticalHit(attacker);
        SpellslingerCombatMechanics.OnCriticalHit(attacker);
    }
}
