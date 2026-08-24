using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Abstract.Spell
{
    /// <summary>
    /// Per character state for the class specific mechanics of a <see cref="IPlayer"/>.
    /// </summary>
    public interface IClassState
    {
        /// <summary>
        /// Invoked once per world update to advance any timers held by the state.
        /// </summary>
        void Update(IPlayer player, double lastTick);
    }
}
