using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Stalker;

public static class StalkerCombatMechanics
{
    public static void OnCriticalHit(IUnitEntity attacker)
    {
        if (attacker is IPlayer { Class: Class.Stalker } stalker)
            StalkerState.For(stalker).EnablePunish(stalker);
    }

    public static void OnDeflect(IUnitEntity attacker, IUnitEntity victim)
    {
        if (victim is not IPlayer { Class: Class.Stalker } stalker)
            return;

        StalkerState state = StalkerState.For(stalker);
        state.TryResetDecimate(stalker);
        if (state.ConsumeSteadfast())
            stalker.ModifyVital(Vital.Resource3, 20f);
    }

    public static bool ShouldForceCritical(IUnitEntity attacker, ISpell spell)
        => attacker is IPlayer { Class: Class.Stalker }
            && spell.Parameters.ForceCritical;

    public static uint ModifyDamage(IUnitEntity attacker, IUnitEntity victim, uint damage)
    {
        if (attacker is IPlayer { Class: Class.Stalker } stalker
            && StalkerState.For(stalker).IsAnalyzeWeaknessTarget(victim.Guid))
            return (uint)MathF.Ceiling(damage * 1.2f);
        return damage;
    }

    public static void OnDamageResolved(IUnitEntity attacker, IUnitEntity victim)
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
