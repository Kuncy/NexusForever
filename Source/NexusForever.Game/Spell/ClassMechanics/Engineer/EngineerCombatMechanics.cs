using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Engineer;

public static class EngineerCombatMechanics
{
    public static void OnMultiHit(IUnitEntity attacker)
    {
        if (attacker is IPlayer { Class: Class.Engineer } engineer)
            EngineerState.For(engineer).EnableQuickBurst();
    }

    public static void OnGlance(IUnitEntity victim)
    {
        if (victim is IPlayer { Class: Class.Engineer } engineer)
            EngineerState.For(engineer).EnableFeedback();
    }
}
