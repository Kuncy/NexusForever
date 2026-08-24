using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using System.Numerics;

namespace NexusForever.Game.Spell.ClassMechanics.Esper;

public static class EsperEffectMechanics
{
    public static bool TryHandleForcedMove(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Esper })
            return false;
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (baseId is not (EsperSpellIds.FadeOutBase or EsperSpellIds.ProjectedSpiritBase))
            return false;

        float distance = BitConverter.UInt32BitsToSingle(info.Entry.DataBits01);
        float yaw = -target.Rotation.X;
        Vector3 forward = new(MathF.Cos(yaw), 0f, MathF.Sin(yaw));
        float direction = baseId == EsperSpellIds.FadeOutBase ? -1f : 1f;
        spell.ScheduleAction(info.Entry.DelayTime / 1000d,
            () => target.MovementManager.SetPosition(
                target.Position + forward * distance * direction, false));
        return true;
    }

    public static bool ShouldApplyDamage(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        uint rootBaseId = spell.Parameters.RootSpellInfo.BaseInfo.Entry.Id;
        if (!EsperSpellIds.IsPsiFinisher(rootBaseId)
            || spell.Caster is not IPlayer { Class: Class.Esper } esper)
            return true;

        uint psiPoints = spell.Parameters.ClassResourceSnapshot > 0u
            ? spell.Parameters.ClassResourceSnapshot
            : EsperState.For(esper).SnapshotPsiPoints(esper);
        if (info.Entry.OrderIndex > 4u)
            return true;
        bool matchingRow = info.Entry.OrderIndex == psiPoints - 1u;
        if (!matchingRow)
            info.DropEffect = true;
        return matchingRow;
    }

    public static bool TryHandleProxy(ISpell spell, IUnitEntity target,
        ISpellTargetEffectInfo info)
    {
        if (spell.Caster is not IPlayer { Class: Class.Esper } esper)
            return false;

        uint rootBaseId = spell.Parameters.RootSpellInfo.BaseInfo.Entry.Id;
        if (EsperSpellIds.IsPsiFinisher(rootBaseId)
            && info.Entry.OrderIndex <= 4u)
        {
            uint points = spell.Parameters.ClassResourceSnapshot > 0u
                ? spell.Parameters.ClassResourceSnapshot
                : EsperState.For(esper).SnapshotPsiPoints(esper);
            if (info.Entry.OrderIndex != points - 1u)
                return true;
            uint selectedProxy = info.Entry.DataBits00 != 0u
                ? info.Entry.DataBits00
                : info.Entry.DataBits01;
            if (selectedProxy != 0u)
            {
                uint duration = info.Entry.DurationTime;
                if (info.Entry.DataBits00 == 0u && info.Entry.TickTime > 0u
                    && duration > 0u)
                {
                    uint ticks = Math.Max(1u,
                        (duration + info.Entry.TickTime - 1u) / info.Entry.TickTime);
                    for (uint tick = 0u; tick < ticks; tick++)
                        spell.CastProxySpell(selectedProxy, target,
                            info.Entry.DelayTime / 1000d
                                + tick * info.Entry.TickTime / 1000d);
                }
                else
                    spell.CastProxySpell(selectedProxy, target,
                        info.Entry.DelayTime / 1000d);
            }
            return true;
        }

        if (rootBaseId == EsperSpellIds.PsychicFrenzyBase
            && spell.TryConsumeSuccessfulHit())
        {
            esper.ModifyVital(Vital.Resource1, 1f);
            return true;
        }

        if (spell.Parameters.SpellInfo.Entry.Id != EsperSpellIds.TelekineticStrike
            || info.Entry.DataBits00 != EsperSpellIds.TelekineticStrikePsiPoint)
            return false;

        if (spell.TryConsumeSuccessfulHit())
            spell.CastProxySpell(info.Entry.DataBits00, spell.Caster);
        return true;
    }

    public static uint GetAdditionalDamageRepeatCount(ISpell spell)
        => spell.Parameters.RootSpellInfo.BaseInfo.Entry.Id == EsperSpellIds.PsychicFrenzyBase
            ? 2u
            : 0u;

    public static double GetAdditionalDamageRepeatInterval(ISpell spell) => 0.12d;
}
