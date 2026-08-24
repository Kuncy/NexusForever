using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Static;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell.ClassMechanics.Warrior;

public static class WarriorSpellMechanics
{
    public static bool IsServerExecutedChannel(Spell spell)
        => spell.Parameters.SpellInfo.BaseInfo.Entry.Id == WarriorSpellIds.Whirlwind;

    public static bool TryCheckPrerequisites(Spell spell, IPlayer player, out CastResult result)
    {
        result = CastResult.Ok;
        if (player.Class != Class.Warrior)
            return false;

        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        WarriorState state = WarriorState.For(player);
        if (baseId == WarriorSpellIds.BreachingStrikes)
        {
            result = state.BreachingStrikesAvailable ? CastResult.Ok : CastResult.PrereqCasterCast;
            return true;
        }

        if (baseId == WarriorSpellIds.AtomicSpear)
        {
            result = state.AtomicSpearAvailable ? CastResult.Ok : CastResult.PrereqCasterCast;
            return true;
        }

        if (baseId is WarriorSpellIds.Onslaught or WarriorSpellIds.Juggernaut
            or WarriorSpellIds.AugmentedBlade or WarriorSpellIds.PowerLink)
            return true;

        if (WarriorSpellIds.IsRampageStage(baseId)
            || baseId is WarriorSpellIds.Whirlwind or WarriorSpellIds.BolsteringStrike
                or WarriorSpellIds.ShieldBurst or WarriorSpellIds.PlasmaWall)
        {
            result = player.GetVitalValue(Vital.KineticCell) >= 250f
                ? CastResult.Ok
                : CastResult.CasterVitalCostResource1;
            return true;
        }

        return false;
    }

    public static void BeforeExecute(Spell spell, IPlayer player)
    {
        if (player.Class != Class.Warrior)
            return;

        WarriorState state = WarriorState.For(player);
        uint spellId = spell.Parameters.SpellInfo.Entry.Id;
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (spellId == WarriorSpellIds.BreachingStrikesBuff)
            state.BreachingStrikesBuffCastingId = spell.CastingId;
        else if (spellId == WarriorSpellIds.AtomicSpearBuff)
            state.AtomicSpearBuffCastingId = spell.CastingId;

        if (baseId == WarriorSpellIds.Onslaught)
        {
            state.EnableOverdrive();
            player.SpellManager.ResetAllSpellCooldowns();
        }
    }

    public static Spell4Entry SelectCooldownEntry(Spell spell, Spell4Entry cooldownEntry)
    {
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (!WarriorSpellIds.IsRampageStage(baseId))
            return cooldownEntry;

        if (baseId != WarriorSpellIds.RampageStage4)
            return null;

        return spell.Parameters.CharacterSpell?.SpellInfo.Entry ?? cooldownEntry;
    }

    public static void AfterEffects(Spell spell, IPlayer player)
    {
        if (player.Class != Class.Warrior)
            return;

        WarriorState state = WarriorState.For(player);
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (baseId == WarriorSpellIds.BreachingStrikes)
            state.ConsumeBreachingStrikes(player);
        else if (baseId == WarriorSpellIds.AtomicSpear)
            state.ConsumeAtomicSpear(player);
    }

    public static void AfterSpellGo(Spell spell, IPlayer player)
    {
        if (player.Class != Class.Warrior)
            return;

        WarriorState state = WarriorState.For(player);
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (baseId == WarriorSpellIds.AugmentedBlade)
            UpdateAugmentedBlade(spell, player, state, 0u);
        else if (baseId == WarriorSpellIds.PowerLink)
            UpdatePowerLink(spell, player, state);
    }

    private static void UpdateAugmentedBlade(Spell spell, IPlayer player,
        WarriorState state, uint quarterSecondTick)
    {
        if (!state.AugmentedBladeActive)
        {
            player.RemoveSpellProperty(Property.DamageDealtMultiplierMelee,
                spell.Parameters.SpellInfo.Entry.Id);
            player.RemoveSpellProperty(Property.BaseLifesteal,
                spell.Parameters.SpellInfo.Entry.Id);
            return;
        }

        uint stack = Math.Min(quarterSecondTick / 4u + 1u, 20u);
        float drain = 5f * stack;
        if (player.GetVitalValue(Vital.KineticCell) < drain && !state.OverdriveActive)
        {
            state.AugmentedBladeActive = false;
            UpdateAugmentedBlade(spell, player, state, quarterSecondTick);
            return;
        }

        if (!state.OverdriveActive)
            player.ModifyVital(Vital.KineticCell, -drain);
        spell.ScheduleAction(0.25d,
            () => UpdateAugmentedBlade(spell, player, state, quarterSecondTick + 1u));
    }

    private static void UpdatePowerLink(Spell spell, IPlayer player, WarriorState state)
    {
        if (!state.PowerLinkActive)
        {
            player.RemoveSpellProperty(Property.DamageDealtMultiplierPhysical,
                WarriorSpellIds.PowerLinkBuff);
            player.RemoveSpellProperty(Property.DamageDealtMultiplierTech,
                WarriorSpellIds.PowerLinkBuff);
            player.RemoveSpellProperty(Property.DamageDealtMultiplierMagic,
                WarriorSpellIds.PowerLinkBuff);
            return;
        }

        const float drain = 56f;
        if (player.GetVitalValue(Vital.KineticCell) < drain && !state.OverdriveActive)
        {
            state.PowerLinkActive = false;
            UpdatePowerLink(spell, player, state);
            return;
        }

        if (!state.OverdriveActive)
            player.ModifyVital(Vital.KineticCell, -drain);
        spell.CastProxySpell(WarriorSpellIds.PowerLinkBuff, player);
        spell.ScheduleAction(0.25d, () => UpdatePowerLink(spell, player, state));
    }
}
