using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Spell;
using NexusForever.Network.World.Message.Model;
using NexusForever.Script;
using NexusForever.Shared.Configuration;
using NexusForever.Shared.Game;

namespace NexusForever.Game.Entity
{
    /// <summary>
    /// An <see cref="ICreatureEntity"/> is an extension to <see cref="IUnitEntity"/> which contains logic specific to non player controlled combat entities.
    /// </summary>
    public abstract class CreatureEntity : UnitEntity, ICreatureEntity
    {
        private UpdateTimer respawnTimer;

        #region Dependency Injection

        public CreatureEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        public override void Initialise(EntityModel model)
        {
            base.Initialise(model);

            scriptCollection = ScriptManager.Instance.InitialiseEntityScripts<ICreatureEntity>(this);
        }

        public override void Update(double lastTick)
        {
            if (respawnTimer != null)
            {
                respawnTimer.Update(lastTick);
                if (respawnTimer.HasElapsed)
                    Respawn();
            }

            base.Update(lastTick);
        }

        protected override void OnDeath()
        {
            base.OnDeath();

            // Only persistent world spawns respawn. Dynamically created creatures have no
            // database entity id and must be managed by their owning event or system.
            if (EntityId == 0u || this is not INonPlayerEntity || respawnTimer != null)
                return;

            double duration = SharedConfiguration.Instance.Get<MapConfig>()?.CreatureRespawnTimer ?? 30d;
            respawnTimer = new UpdateTimer(Math.Max(0d, duration));
        }

        private void Respawn()
        {
            respawnTimer = null;

            SetTarget((IWorldEntity)null);
            ThreatManager.ClearThreatList();
            AuraManager.RemoveAll(AuraRemoveReason.Death);

            Absorption = 0u;
            Shield = MaxShieldCapacity;

            MovementManager.SetVelocityDefaults();
            MovementManager.SetMoveDefaults(false);
            MovementManager.SetPosition(LeashPosition, false);
            Relocate(LeashPosition);

            Health = MaxHealth;
            DeathState = null;
        }

        /// <summary>
        /// Set target to supplied <see cref="IUnitEntity"/>.
        /// </summary>
        /// <remarks>
        /// A null target will clear the current target.
        /// </remarks>
        public override void SetTarget(IWorldEntity target, uint threat = 0)
        {
            base.SetTarget(target, threat);

            if (target is IPlayer player)
            {
                // plays aggro sound at client, maybe more??
                player.Session.EnqueueMessageEncrypted(new ServerEntityAggroSwitch
                {
                    UnitId   = Guid,
                    TargetId = TargetGuid.Value
                });
            }
        }

        /// <summary>
        /// Invoked when <see cref="ICreatureEntity"/> is targeted by another <see cref="IUnitEntity"/>.
        /// </summary>
        public override void OnTargeted(IUnitEntity source)
        {
            base.OnTargeted(source);

            // client only processes threat list message if the source matches the current target
            if (InCombat && source is IPlayer player)
                ThreatManager.SendThreatList(player.Session);
        }

        /// <summary>
        /// Invoked when a new <see cref="IHostileEntity"/> is added to the threat list.
        /// </summary>
        public override void OnThreatAddTarget(IHostileEntity hostile)
        {
            ThreatManager.BroadcastThreatList();
            base.OnThreatAddTarget(hostile);
        }

        /// <summary>
        /// Invoked when an existing <see cref="IHostileEntity"/> is removed from the threat list.
        /// </summary>
        public override void OnThreatRemoveTarget(IHostileEntity hostile)
        {
            ThreatManager.BroadcastThreatList();
            base.OnThreatRemoveTarget(hostile);
        }

        /// <summary>
        /// Invoked when an existing <see cref="IHostileEntity"/> is update on the threat list.
        /// </summary>
        public override void OnThreatChange(IHostileEntity hostile)
        {
            ThreatManager.BroadcastThreatList();
            base.OnThreatChange(hostile);
        }

        protected override void RewardKillParticipants(IReadOnlyCollection<IPlayer> participants)
        {
            base.RewardKillParticipants(participants);
            LootManager.Instance.DropLoot(participants, this);
        }
    }
}
