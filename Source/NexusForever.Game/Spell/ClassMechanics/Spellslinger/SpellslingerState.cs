using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;

namespace NexusForever.Game.Spell.ClassMechanics.Spellslinger;

public sealed class SpellslingerState : ClassState
{
    public static SpellslingerState For(IPlayer player) => ClassStates.For<SpellslingerState>(player);

    public bool SpellSurgeActive { get; private set; }
    public uint? SpellSurgeBuffCastingId { get; set; }
    public bool FlameBurstAvailable => flameBurstTime > 0d;
    public uint? FlameBurstBuffCastingId { get; set; }
    public byte TrueShotTap { get; set; }
    public double TrueShotTapExpiresAt { get; set; }

    private double flameBurstTime;

    public void SetSpellSurgeActive(IPlayer player, bool active)
    {
        SpellSurgeActive = active;
        if (active || !SpellSurgeBuffCastingId.HasValue)
            return;

        RemoveBuff(player, SpellSurgeBuffCastingId.Value);
        SpellSurgeBuffCastingId = null;
    }

    public void EnableFlameBurst(IPlayer player)
    {
        if (FlameBurstBuffCastingId.HasValue)
            ConsumeFlameBurst(player);
        flameBurstTime = 5d;
    }

    public void ConsumeFlameBurst(IPlayer player)
    {
        flameBurstTime = 0d;
        if (!FlameBurstBuffCastingId.HasValue)
            return;

        RemoveBuff(player, FlameBurstBuffCastingId.Value);
        FlameBurstBuffCastingId = null;
    }

    protected override void OnUpdate(IPlayer player, double lastTick)
    {
        if (flameBurstTime <= 0d)
            return;

        flameBurstTime = Math.Max(0d, flameBurstTime - lastTick);
        if (flameBurstTime == 0d)
            ConsumeFlameBurst(player);
    }
}
