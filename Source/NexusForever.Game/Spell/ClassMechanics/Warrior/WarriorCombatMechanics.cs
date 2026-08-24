using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Warrior;

public static class WarriorCombatMechanics
{
    public static void OnDeflect(IUnitEntity attacker, IUnitEntity victim)
    {
        if (attacker is IPlayer { Class: Class.Warrior } attackingWarrior)
            WarriorState.For(attackingWarrior).EnableAtomicSpear(attackingWarrior);
        if (victim is IPlayer { Class: Class.Warrior } defendingWarrior)
            WarriorState.For(defendingWarrior).EnableAtomicSpear(defendingWarrior);
    }

    public static void OnCriticalHit(IUnitEntity attacker)
    {
        if (attacker is IPlayer { Class: Class.Warrior } warrior)
            WarriorState.For(warrior).EnableBreachingStrikes(warrior);
    }

}
