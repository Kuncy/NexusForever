using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;

namespace NexusForever.Game.Abstract.Spell
{
    public interface ISpell : IDisposable, IUpdate
    {
        ISpellParameters Parameters { get; }
        uint CastingId { get; }
        bool IsCasting { get; }
        bool IsFinished { get; }

        IUnitEntity Caster { get; }

        /// <summary>
        /// Begin cast, checking prerequisites before initiating.
        /// </summary>
        void Cast();

        /// <summary>
        /// Cancel cast with supplied <see cref="CastResult"/>.
        /// </summary>
        void CancelCast(CastResult result);

        void SucceedClientInteraction();
        void FailClientInteraction();

        /// <summary>
        /// Cast a child spell from the original caster against the supplied
        /// target after an optional delay.
        /// </summary>
        void CastProxySpell(uint spell4Id, IUnitEntity target, double delay = 0d);

        bool IsMovingInterrupted();
    }
}
