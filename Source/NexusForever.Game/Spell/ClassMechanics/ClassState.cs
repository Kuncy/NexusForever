using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Spell.ClassMechanics;

/// <summary>
/// Base implementation of <see cref="IClassState"/> that supplies the clock every class mechanic times against.
/// </summary>
/// <remarks>
/// <see cref="Elapsed"/> accumulates the world tick, the same clock the countdowns already used. Deadlines are
/// stored as a point on it rather than as a wall clock timestamp, so a paused or time scaled instance moves the
/// countdowns and the deadlines together instead of only one of the two.
/// </remarks>
public abstract class ClassState : IClassState
{
    /// <summary>
    /// Seconds of world time since this state came into existence.
    /// </summary>
    public double Elapsed { get; private set; }

    public void Update(IPlayer player, double lastTick)
    {
        Elapsed += lastTick;
        OnUpdate(player, lastTick);
    }

    /// <summary>
    /// Returns a deadline <paramref name="seconds"/> from now, to be compared against <see cref="Elapsed"/>.
    /// </summary>
    public double DeadlineIn(double seconds)
    {
        return Elapsed + seconds;
    }

    /// <summary>
    /// Returns whether <paramref name="deadline"/> is still in the future.
    /// </summary>
    public bool IsPending(double deadline)
    {
        return Elapsed < deadline;
    }

    protected virtual void OnUpdate(IPlayer player, double lastTick)
    {
    }

    /// <summary>
    /// Tell the client to drop the buff icon of a self cast class buff.
    /// </summary>
    /// <remarks>
    /// Needed because these buffs outlive the spell that applied them, so the client never receives the
    /// ServerSpellFinish that would otherwise wear them off.
    /// </remarks>
    protected static void RemoveBuff(IPlayer player, uint castingId)
    {
        player.EnqueueToVisible(new ServerSpellBuffRemove
        {
            CastingId = castingId,
            CasterId  = player.Guid
        }, true);
    }
}
