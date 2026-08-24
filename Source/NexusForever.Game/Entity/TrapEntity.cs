using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Spell;
using NexusForever.GameTable;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Entity
{
    public class TrapEntity : WorldEntity, ITrapEntity
    {
        public override EntityType Type => EntityType.Trap;

        private uint ownerGuid;
        private uint triggerSpellId;
        private double remainingTime;
        private bool triggered;

        #region Dependency Injection

        public TrapEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        public void Initialise(IPlayer owner, uint creatureId, uint triggerSpell,
            uint duration, float triggerRadius)
        {
            ownerGuid = owner.Guid;
            triggerSpellId = triggerSpell;
            remainingTime = duration / 1000d;
            Initialise(creatureId);

            var displayGroup = GameTableManager.Instance.Creature2DisplayGroupEntry.Entries
                .FirstOrDefault(entry => entry.Creature2DisplayGroupId
                    == CreatureEntry.Creature2DisplayGroupId);
            DisplayInfo = displayGroup?.Creature2DisplayInfoId ?? 0u;
            Faction1 = owner.Faction1;
            Faction2 = owner.Faction2;
            SetInRangeCheck(triggerRadius);
        }

        public override void CheckEntityInRange(IGridEntity target)
        {
            base.CheckEntityInRange(target);
            if (triggered || target is not IUnitEntity unit
                || !RangeCheck.HasValue
                || Position.GetDistance(unit.Position) >= RangeCheck.Value)
                return;

            IPlayer owner = Map?.GetEntity<IPlayer>(ownerGuid);
            if (owner == null || !owner.CanAttack(unit))
                return;

            triggered = true;
            owner.CastSpell(triggerSpellId, new SpellParameters
            {
                PrimaryTargetId = unit.Guid
            });
            RemoveFromMap();
        }

        public override void Update(double lastTick)
        {
            base.Update(lastTick);
            remainingTime = Math.Max(0d, remainingTime - lastTick);
            if (!triggered && remainingTime == 0d)
                RemoveFromMap();
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new TrapEntityModel
            {
                CreatureId = CreatureId,
                OwnerId = ownerGuid
            };
        }
    }
}
