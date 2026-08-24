using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Spell.ClassMechanics.Spellslinger;

public sealed class SpellslingerState
{
    private static readonly ConditionalWeakTable<IPlayer, SpellslingerState> states = new();

    public static SpellslingerState For(IPlayer player) => states.GetOrCreateValue(player);

    public bool SpellSurgeActive { get; private set; }
    public uint? SpellSurgeBuffCastingId { get; set; }
    public bool FlameBurstAvailable => flameBurstTime > 0d;
    public uint? FlameBurstBuffCastingId { get; set; }
    public byte TrueShotTap { get; set; }
    public long TrueShotTapExpiresAt { get; set; }

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

    public void Update(IPlayer player, double lastTick)
    {
        if (flameBurstTime <= 0d)
            return;

        flameBurstTime = Math.Max(0d, flameBurstTime - lastTick);
        if (flameBurstTime == 0d)
            ConsumeFlameBurst(player);
    }

    private static void RemoveBuff(IPlayer player, uint castingId)
    {
        player.EnqueueToVisible(new ServerSpellBuffRemove
        {
            CastingId = castingId,
            CasterId  = player.Guid
        }, true);
    }
}
