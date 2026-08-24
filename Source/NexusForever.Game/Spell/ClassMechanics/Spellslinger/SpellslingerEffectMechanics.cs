using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Spell.ClassMechanics.Spellslinger;

public static class SpellslingerEffectMechanics
{
    public static bool TryHandleForcedMove(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id != SpellslingerSpellIds.Gate)
            return false;

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
        return true;
    }

    public static bool ShouldApplyDamage(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Parameters.SpellInfo.Entry.Id is not (39324u or 39325u))
            return true;

        bool executeDamage = target.MaxHealth > 0u
            && target.Health * 100u < target.MaxHealth * 30u;
        bool matchingRow = executeDamage
            ? info.Entry.OrderIndex == 2u
            : info.Entry.OrderIndex == 1u;
        if (!matchingRow)
            info.DropEffect = true;
        return matchingRow;
    }

    public static bool TryHandleProxy(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Spellslinger } player)
            return false;

        uint parentSpellId = spell.Parameters.SpellInfo.Entry.Id;
        uint parentBaseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        uint proxySpellId = info.Entry.DataBits00;

        if (parentBaseId is SpellslingerSpellIds.QuickDraw or SpellslingerSpellIds.QuickDrawSurged)
        {
            if (info.Entry.OrderIndex != 0u)
                return true;
            uint first = parentBaseId == SpellslingerSpellIds.QuickDraw ? 43498u : 76003u;
            uint second = parentBaseId == SpellslingerSpellIds.QuickDraw ? 43499u : 76004u;
            spell.CastProxySpell(first, target);
            spell.CastProxySpell(second, target, 0.33d);
            spell.CastProxySpell(first, target, 0.66d);
            return true;
        }

        if (parentSpellId is SpellslingerSpellIds.RapidFire or SpellslingerSpellIds.RapidFireSurged)
        {
            if (info.Entry.OrderIndex != 0u)
                return true;
            uint impact = parentSpellId == SpellslingerSpellIds.RapidFire ? 35360u : 76838u;
            spell.CastProxySpell(impact, target);
            spell.CastProxySpell(impact, target, 0.33d);
            spell.CastProxySpell(impact, target, 0.66d);
            return true;
        }

        if (parentSpellId == SpellslingerSpellIds.Assassinate)
        {
            if (info.Entry.OrderIndex != 0u)
                return true;
            bool surged = SpellslingerState.For(player).SpellSurgeActive;
            spell.CastProxySpell(surged ? 76927u : 39324u, target);
            if (!surged)
                spell.CastProxySpell(38907u, target);
            return true;
        }

        if (parentBaseId == SpellslingerSpellIds.SpellSurge
            && proxySpellId == 47439u)
        {
            if (SpellslingerState.For(player).SpellSurgeActive)
                spell.CastProxySpell(proxySpellId, target);
            return true;
        }

        return false;
    }

    public static void AfterPropertyApplied(ISpell spell, IUnitEntity target)
    {
        if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id != SpellslingerSpellIds.GatherFocus
            || target is not IPlayer { Class: Class.Spellslinger })
            return;

        target.ModifyVital(Vital.Focus, 60f);
        for (uint tick = 1u; tick <= 6u; tick++)
            spell.ScheduleAction(tick, () => target.ModifyVital(Vital.Resource4, 3f));
    }
}
