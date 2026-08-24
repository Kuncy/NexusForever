using System.Numerics;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Spell.ClassMechanics.Stalker;

public sealed class StalkerState
{
    private static readonly ConditionalWeakTable<IPlayer, StalkerState> states = new();

    public static StalkerState For(IPlayer player) => states.GetOrCreateValue(player);

    public bool StealthActive { get; private set; }
    public uint ActiveNanoSkinBaseId { get; private set; } = StalkerSpellIds.NanoSkinLethalBase;
    public bool PunishAvailable => punishTime > 0d;
    public uint? PunishBuffCastingId { get; set; }
    public bool SteadfastActive => steadfastTime > 0d;
    public bool NanoFieldActive => nanoFieldTime > 0d;
    public bool FalseRetreatActive => falseRetreatTime > 0d && falseRetreatPosition.HasValue;
    public Vector3? FalseRetreatPosition => falseRetreatPosition;

    private double punishTime;
    private double steadfastTime;
    private double nanoFieldTime;
    private double falseRetreatTime;
    private Vector3? falseRetreatPosition;
    private byte neutralizeStacks;
    private double neutralizeStackTime;
    private double decimateResetTime;
    private double stealthDamageProtectionTime;
    private readonly Dictionary<uint, long> analyzeWeaknessTargets = [];

    public void EnterStealth(uint nanoSkinBaseId)
    {
        ActiveNanoSkinBaseId = nanoSkinBaseId;
        StealthActive = true;
    }

    public void EnterStealth() => StealthActive = true;

    public void EnterProtectedStealth(double duration)
    {
        StealthActive = true;
        stealthDamageProtectionTime = duration;
    }

    public void BreakStealth() => StealthActive = false;

    public void OnDamageTaken()
    {
        if (stealthDamageProtectionTime <= 0d)
            BreakStealth();
    }

    public void EnablePunish(IPlayer player)
    {
        if (PunishAvailable)
            return;

        punishTime = 4d;
        player.CastSpell(StalkerSpellIds.PunishAvailableBuff, new SpellParameters());
    }

    public void ConsumePunish(IPlayer player)
    {
        punishTime = 0d;
        if (!PunishBuffCastingId.HasValue)
            return;

        player.EnqueueToVisible(new ServerSpellBuffRemove
        {
            CastingId = PunishBuffCastingId.Value,
            CasterId = player.Guid
        }, true);
        PunishBuffCastingId = null;
    }

    public void ActivateSteadfast() => steadfastTime = 4d;

    public bool ConsumeSteadfast()
    {
        if (!SteadfastActive)
            return false;
        steadfastTime = 0d;
        return true;
    }

    public void StartNanoField() => nanoFieldTime = 5.05d;
    public void EndNanoField() => nanoFieldTime = 0d;

    public void StartFalseRetreat(Vector3 position)
    {
        falseRetreatPosition = position;
        falseRetreatTime = 5d;
    }

    public void EndFalseRetreat()
    {
        falseRetreatPosition = null;
        falseRetreatTime = 0d;
    }

    public uint GetNeutralizeCost()
    {
        if (neutralizeStackTime <= 0d)
            neutralizeStacks = 0;
        return 15u + neutralizeStacks * 5u;
    }

    public void RecordNeutralizeCast()
    {
        neutralizeStacks = Math.Min((byte)4, (byte)(neutralizeStacks + 1));
        neutralizeStackTime = 8d;
    }

    public bool TryResetDecimate(IPlayer player)
    {
        if (decimateResetTime > 0d)
            return false;
        decimateResetTime = 5d;
        player.SpellManager.SetSpellCooldown(StalkerSpellIds.Decimate, 0d);
        return true;
    }

    public void MarkAnalyzeWeaknessTarget(uint targetGuid)
        => analyzeWeaknessTargets[targetGuid] = Environment.TickCount64 + 8000L;

    public bool IsAnalyzeWeaknessTarget(uint targetGuid)
    {
        if (!analyzeWeaknessTargets.TryGetValue(targetGuid, out long expiresAt))
            return false;
        if (Environment.TickCount64 <= expiresAt)
            return true;
        analyzeWeaknessTargets.Remove(targetGuid);
        return false;
    }

    public bool RemoveAnalyzeWeaknessTarget(uint targetGuid)
        => analyzeWeaknessTargets.Remove(targetGuid);

    public void Update(IPlayer player, double lastTick)
    {
        punishTime = Math.Max(0d, punishTime - lastTick);
        if (punishTime == 0d && PunishBuffCastingId.HasValue)
            ConsumePunish(player);
        steadfastTime = Math.Max(0d, steadfastTime - lastTick);
        neutralizeStackTime = Math.Max(0d, neutralizeStackTime - lastTick);
        decimateResetTime = Math.Max(0d, decimateResetTime - lastTick);
        stealthDamageProtectionTime = Math.Max(0d, stealthDamageProtectionTime - lastTick);

        nanoFieldTime = Math.Max(0d, nanoFieldTime - lastTick);
        falseRetreatTime = Math.Max(0d, falseRetreatTime - lastTick);
        if (falseRetreatTime == 0d)
            falseRetreatPosition = null;
    }
}
