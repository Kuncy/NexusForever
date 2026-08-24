using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Spell.ClassMechanics.Stalker;

public sealed partial class StalkerClassMechanics
{
    public bool TryCheckPrerequisites(Spell spell, IPlayer player,
        out CastResult result)
    {
        result = CastResult.Ok;
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        StalkerState state = StalkerState.For(player);
        if (StalkerSpellIds.IsNanoSkin(baseId))
        {
            if (!player.InCombat)
                player.SpellManager.SetSpellCooldown(spell.Parameters.SpellInfo.Entry.Id, 0d);
            return true;
        }
        if (baseId == StalkerSpellIds.AnalyzeWeaknessBase)
        {
            result = state.StealthActive ? CastResult.Ok : CastResult.PrereqCasterCast;
            return true;
        }
        if (baseId == StalkerSpellIds.PunishBase)
        {
            result = state.PunishAvailable ? CastResult.Ok : CastResult.PrereqCasterCast;
            return true;
        }
        if (baseId == StalkerSpellIds.NeutralizeBase)
        {
            result = player.GetVitalValue(Vital.Resource3) >= state.GetNeutralizeCost()
                ? CastResult.Ok
                : CastResult.CasterVitalCostResource3;
            return true;
        }
        return false;
    }

    public void BeforeExecute(Spell spell, IPlayer player)
    {
        StalkerState state = StalkerState.For(player);
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (spell.Parameters.UserInitiatedSpellCast
            && state.StealthActive
            && state.ActiveNanoSkinBaseId == StalkerSpellIds.NanoSkinLethalBase
            && spell.Parameters.SpellInfo.Entry.SpellCastStealthChange == 1u)
            spell.Parameters.ForceCritical = true;

        if (StalkerSpellIds.IsNanoSkin(baseId))
            state.EnterStealth(baseId);
        else if (baseId == StalkerSpellIds.TacticalRetreatBase)
            state.EnterProtectedStealth(2d);
        else if (baseId == StalkerSpellIds.FalseRetreatBase)
            state.StartFalseRetreat(player.Position);
        else if (baseId == StalkerSpellIds.NanoFieldBase)
            state.StartNanoField();
        else if (baseId == StalkerSpellIds.NanoFieldEndBase)
            state.EndNanoField();
        else if (baseId == StalkerSpellIds.SteadfastBase)
            state.ActivateSteadfast();

        if (spell.Parameters.SpellInfo.Entry.Id == StalkerSpellIds.PunishAvailableBuff)
            state.PunishBuffCastingId = spell.CastingId;
    }

    public void AfterEffects(Spell spell, IPlayer player)
    {
        StalkerState state = StalkerState.For(player);
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (baseId == StalkerSpellIds.PunishBase)
            state.ConsumePunish(player);

        if (spell.Parameters.UserInitiatedSpellCast
            && spell.Parameters.SpellInfo.Entry.SpellCastStealthChange == 1u)
            state.BreakStealth();
    }

    public void AfterSpellGo(Spell spell, IPlayer player)
    {
        if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id != StalkerSpellIds.CloneBase
            || spell.Parameters.PrimaryTargetId == 0u)
            return;

        IUnitEntity target = player.Map.GetEntity<IUnitEntity>(spell.Parameters.PrimaryTargetId);
        if (target == null)
            return;

        for (uint cycle = 0u; cycle < 8u; cycle++)
        {
            double start = 0.25d + cycle * 1.25d;
            spell.CastProxySpell(StalkerSpellIds.CloneSlash, target, start);
            spell.CastProxySpell(StalkerSpellIds.CloneSlash, target, start + 0.14d);
            spell.CastProxySpell(StalkerSpellIds.CloneSlash, target, start + 0.28d);
        }
        spell.CastProxySpell(StalkerSpellIds.ClonePrecisionStrike, target, 5d);
        spell.CastProxySpell(StalkerSpellIds.ClonePrecisionStrike, target, 10d);
    }

    public Spell4Entry SelectCooldownEntry(Spell spell, IPlayer player,
        Spell4Entry entry)
    {
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (StalkerSpellIds.IsNanoSkin(baseId)
            && !player.InCombat)
            return null;
        return entry;
    }

    public bool TryCostResources(Spell spell, IPlayer player, Spell4Entry entry)
    {
        if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id != StalkerSpellIds.NeutralizeBase)
            return false;

        StalkerState state = StalkerState.For(player);
        spell.CostResource(entry.InnateCostType0, state.GetNeutralizeCost());
        spell.CostResource(entry.InnateCostType1, entry.InnateCost1);
        state.RecordNeutralizeCast();
        return true;
    }

    public bool IsServerExecutedChannel(Spell spell)
        => spell.Parameters.SpellInfo.BaseInfo.Entry.Id
            is StalkerSpellIds.FrenzyBase or StalkerSpellIds.PreparationBase;
}
