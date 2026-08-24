using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Warrior;

public sealed partial class WarriorClassMechanics
{
    public void OnDeflect(IUnitEntity attacker, IUnitEntity victim)
    {
        if (attacker is IPlayer { Class: Class.Warrior } attackingWarrior)
            WarriorState.For(attackingWarrior).EnableAtomicSpear(attackingWarrior);
        if (victim is IPlayer { Class: Class.Warrior } defendingWarrior)
            WarriorState.For(defendingWarrior).EnableAtomicSpear(defendingWarrior);
    }

    public void OnCriticalHit(IUnitEntity attacker)
    {
        if (attacker is IPlayer warrior)
            WarriorState.For(warrior).EnableBreachingStrikes(warrior);
    }
}
