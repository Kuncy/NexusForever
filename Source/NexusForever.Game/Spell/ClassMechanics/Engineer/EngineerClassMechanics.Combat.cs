using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Engineer;

public sealed partial class EngineerClassMechanics
{
    public void OnMultiHit(IUnitEntity attacker)
    {
        if (attacker is IPlayer engineer)
            EngineerState.For(engineer).EnableQuickBurst();
    }

    public void OnGlance(IUnitEntity victim)
    {
        if (victim is IPlayer engineer)
            EngineerState.For(engineer).EnableFeedback();
    }
}
