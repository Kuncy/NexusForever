using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Esper;

public sealed class EsperState
{
    private static readonly ConditionalWeakTable<IPlayer, EsperState> states = new();

    public static EsperState For(IPlayer player) => states.GetOrCreateValue(player);

    public uint SnapshotPsiPoints(IPlayer player)
        => (uint)Math.Clamp((int)MathF.Floor(player.GetVitalValue(
            Game.Static.Entity.Vital.Resource1)), 1, 5);
}
