using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell.ClassMechanics.Warrior;

public static class WarriorEffectMechanics
{
    public static bool TryHandleVitalModifier(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (target is not IPlayer { Class: Class.Warrior } player)
            return false;

        uint? kineticEnergy = spell.Parameters.SpellInfo.Entry.Id switch
        {
            53865u or 54587u => 180u,
            79652u or 81699u => 150u,
            53867u or 83759u => 150u,
            46712u or 54606u => 250u,
            46713u           => 125u,
            88546u           => 500u,
            _                => null
        };
        if (kineticEnergy.HasValue)
        {
            if ((Vital)info.Entry.DataBits00 == Vital.Resource1
                && info.Entry.DataBits01 == kineticEnergy.Value)
                player.ModifyVital(Vital.KineticCell, kineticEnergy.Value);
            return true;
        }

        int amount = unchecked((int)info.Entry.DataBits01);
        return amount < 0 && WarriorState.For(player).OverdriveActive;
    }

    public static bool TryHandleForcedMove(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id
            is not (WarriorSpellIds.LeapMove or WarriorSpellIds.BumRushMove))
            return false;

        MoveForward(spell, target, info);
        return true;
    }

    public static bool TryHandleProc(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (baseId == WarriorSpellIds.PlasmaWall
            && info.Entry.DataBits01 == WarriorSpellIds.PlasmaWallDamage)
        {
            for (uint tick = 0u; tick < 10u; tick++)
                spell.CastProxySpell(WarriorSpellIds.PlasmaWallDamage,
                    spell.Caster, tick * 0.5d);
            return true;
        }

        if (baseId == WarriorSpellIds.SentinelGuard
            && info.Entry.DataBits01 == WarriorSpellIds.SentinelRetaliation
            && target is UnitEntity guardedUnit)
        {
            guardedUnit.SetReactiveDamage(spell.Caster,
                WarriorSpellIds.SentinelRetaliation, info.Entry.DurationTime);
            return true;
        }

        return false;
    }

    public static bool TryHandleProxy(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Warrior } player)
            return false;

        uint parentSpellId = spell.Parameters.SpellInfo.Entry.Id;
        uint parentBaseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        uint proxySpellId = info.Entry.DataBits00;
        WarriorState state = WarriorState.For(player);

        if (parentBaseId == WarriorSpellIds.PolarityField
            && proxySpellId == WarriorSpellIds.PolarityFieldPulse)
        {
            for (uint tick = 0u; tick < 10u; tick++)
                spell.CastProxySpell(proxySpellId, target, tick);
            return true;
        }

        if (parentBaseId == WarriorSpellIds.AugmentedBlade)
        {
            if (!state.AugmentedBladeActive
                && proxySpellId == WarriorSpellIds.AugmentedBladeOff)
                spell.CastProxySpell(proxySpellId, target);
            return true;
        }

        if (parentBaseId == WarriorSpellIds.PowerLink)
        {
            if (state.PowerLinkActive && proxySpellId == WarriorSpellIds.PowerLinkOn)
                spell.CastProxySpell(proxySpellId, target);
            else if (!state.PowerLinkActive && proxySpellId == WarriorSpellIds.PowerLinkOff)
                spell.CastProxySpell(proxySpellId, target);
            return true;
        }

        if (proxySpellId == 0u)
        {
            if (parentBaseId == WarriorSpellIds.PlasmaWall
                && info.Entry.DataBits01 == WarriorSpellIds.PlasmaWallDrain)
                for (uint tick = 1u; tick <= 10u; tick++)
                    spell.CastProxySpell(WarriorSpellIds.PlasmaWallDrain,
                        spell.Caster, tick * 0.5d);
            return true;
        }

        if (proxySpellId is 53865u or 54587u or 79652u or 81699u)
        {
            if (spell.TryConsumeSuccessfulHit())
                spell.CastProxySpell(proxySpellId, spell.Caster);
            return true;
        }

        if (parentSpellId == 61053u && proxySpellId == 53867u)
        {
            if (spell.TryConsumeSuccessfulHit())
            {
                spell.CastProxySpell(proxySpellId, spell.Caster);
                spell.CastProxySpell(proxySpellId, spell.Caster, 0.25d);
            }
            return true;
        }

        return false;
    }

    public static bool ShouldApplyProperty(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Warrior } player
            || spell.Parameters.SpellInfo.BaseInfo.Entry.Id != WarriorSpellIds.AugmentedBlade)
            return true;

        if (WarriorState.For(player).AugmentedBladeActive)
            return true;

        target.RemoveSpellProperty((Property)info.Entry.DataBits00,
            spell.Parameters.SpellInfo.Entry.Id);
        info.DropEffect = true;
        return false;
    }

    public static uint GetPropertyDuration(ISpell spell, uint duration)
    {
        return spell.Parameters.SpellInfo.BaseInfo.Entry.Id switch
        {
            WarriorSpellIds.DefenseGrid       => 10000u,
            WarriorSpellIds.PolarityFieldAura => 1100u,
            55436u                            => 500u,
            _                                 => duration
        };
    }

    public static bool ShouldEvaluatePropertyPrerequisite(ISpell spell)
        => spell.Parameters.SpellInfo.BaseInfo.Entry.Id != WarriorSpellIds.AugmentedBlade;

    public static bool IsMenacingStrike(ISpell spell)
        => spell.Parameters.SpellInfo.BaseInfo.Entry.Id == WarriorSpellIds.MenacingStrike;

    private static void MoveForward(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        void Move()
        {
            float distance = BitConverter.UInt32BitsToSingle(info.Entry.DataBits01);
            if (distance <= 0f)
                return;

            float yaw = -target.Rotation.X;
            Vector3 forward = new(MathF.Cos(yaw), 0f, MathF.Sin(yaw));
            target.MovementManager.SetPosition(target.Position + forward * distance, false);
        }

        if (info.Entry.DelayTime == 0u)
            Move();
        else
            spell.ScheduleAction(info.Entry.DelayTime / 1000d, Move);
    }
}
