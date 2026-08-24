using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Spell.ClassMechanics;

/// <summary>
/// Server side mechanics for a single <see cref="Class"/>.
/// </summary>
/// <remarks>
/// Every member has a default implementation that does nothing, a class only implements the hooks it actually uses.
/// Implementations are resolved through <see cref="ClassMechanicsRegistry"/>, adding a new class means adding a
/// single <see cref="IClassMechanics"/> implementation rather than touching each of the mechanic entry points.
/// </remarks>
public interface IClassMechanics
{
    /// <summary>
    /// <see cref="Class"/> the mechanics belong to.
    /// </summary>
    Class Class { get; }

    #region Resources

    /// <summary>
    /// Invoked when a <see cref="IPlayer"/> of this class enters the world.
    /// </summary>
    void InitialiseResources(IPlayer player)
    {
    }

    /// <summary>
    /// Invoked on each stat update tick for a <see cref="IPlayer"/> of this class.
    /// </summary>
    void UpdateResources(IPlayer player, uint statUpdateTick, double outOfCombatTime)
    {
    }

    /// <summary>
    /// Invoked after a vital of a <see cref="IPlayer"/> of this class was modified.
    /// </summary>
    /// <param name="amount">Amount that was requested.</param>
    /// <param name="delta">Amount that was actually applied after clamping, can be 0 when the vital was already at its bound.</param>
    void OnVitalModified(IPlayer player, Vital vital, float amount, float delta)
    {
    }

    #endregion

    #region Combat

    void OnMultiHit(IUnitEntity attacker)
    {
    }

    void OnMultiHeal(IUnitEntity healer)
    {
    }

    void OnGlance(IUnitEntity victim)
    {
    }

    void OnDeflect(IUnitEntity attacker, IUnitEntity victim)
    {
    }

    void OnCriticalHit(IUnitEntity attacker)
    {
    }

    uint ModifyDamage(IUnitEntity attacker, IUnitEntity victim, uint damage)
    {
        return damage;
    }

    void OnDamageResolved(IUnitEntity attacker, IUnitEntity victim)
    {
    }

    #endregion

    #region Effects

    bool TryHandleVitalModifier(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
    {
        return false;
    }

    bool TryHandleForcedMove(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
    {
        return false;
    }

    bool TryHandleProc(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
    {
        return false;
    }

    bool ShouldApplyDamage(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
    {
        return true;
    }

    bool TryHandleProxy(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
    {
        return false;
    }

    /// <summary>
    /// Returns additional damage repeats for spells the client executes in a repeated phase.
    /// </summary>
    bool TryGetAdditionalDamageRepeat(ISpell spell, out uint count, out double interval)
    {
        count    = 0u;
        interval = 0d;
        return false;
    }

    void AfterCcStateApplied(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
    {
    }

    bool ShouldApplyProperty(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
    {
        return true;
    }

    bool ShouldEvaluatePropertyPrerequisite(ISpell spell)
    {
        return true;
    }

    uint GetPropertyDuration(ISpell spell, uint duration)
    {
        return duration;
    }

    void AfterPropertyApplied(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
    {
    }

    double GetEffectDelay(ISpell spell, ISpellTargetEffectInfo info)
    {
        return 0d;
    }

    bool TryHandlePersonalDamageHealModifier(ISpell spell, IUnitEntity target)
    {
        return false;
    }

    #endregion

    #region Spell

    bool TryCheckPrerequisites(Spell spell, IPlayer player, out CastResult result)
    {
        result = CastResult.Ok;
        return false;
    }

    void BeforeExecute(Spell spell, IPlayer player)
    {
    }

    Spell4Entry SelectCooldownEntry(Spell spell, IPlayer player, Spell4Entry entry)
    {
        return entry;
    }

    void AfterEffects(Spell spell, IPlayer player)
    {
    }

    void AfterSpellGo(Spell spell, IPlayer player)
    {
    }

    bool ShouldFinishRoot(Spell spell)
    {
        return false;
    }

    bool TryCostResources(Spell spell, IPlayer player, Spell4Entry entry)
    {
        return false;
    }

    bool IsServerExecutedChannel(Spell spell)
    {
        return false;
    }

    #endregion

    #region Character Spell

    bool CanBeginCast(IPlayer owner, ICharacterSpell characterSpell)
    {
        return true;
    }

    ISpellInfo SelectSpell(IPlayer owner, ICharacterSpell characterSpell, ISpellInfo current)
    {
        return current;
    }

    #endregion
}
