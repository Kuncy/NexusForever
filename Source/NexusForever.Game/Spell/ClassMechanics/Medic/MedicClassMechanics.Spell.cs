namespace NexusForever.Game.Spell.ClassMechanics.Medic;

public sealed partial class MedicClassMechanics
{
    public bool TryCheckPrerequisites(Spell spell,
        NexusForever.Game.Abstract.Entity.IPlayer player,
        out NexusForever.Network.World.Message.Static.CastResult result)
    {
        result = NexusForever.Network.World.Message.Static.CastResult.Ok;
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        MedicState state = MedicState.For(player);
        if (baseId == MedicSpellIds.AtomizeBase)
        {
            result = state.AtomizeAvailable ? result
                : NexusForever.Network.World.Message.Static.CastResult.PrereqCasterCast;
            return true;
        }
        if (baseId == MedicSpellIds.DualShockBase)
        {
            result = state.DualShockAvailable ? result
                : NexusForever.Network.World.Message.Static.CastResult.PrereqCasterCast;
            return true;
        }
        return false;
    }

    public void BeforeExecute(Spell spell,
        NexusForever.Game.Abstract.Entity.IPlayer player)
    {
        if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id == MedicSpellIds.EnergizeBase)
            player.ModifyVital(NexusForever.Game.Static.Entity.Vital.MedicCore,
                player.GetVitalMaximum(NexusForever.Game.Static.Entity.Vital.MedicCore));
        if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id == MedicSpellIds.RechargeBase)
            spell.Parameters.ClassResourceSnapshot = (uint)Math.Clamp(
                (int)MathF.Floor(player.GetVitalValue(
                    NexusForever.Game.Static.Entity.Vital.MedicCore)), 1, 4);
        if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id == 63410u)
        {
            MedicState state = MedicState.For(player);
            spell.Parameters.ClassResourceSnapshot = state.FusionProbeActive ? 2u : 1u;
            if (state.FusionProbeActive)
                state.EndFusionProbe();
            else
                state.StartFusionProbe();
        }
    }

    public void AfterEffects(Spell spell,
        NexusForever.Game.Abstract.Entity.IPlayer player)
    {
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (baseId == MedicSpellIds.AtomizeBase)
            MedicState.For(player).ConsumeAtomize();
        else if (baseId == MedicSpellIds.DualShockBase)
            MedicState.For(player).ConsumeDualShock();
    }

    public bool IsServerExecutedChannel(Spell spell)
        => spell.Parameters.SpellInfo.BaseInfo.Entry.Id is
            MedicSpellIds.QuantumCascadeBase or 38201u or 38210u or 25820u;

    public bool ShouldFinishRoot(Spell spell)
        => spell.Parameters.RootSpellInfo.Entry.Id is MedicSpellIds.Discharge
            or MedicSpellIds.Emission;
}
