using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Medic;

public sealed class MedicState : ClassState
{
    public static MedicState For(IPlayer player) => ClassStates.For<MedicState>(player);

    private byte powerChargeStacks;
    private double atomizeTime;
    private double dualShockTime;
    private readonly Dictionary<uint, double> nerveInductionTargets = [];
    private double fusionProbeTime;

    public bool AtomizeAvailable => atomizeTime > 0d;
    public bool DualShockAvailable => dualShockTime > 0d;
    public bool FusionProbeActive => fusionProbeTime > 0d;

    public void EnableAtomize() => atomizeTime = 8d;
    public void EnableDualShock() => dualShockTime = 8d;
    public void ConsumeAtomize() => atomizeTime = 0d;
    public void ConsumeDualShock() => dualShockTime = 0d;
    public void StartFusionProbe() => fusionProbeTime = 4.25d;
    public void EndFusionProbe() => fusionProbeTime = 0d;
    public void MarkNerveInduction(uint targetGuid)
        => nerveInductionTargets[targetGuid] = DeadlineIn(6d);

    public bool IsNerveInductionTarget(uint targetGuid)
    {
        if (!nerveInductionTargets.TryGetValue(targetGuid, out double expiry))
            return false;
        if (IsPending(expiry))
            return true;
        nerveInductionTargets.Remove(targetGuid);
        return false;
    }

    public void AddPowerCharge(IPlayer player)
    {
        powerChargeStacks++;
        if (powerChargeStacks < 3)
            return;

        powerChargeStacks = 0;
        player.ModifyVital(Vital.MedicCore, 1f);
    }

    protected override void OnUpdate(IPlayer player, double lastTick)
    {
        atomizeTime = Math.Max(0d, atomizeTime - lastTick);
        dualShockTime = Math.Max(0d, dualShockTime - lastTick);
        fusionProbeTime = Math.Max(0d, fusionProbeTime - lastTick);

        SweepNerveInductionTargets();
    }

    /// <remarks>
    /// Expired entries are only dropped lazily when the same guid is queried again, without this sweep a medic
    /// marking many different targets accumulates entries indefinitely and a recycled guid can hit a stale mark.
    /// </remarks>
    private void SweepNerveInductionTargets()
    {
        if (nerveInductionTargets.Count == 0)
            return;

        foreach (uint targetGuid in nerveInductionTargets
            .Where(t => !IsPending(t.Value))
            .Select(t => t.Key)
            .ToList())
            nerveInductionTargets.Remove(targetGuid);
    }
}
