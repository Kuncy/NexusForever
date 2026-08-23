using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using System.Numerics;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Template;
using NexusForever.Shared;
using NexusForever.Shared.Game;

namespace NexusForever.Script.Main.AI
{
    //[ScriptFilterIgnore]
    public class CombatAI : IOwnedScript<ICreatureEntity>, IUnitScript
    {
        private ICreatureEntity owner;

        private readonly IFactory<ISpellParameters> spellParametersFactory;
        private readonly IGameTableManager gameTableManager;

        private readonly uint[] autoAttacks = [5649u, 5652u];
        private readonly UpdateTimer autoAttackTimer = new(TimeSpan.FromSeconds(1.5d));
        private readonly UpdateTimer chaseTimer = new(TimeSpan.FromSeconds(0.5d));
        private int autoAttackIndex;
        private float attackRange = 3f;

        public CombatAI(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
        {
            this.spellParametersFactory = spellParametersFactory;
            this.gameTableManager = gameTableManager;
        }

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void Update(double lastTick)
        {
            if (!owner.IsAlive || !owner.TargetGuid.HasValue)
                return;

            IUnitEntity target = owner.Map?.GetEntity<IUnitEntity>(owner.TargetGuid.Value);
            if (target == null || !target.IsAlive)
                return;

            autoAttackTimer.Update(lastTick);
            if (autoAttackTimer.HasElapsed)
            {
                DoAutoAttack(target);
                autoAttackTimer.Reset();
            }

            chaseTimer.Update(lastTick);
            if (chaseTimer.HasElapsed)
            {
                DoChase(target);
                chaseTimer.Reset();
            }
        }

        private void DoAutoAttack(IUnitEntity target)
        {
            uint spell4Id = autoAttacks[autoAttackIndex];
            autoAttackIndex = (autoAttackIndex + 1) % autoAttacks.Length;

            Spell4Entry spell4Entry = gameTableManager.Spell4.GetEntry(spell4Id);
            if (spell4Entry == null)
                return;

            attackRange = MathF.Min(attackRange, spell4Entry.TargetMaxRange);
            float distance = Vector3.Distance(owner.Position, target.Position);
            if (distance > spell4Entry.TargetMaxRange + owner.HitRadius + target.HitRadius)
                return;

            ISpellParameters parameters = spellParametersFactory.Resolve();
            parameters.PrimaryTargetId = target.Guid;
            owner.CastSpell(spell4Id, parameters);
        }

        private void DoChase(IUnitEntity target)
        {
            float stopDistance = attackRange + owner.HitRadius + target.HitRadius;
            if (Vector3.Distance(owner.Position, target.Position) <= stopDistance)
                return;

            owner.MovementManager.Follow(target, MathF.Max(1f, stopDistance * 0.5f));
        }

        public void OnThreatAddTarget(IHostileEntity hostile)
        {
            SelectTarget();
        }

        public void OnThreatRemoveTarget(IHostileEntity hostile)
        {
            SelectTarget();
        }

        public void OnThreatChange(IHostileEntity hostile)
        {
            SelectTarget();
        }

        protected virtual void SelectTarget()
        {
            IHostileEntity hostile = owner.ThreatManager.GetTopHostile();
            if (hostile == null)
            {
                owner.SetTarget((IWorldEntity)null);
                return;
            }

            if (owner.TargetGuid == hostile.HatedUnitId)
                return;

            owner.SetTarget(hostile.HatedUnitId, hostile.Threat);
        }
    }
}
