using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using System.Numerics;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Template;
using NexusForever.Script.Template.AI;
using NexusForever.Shared;
using NexusForever.Shared.Game;

namespace NexusForever.Script.Main.AI
{
    //[ScriptFilterIgnore]
    public class CombatAI : IOwnedScript<ICreatureEntity>, IUnitScript
    {
        private static readonly CombatProfile defaultProfile = new(
            [5649u, 5652u], // Punch, Jab
            3f,
            []);

        private sealed class AbilityState
        {
            public CombatAbility Ability { get; }
            public double Remaining { get; private set; }

            public AbilityState(CombatAbility ability)
            {
                Ability = ability;
                ResetForEncounter();
            }

            public void Update(double lastTick)
            {
                Remaining = Math.Max(0d, Remaining - lastTick);
            }

            public void ResetCooldown()
            {
                Remaining = Ability.CooldownSeconds;
            }

            public void ResetForEncounter()
            {
                Remaining = Ability.InitialDelaySeconds;
            }
        }

        private ICreatureEntity owner;
        private CombatProfile profile;
        private AbilityState[] abilities = [];
        private bool profileResolved;

        private readonly IFactory<ISpellParameters> spellParametersFactory;
        private readonly IGameTableManager gameTableManager;

        private readonly UpdateTimer autoAttackTimer = new(TimeSpan.FromSeconds(1.5d));
        private readonly UpdateTimer chaseTimer = new(TimeSpan.FromSeconds(0.5d));
        private int autoAttackIndex;
        private bool engaged;

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
            ResolveProfile();

            if (!owner.IsAlive || !owner.TargetGuid.HasValue)
            {
                ResetEncounter();
                return;
            }

            IUnitEntity target = owner.Map?.GetEntity<IUnitEntity>(owner.TargetGuid.Value);
            if (target == null || !target.IsAlive)
            {
                ResetEncounter();
                return;
            }

            engaged = true;

            autoAttackTimer.Update(lastTick);
            chaseTimer.Update(lastTick);
            foreach (AbilityState ability in abilities)
                ability.Update(lastTick);

            // Do not move or start another ability while a telegraphed cast is winding up.
            if (owner.GetActiveSpell(spell => spell.IsCasting) != null)
                return;

            if (DoSpecialAbility(target))
            {
                autoAttackTimer.Reset();
                return;
            }

            if (autoAttackTimer.HasElapsed && DoAutoAttack(target))
                autoAttackTimer.Reset();

            if (chaseTimer.HasElapsed)
            {
                DoChase(target);
                chaseTimer.Reset();
            }
        }

        private void ResolveProfile()
        {
            if (profileResolved)
                return;

            CombatProfile resolvedProfile = null;
            owner.InvokeScriptCollection<ICreatureCombatProfileScript>(script => resolvedProfile ??= script.Profile);

            profile = resolvedProfile is { AutoAttacks.Count: > 0, PreferredRange: > 0f }
                ? resolvedProfile
                : defaultProfile;
            abilities = (profile.Abilities ?? []).Select(ability => new AbilityState(ability)).ToArray();
            profileResolved = true;
        }

        private void ResetEncounter()
        {
            if (!engaged)
                return;

            engaged = false;
            autoAttackIndex = 0;
            autoAttackTimer.Reset();
            chaseTimer.Reset();
            foreach (AbilityState ability in abilities)
                ability.ResetForEncounter();
        }

        private bool DoSpecialAbility(IUnitEntity target)
        {
            foreach (AbilityState ability in abilities)
            {
                if (ability.Remaining > 0d || !TryCast(target, ability.Ability.Spell4Id))
                    continue;

                ability.ResetCooldown();
                return true;
            }

            return false;
        }

        private bool DoAutoAttack(IUnitEntity target)
        {
            uint spell4Id = profile.AutoAttacks[autoAttackIndex];
            if (!TryCast(target, spell4Id))
                return false;

            autoAttackIndex = (autoAttackIndex + 1) % profile.AutoAttacks.Count;
            return true;
        }

        private bool TryCast(IUnitEntity target, uint spell4Id)
        {
            Spell4Entry spell4Entry = gameTableManager.Spell4.GetEntry(spell4Id);
            if (spell4Entry == null)
                return false;

            float distance = Vector3.Distance(owner.Position, target.Position);
            if (distance > spell4Entry.TargetMaxRange + owner.HitRadius + target.HitRadius)
                return false;

            ISpellParameters parameters = spellParametersFactory.Resolve();
            parameters.PrimaryTargetId = target.Guid;
            owner.MovementManager.SetRotationFaceUnit(target.Guid);
            owner.CastSpell(spell4Id, parameters);
            return true;
        }

        private void DoChase(IUnitEntity target)
        {
            float stopDistance = profile.PreferredRange + owner.HitRadius + target.HitRadius;
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
