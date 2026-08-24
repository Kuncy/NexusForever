using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Medic;

public sealed partial class MedicClassMechanics
{
    public void OnMultiHit(IUnitEntity attacker)
    {
        if (attacker is IPlayer medic)
            MedicState.For(medic).EnableAtomize();
    }

    public void OnMultiHeal(IUnitEntity healer)
    {
        if (healer is IPlayer medic)
            MedicState.For(medic).EnableDualShock();
    }

    public uint ModifyDamage(IUnitEntity attacker, IUnitEntity victim, uint damage)
        => attacker is IPlayer medic
            && MedicState.For(medic).IsNerveInductionTarget(victim.Guid)
                ? (uint)MathF.Round(damage * 1.25f)
                : damage;
}
