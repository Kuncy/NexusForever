using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Stalker;

public sealed partial class StalkerClassMechanics
{
    public void OnCriticalHit(IUnitEntity attacker)
    {
        if (attacker is IPlayer stalker)
            StalkerState.For(stalker).EnablePunish(stalker);
    }

    public void OnDeflect(IUnitEntity attacker, IUnitEntity victim)
    {
        if (victim is not IPlayer { Class: Class.Stalker } stalker)
            return;

        StalkerState state = StalkerState.For(stalker);
        state.TryResetDecimate(stalker);
        if (state.ConsumeSteadfast())
            stalker.ModifyVital(Vital.Resource3, 20f);
    }

    public uint ModifyDamage(IUnitEntity attacker, IUnitEntity victim, uint damage)
    {
        if (attacker is IPlayer stalker
            && StalkerState.For(stalker).IsAnalyzeWeaknessTarget(victim.Guid))
            return (uint)MathF.Ceiling(damage * 1.2f);
        return damage;
    }

    public void OnDamageResolved(IUnitEntity attacker, IUnitEntity victim)
    {
        if (victim is IPlayer { Class: Class.Stalker } damagedStalker
            && attacker != victim)
            StalkerState.For(damagedStalker).OnDamageTaken();

        if (victim.IsAlive || attacker is not IPlayer { Class: Class.Stalker } stalker)
            return;

        if (StalkerState.For(stalker).RemoveAnalyzeWeaknessTarget(victim.Guid))
            stalker.SpellManager.SetSpellCooldown(StalkerSpellIds.AnalyzeWeakness, 0d);
    }
}
