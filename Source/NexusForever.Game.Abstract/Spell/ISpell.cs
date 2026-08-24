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
        /// Release a charge/release spell into the selected threshold spell.
        /// </summary>
        void ReleaseCharge(uint thresholdSpell4Id, double cooldown);

        /// <summary>
        /// Cast a child spell from the original caster against the supplied
        /// target after an optional delay.
        /// </summary>
        void CastProxySpell(uint spell4Id, IUnitEntity target, double delay = 0d, bool parentSpellSuccessfulHit = false);

        /// <summary>
        /// Keep this spell alive and execute an effect callback after a delay.
        /// Used by table-defined periodic damage and healing effects.
        /// </summary>
        void ScheduleAction(double delay, Action action);

        /// <summary>
        /// Broadcast one delayed/periodic effect result to nearby clients.
        /// </summary>
        void SendEffectGo(IUnitEntity target, ISpellTargetEffectInfo info);

        /// <summary>
        /// Record and consume whether this spell execution hit at least one attackable target.
        /// </summary>
        void RegisterSuccessfulHit();
        bool TryConsumeSuccessfulHit();

        bool IsMovingInterrupted();
    }
}
