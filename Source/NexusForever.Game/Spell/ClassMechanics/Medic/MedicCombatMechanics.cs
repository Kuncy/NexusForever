using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Medic;

public static class MedicCombatMechanics
{
    public static void OnMultiHit(IUnitEntity attacker)
    {
        if (attacker is IPlayer { Class: Class.Medic } medic)
            MedicState.For(medic).EnableAtomize();
    }

    public static void OnMultiHeal(IUnitEntity healer)
    {
        if (healer is IPlayer { Class: Class.Medic } medic)
            MedicState.For(medic).EnableDualShock();
    }

    public static uint ModifyDamage(IUnitEntity attacker, IUnitEntity victim, uint damage)
        => attacker is IPlayer { Class: Class.Medic } medic
            && MedicState.For(medic).IsNerveInductionTarget(victim.Guid)
                ? (uint)MathF.Round(damage * 1.25f)
                : damage;
}
