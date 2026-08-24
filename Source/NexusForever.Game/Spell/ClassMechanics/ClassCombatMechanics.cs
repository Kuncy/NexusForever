using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Spell.ClassMechanics;

/// <summary>
/// Entry point for the combat related <see cref="IClassMechanics"/> hooks.
/// </summary>
/// <remarks>
/// Hooks with a single acting entity are dispatched to that entity's class, hooks where either side of an
/// exchange can react are dispatched to both.
/// </remarks>
public static class ClassCombatMechanics
{
    public static void OnMultiHit(IUnitEntity attacker)
    {
        ClassMechanicsRegistry.For(attacker)?.OnMultiHit(attacker);
    }

    public static void OnMultiHeal(IUnitEntity healer)
    {
        ClassMechanicsRegistry.For(healer)?.OnMultiHeal(healer);
    }

    public static void OnGlance(IUnitEntity victim)
    {
        ClassMechanicsRegistry.For(victim)?.OnGlance(victim);
    }

    public static void OnDeflect(IUnitEntity attacker, IUnitEntity victim)
    {
        ClassMechanicsRegistry.ForBoth(attacker, victim, m => m.OnDeflect(attacker, victim));
    }

    public static void OnCriticalHit(IUnitEntity attacker)
    {
        ClassMechanicsRegistry.For(attacker)?.OnCriticalHit(attacker);
    }

    public static uint ModifyDamage(IUnitEntity attacker, IUnitEntity victim, uint damage)
    {
        return ClassMechanicsRegistry.For(attacker)?.ModifyDamage(attacker, victim, damage) ?? damage;
    }

    public static void OnDamageResolved(IUnitEntity attacker, IUnitEntity victim)
    {
        ClassMechanicsRegistry.ForBoth(attacker, victim, m => m.OnDamageResolved(attacker, victim));
    }
}
