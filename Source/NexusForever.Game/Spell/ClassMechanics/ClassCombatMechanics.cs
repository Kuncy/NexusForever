using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell.ClassMechanics.Spellslinger;
using NexusForever.Game.Spell.ClassMechanics.Stalker;
using NexusForever.Game.Spell.ClassMechanics.Warrior;
using NexusForever.Game.Spell.ClassMechanics.Engineer;
using NexusForever.Game.Spell.ClassMechanics.Medic;

namespace NexusForever.Game.Spell.ClassMechanics;

public static class ClassCombatMechanics
{
    public static void OnMultiHit(IUnitEntity attacker)
    {
        EngineerCombatMechanics.OnMultiHit(attacker);
        MedicCombatMechanics.OnMultiHit(attacker);
    }

    public static void OnMultiHeal(IUnitEntity healer)
        => MedicCombatMechanics.OnMultiHeal(healer);

    public static void OnGlance(IUnitEntity victim)
        => EngineerCombatMechanics.OnGlance(victim);

    public static void OnDeflect(IUnitEntity attacker, IUnitEntity victim)
    {
        WarriorCombatMechanics.OnDeflect(attacker, victim);
        StalkerCombatMechanics.OnDeflect(attacker, victim);
    }

    public static void OnCriticalHit(IUnitEntity attacker)
    {
        WarriorCombatMechanics.OnCriticalHit(attacker);
        SpellslingerCombatMechanics.OnCriticalHit(attacker);
        StalkerCombatMechanics.OnCriticalHit(attacker);
    }

    public static bool ShouldForceCritical(IUnitEntity attacker, ISpell spell)
        => StalkerCombatMechanics.ShouldForceCritical(attacker, spell);

    public static uint ModifyDamage(IUnitEntity attacker, IUnitEntity victim, uint damage)
    {
        damage = StalkerCombatMechanics.ModifyDamage(attacker, victim, damage);
        return MedicCombatMechanics.ModifyDamage(attacker, victim, damage);
    }

    public static void OnDamageResolved(IUnitEntity attacker, IUnitEntity victim)
        => StalkerCombatMechanics.OnDamageResolved(attacker, victim);
}
