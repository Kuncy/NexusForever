using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Medic;

public sealed class MedicState
{
    private static readonly ConditionalWeakTable<IPlayer, MedicState> states = new();

    public static MedicState For(IPlayer player) => states.GetOrCreateValue(player);

    private byte powerChargeStacks;
    private double atomizeTime;
    private double dualShockTime;
    private readonly Dictionary<uint, long> nerveInductionTargets = [];
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
        => nerveInductionTargets[targetGuid] = Environment.TickCount64 + 6000L;

    public bool IsNerveInductionTarget(uint targetGuid)
    {
        if (!nerveInductionTargets.TryGetValue(targetGuid, out long expiry))
            return false;
        if (Environment.TickCount64 <= expiry)
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

    public void Update(double lastTick)
    {
        atomizeTime = Math.Max(0d, atomizeTime - lastTick);
        dualShockTime = Math.Max(0d, dualShockTime - lastTick);
        fusionProbeTime = Math.Max(0d, fusionProbeTime - lastTick);
    }
}
