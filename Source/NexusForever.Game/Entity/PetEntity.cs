using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Map.Search;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Shared.Game;
using NLog;
using System.Runtime.CompilerServices;

namespace NexusForever.Game.Entity
{
    public class PetEntity : WorldEntity, IPetEntity
    {
        private const float FollowDistance = 3f;
        private const float FollowMinRecalculateDistance = 5f;

        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ConditionalWeakTable<IPlayer, Queue<PetEntity>> combatPets = new();

        public override EntityType Type => EntityType.Pet;

        public uint OwnerGuid { get; private set; }
        public Creature2DisplayGroupEntryEntry Creature2DisplayGroup { get; private set; }

        private readonly UpdateTimer followTimer = new(1d);
        private bool combatPet;
        private uint attackSpellId;
        private float attackRange;
        private double remainingDuration;
        private double attackInterval;
        private double attackTime;

        #region Dependency Injection

        public PetEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        public void Initialise(IPlayer owner, uint creature)
        {
            OwnerGuid = owner.Guid;
            Initialise(creature);

            Creature2DisplayGroup = GameTableManager.Instance.Creature2DisplayGroupEntry.Entries.SingleOrDefault(x => x.Creature2DisplayGroupId == CreatureEntry.Creature2DisplayGroupId);
            SetVisualInfo(Creature2DisplayGroup?.Creature2DisplayInfoId ?? 0u, 0);

            SetBaseProperty(Property.BaseHealth, 800.0f);

            SetStat(Stat.Health, 800u);
            SetStat(Stat.Level, 3u);
            SetStat(Stat.Sheathed, 0u);
        }

        public void InitialiseCombat(IPlayer owner, uint creature, uint attackSpell,
            uint attackInterval, float range, uint duration)
        {
            combatPet = true;
            attackSpellId = attackSpell;
            attackRange = range > 0f ? range : 15f;
            remainingDuration = duration / 1000d;
            Initialise(owner, creature);
            this.attackInterval = Math.Max(0.25d, attackInterval / 1000d);
            attackTime = this.attackInterval;
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new PetEntityModel
            {
                CreatureId  = CreatureEntry.Id,
                OwnerId     = OwnerGuid,
                Name        = ""
            };
        }

        public override void OnAddToMap(IBaseMap map, uint guid, Vector3 vector)
        {
            base.OnAddToMap(map, guid, vector);

            IPlayer owner = GetVisible<IPlayer>(OwnerGuid);
            if (owner == null)
            {
                // this shouldn't happen, log it anyway
                log.Error($"VanityPet {Guid} has lost it's owner {OwnerGuid}!");
                RemoveFromMap();
                return;
            }

            if (!combatPet)
                owner.VanityPetGuid = Guid;
            else if (owner.Class == Class.Engineer)
            {
                Queue<PetEntity> pets = combatPets.GetOrCreateValue(owner);
                pets.Enqueue(this);
                while (pets.Count > 2)
                {
                    PetEntity oldest = pets.Dequeue();
                    if (oldest != this && oldest.InWorld)
                        oldest.RemoveFromMap();
                }
            }

            owner.EnqueueToVisible(new ServerPathScientistUnitScanParameters
            {
                UnitId = Guid,
                ScanRewardFlags  = 0,
                IsScannable  = true
            }, true);
        }

        public override void OnEnqueueRemoveFromMap()
        {
            followTimer.Reset(false);
            OwnerGuid = 0u;
        }

        public override void Update(double lastTick)
        {
            base.Update(lastTick);
            Follow(lastTick);
            Attack(lastTick);

            if (remainingDuration > 0d)
            {
                remainingDuration = Math.Max(0d, remainingDuration - lastTick);
                if (remainingDuration == 0d)
                    RemoveFromMap();
            }
        }

        private void Attack(double lastTick)
        {
            if (!combatPet || attackSpellId == 0u)
                return;
            attackTime -= lastTick;
            if (attackTime > 0d)
                return;
            attackTime = attackInterval;

            IPlayer owner = GetVisible<IPlayer>(OwnerGuid);
            if (owner == null || !owner.InCombat)
                return;
            var check = new SearchCheckRange<IUnitEntity>(Position, attackRange);
            IUnitEntity target = Map.Search(Position, attackRange, check)
                .Where(owner.CanAttack)
                .OrderBy(unit => Vector3.DistanceSquared(Position, unit.Position))
                .FirstOrDefault();
            if (target != null)
                owner.CastSpell(attackSpellId, new NexusForever.Game.Spell.SpellParameters
                {
                    PrimaryTargetId = target.Guid
                });
        }

        private void Follow(double lastTick)
        {
            followTimer.Update(lastTick);
            if (!followTimer.HasElapsed)
                return;

            IPlayer owner = GetVisible<IPlayer>(OwnerGuid);
            if (owner == null)
            {
                // this shouldn't happen, log it anyway
                log.Error($"VanityPet {Guid} has lost it's owner {OwnerGuid}!");
                RemoveFromMap();
                return;
            }

            // only recalculate the path to owner if distance is significant
            float distance = owner.Position.GetDistance(Position);
            if (distance < FollowMinRecalculateDistance)
                return;

            MovementManager.Follow(owner, FollowDistance);

            followTimer.Reset();
        }
    }
}
