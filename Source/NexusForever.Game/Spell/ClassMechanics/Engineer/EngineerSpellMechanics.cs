namespace NexusForever.Game.Spell.ClassMechanics.Engineer;

public static class EngineerSpellMechanics
{
    public static bool TryCheckPrerequisites(Spell spell,
        NexusForever.Game.Abstract.Entity.IPlayer player,
        out NexusForever.Network.World.Message.Static.CastResult result)
    {
        result = NexusForever.Network.World.Message.Static.CastResult.Ok;
        if (player.Class != NexusForever.Game.Static.Entity.Class.Engineer)
            return false;

        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        EngineerState state = EngineerState.For(player);
        if (baseId == EngineerSpellIds.QuickBurstBase)
        {
            result = state.QuickBurstAvailable ? result
                : NexusForever.Network.World.Message.Static.CastResult.PrereqCasterCast;
            return true;
        }
        if (baseId == EngineerSpellIds.FeedbackBase)
        {
            result = state.FeedbackAvailable ? result
                : NexusForever.Network.World.Message.Static.CastResult.PrereqCasterCast;
            return true;
        }
        if (state.ExoSuitActive)
            return true;
        return false;
    }

    public static void BeforeExecute(Spell spell,
        NexusForever.Game.Abstract.Entity.IPlayer player)
    {
        if (player.Class != NexusForever.Game.Static.Entity.Class.Engineer)
            return;
        uint spellId = spell.Parameters.SpellInfo.Entry.Id;
        if (spellId is EngineerSpellIds.ModeEradicateActive
            or EngineerSpellIds.ModeProvokeActive)
            EngineerState.For(player).EnableExoSuit();
        if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id == 25951u)
        {
            for (uint tick = 1u; tick <= 10u; tick++)
                spell.ScheduleAction(tick,
                    () => player.ModifyVital(
                        NexusForever.Game.Static.Entity.Vital.Volatility, 5f));
        }
    }

    public static void AfterEffects(Spell spell,
        NexusForever.Game.Abstract.Entity.IPlayer player)
    {
        if (player.Class != NexusForever.Game.Static.Entity.Class.Engineer)
            return;
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (baseId == EngineerSpellIds.QuickBurstBase)
            EngineerState.For(player).ConsumeQuickBurst();
        else if (baseId == EngineerSpellIds.FeedbackBase)
            EngineerState.For(player).ConsumeFeedback();
    }

    public static bool IsServerExecutedChannel(Spell spell)
        => spell.Parameters.SpellInfo.BaseInfo.Entry.Id is
            EngineerSpellIds.ElectrocuteBase or EngineerSpellIds.ParticleEjectorBase
            or EngineerSpellIds.FlakCannonBase or 25293u or 20428u or 47512u
            or 63039u;

    public static bool ShouldFinishRoot(Spell spell)
        => spell.Parameters.RootSpellInfo.Entry.Id == EngineerSpellIds.PulseBlast;
}
