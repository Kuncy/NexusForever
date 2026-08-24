using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;

namespace NexusForever.Game.Spell.ClassMechanics.Esper;

public sealed class EsperState : ClassState
{
    public static EsperState For(IPlayer player) => ClassStates.For<EsperState>(player);

    /// <remarks>
    /// The esper holds no timed state, psi points live on <see cref="Game.Static.Entity.Vital.Resource1"/>.
    /// </remarks>
    protected override void OnUpdate(IPlayer player, double lastTick)
    {
    }

    public uint SnapshotPsiPoints(IPlayer player)
    {
        int maximum = Math.Max(1, (int)MathF.Floor(
            player.GetVitalMaximum(Game.Static.Entity.Vital.Resource1)));
        return (uint)Math.Clamp((int)MathF.Floor(player.GetVitalValue(
            Game.Static.Entity.Vital.Resource1)), 1, maximum);
    }
}
