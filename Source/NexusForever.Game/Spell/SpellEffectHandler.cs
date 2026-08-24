using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Combat;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Spell
{
    public static class SpellHandler
    {
        [SpellEffectHandler(SpellEffectType.SetBusy)]
        public static void HandleEffectSetBusy(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // State is represented by the effect in ServerSpellGo. No separate
            // server-side property exists on this branch.
        }

        [SpellEffectHandler(SpellEffectType.Activate)]
        public static void HandleEffectActivate(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (spell.Parameters.ActivationTargetGuid == 0u || spell.Caster is not IPlayer player)
                return;
            if (target.Guid != spell.Parameters.ActivationTargetGuid)
                return;

            uint[] targetGroupIds = AssetManager.Instance.GetTargetGroupsForCreatureId(target.CreatureId)?.ToArray() ?? [];
            var questObjectives = spell.Parameters.QuestEntityActivation
                ? player.QuestManager.GetActiveQuests()
                    .Where(q => q.State == Game.Static.Quest.QuestState.Accepted)
                    .SelectMany(q => q)
                    .Where(o => !o.IsComplete()
                        && ((o.ObjectiveInfo.Entry.Data == target.CreatureId
                                && o.ObjectiveInfo.Type is Game.Static.Quest.QuestObjectiveType.ActivateEntity
                                    or Game.Static.Quest.QuestObjectiveType.ActivateEntity2
                                    or Game.Static.Quest.QuestObjectiveType.SucceedCSI)
                            || (targetGroupIds.Contains(o.ObjectiveInfo.Entry.Data)
                                && o.ObjectiveInfo.Type is Game.Static.Quest.QuestObjectiveType.ActivateTargetGroupChecklist
                                    or Game.Static.Quest.QuestObjectiveType.ActivateTargetGroup)))
                    .Select(o => (Objective: o, Progress: o.Progress))
                    .ToArray()
                : [];

            player.QuestManager.ObjectiveUpdate(Game.Static.Quest.QuestObjectiveType.ActivateEntity, target.CreatureId, 1u);
            player.QuestManager.ObjectiveUpdate(Game.Static.Quest.QuestObjectiveType.ActivateEntity2, target.CreatureId, 1u);
            player.QuestManager.ObjectiveUpdate(Game.Static.Quest.QuestObjectiveType.SucceedCSI, target.CreatureId, 1u);
            foreach (uint targetGroupId in targetGroupIds)
            {
                uint checklistProgress = target is ISimpleEntity simple
                    ? 1u << simple.QuestChecklistIdx
                    : 1u;
                player.QuestManager.ObjectiveUpdate(Game.Static.Quest.QuestObjectiveType.ActivateTargetGroupChecklist, targetGroupId, checklistProgress);
                player.QuestManager.ObjectiveUpdate(Game.Static.Quest.QuestObjectiveType.ActivateTargetGroup, targetGroupId, 1u);
            }

            target.OnActivateCast(player);

            if (!spell.Parameters.QuestEntityActivation)
                return;

            bool questProgressed = questObjectives.Any(o => o.Objective.Progress > o.Progress);
            if (!questProgressed)
            {
                player.CancelQuestEntityActivation(target.Guid);
                return;
            }
            if (!player.CompleteQuestEntityActivation(target.Guid))
                return;

            // Let the client render the activation effect first, then remove
            // only this player's representation of the completed quest object.
            player.Session.Events.EnqueueEvent(new DelayEvent(TimeSpan.FromMilliseconds(250), () =>
            {
                player.Session.EnqueueMessageEncrypted(new ServerEntityDestroy
                {
                    Guid     = target.Guid,
                    Unknown0 = true
                });
            }));
        }

        [SpellEffectHandler(SpellEffectType.VitalModifier)]
        public static void HandleEffectVitalModifier(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // Pulse Blast's hidden +15 Volatility spell is guarded by an
            // InCombat prerequisite, which is not implemented by the generic
            // prerequisite system on this branch. The ability tooltip and
            // Spell4 data identify this as its normal resource gain.
            if (spell.Parameters.SpellInfo.Entry.Id == 42148u
                && target is IPlayer { Class: Game.Static.Entity.Class.Engineer }
                && (Vital)info.Entry.DataBits00 == Vital.Resource1)
            {
                target.ModifyVital(Vital.Volatility, info.Entry.DataBits01);
                return;
            }

            uint? warriorKineticEnergy = spell.Parameters.SpellInfo.Entry.Id switch
            {
                53865u or 54587u => 180u,
                79652u or 81699u => 150u,
                53867u or 83759u => 150u,
                46712u or 54606u => 250u,
                46713u => 125u,
                88546u => 500u,
                _ => null
            };
            if (warriorKineticEnergy.HasValue
                && target is IPlayer { Class: Game.Static.Entity.Class.Warrior })
            {
                // These generator spells contain mutually exclusive UnderSpell
                // prerequisites for stance/buff variants. Until persistent spell
                // effects are implemented, apply only the normal build value.
                if ((Vital)info.Entry.DataBits00 == Vital.Resource1
                    && info.Entry.DataBits01 == warriorKineticEnergy.Value)
                    target.ModifyVital(Vital.KineticCell, warriorKineticEnergy.Value);
                return;
            }

            if (info.Entry.PrerequisiteIdCasterApply != 0u
                && spell.Caster is IPlayer player
                && !PrerequisiteManager.Instance.Meets(player, info.Entry.PrerequisiteIdCasterApply))
                return;

            int amount = unchecked((int)info.Entry.DataBits01);
            if (amount == 0)
                return;

            if (amount < 0
                && target is IPlayer { Class: Game.Static.Entity.Class.Warrior, WarriorOverdriveActive: true })
                return;

            target.ModifyVital((Vital)info.Entry.DataBits00, amount);
        }

        [SpellEffectHandler(SpellEffectType.ForcedMove)]
        public static void HandleEffectForcedMove(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // Gate, Leap and Bum Rush all store their authoritative forward
            // travel distance as a float in DataBits01.
            if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id
                is not (20325u or 27236u or 37961u))
                return;

            void Move()
            {
                float distance = BitConverter.UInt32BitsToSingle(info.Entry.DataBits01);
                if (distance <= 0f)
                    return;

                float yaw = -target.Rotation.X;
                Vector3 forward = new(MathF.Cos(yaw), 0f, MathF.Sin(yaw));
                target.MovementManager.SetPosition(target.Position + forward * distance, false);
            }

            if (info.Entry.DelayTime == 0u)
                Move();
            else
                spell.ScheduleAction(info.Entry.DelayTime / 1000d, Move);
        }

        [SpellEffectHandler(SpellEffectType.CCStateSet)]
        public static void HandleEffectCCStateSet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // Interrupt armor absorbs one hard-CC application. Kick, Flash
            // Bang and Grapple encode the amount to destroy in DataBits03.
            uint interruptArmorDamage = info.Entry.DataBits03;
            if (interruptArmorDamage > 0u && target.InterruptArmor > 0u)
            {
                target.InterruptArmor = target.InterruptArmor > interruptArmorDamage
                    ? target.InterruptArmor - interruptArmorDamage
                    : 0u;
                info.DropEffect = true;
                return;
            }

            // Taunt/Intimidate (CC state 13) places the Warrior just above the
            // current top threat so creature AI actually changes target.
            if (info.Entry.DataBits00 == 13u && target.CanAttack(spell.Caster))
            {
                uint topThreat = target.ThreatManager.Any()
                    ? target.ThreatManager.Max(h => h.Threat)
                    : 0u;
                uint currentThreat = target.ThreatManager.GetHostile(spell.Caster.Guid)?.Threat ?? 0u;
                int delta = (int)Math.Min(int.MaxValue, topThreat - Math.Min(topThreat, currentThreat) + 1u);
                target.ThreatManager.UpdateThreat(spell.Caster, delta);
            }

            // Preserve the table effect in ServerSpellGo so the client applies
            // the matching root/snare/stun presentation. Spatial Shift also
            // requires an authoritative server-side position swap.
            if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id == 16454u
                && info.Entry.DataBits00 == 19u)
            {
                spell.ScheduleAction(info.Entry.DelayTime / 1000d, () =>
                {
                    Vector3 casterPosition = spell.Caster.Position;
                    Vector3 targetPosition = target.Position;
                    spell.Caster.MovementManager.SetPosition(targetPosition, false);
                    target.MovementManager.SetPosition(casterPosition, false);
                });
            }
        }

        [SpellEffectHandler(SpellEffectType.ModifyInterruptArmor)]
        public static void HandleEffectModifyInterruptArmor(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            uint amount = info.Entry.DataBits00;
            target.InterruptArmor += amount;

            if (info.Entry.DurationTime > 0u)
                spell.ScheduleAction(info.Entry.DurationTime / 1000d,
                    () => target.InterruptArmor = target.InterruptArmor > amount
                        ? target.InterruptArmor - amount
                        : 0u);
        }

        [SpellEffectHandler(SpellEffectType.Proc)]
        public static void HandleEffectProc(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // Plasma Wall's proc payload is its half-second damage pulse. The
            // old runtime did not have a persistent proc dispatcher, so run
            // the ten table-defined pulses for its five-second duration.
            if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id == 23169u
                && info.Entry.DataBits01 == 57852u)
            {
                for (uint tick = 0u; tick < 10u; tick++)
                    spell.CastProxySpell(57852u, spell.Caster, tick * 0.5d);
                return;
            }

            // Sentinel's Guard retaliates against an attacker of the guarded
            // ally for the table-defined 18-second duration.
            if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id == 37924u
                && info.Entry.DataBits01 == 54400u
                && target is UnitEntity guardedUnit)
            {
                guardedUnit.SetReactiveDamage(spell.Caster, info.Entry.DataBits01,
                    info.Entry.DurationTime);
                return;
            }

            // Healing Salve: taking damage triggers its heal at most once every
            // two seconds for the table-defined 12-second duration.
            if (info.Entry.DataBits00 == 16u
                && info.Entry.DataBits01 != 0u
                && target is UnitEntity unit)
            {
                unit.SetReactiveHeal(spell.Caster, info.Entry.DataBits01,
                    info.Entry.DurationTime);
                return;
            }

            // Other proc families require the generic persistent aura/proc
            // system. Keep their effect visible to the client without firing
            // the payload unconditionally.
        }

        [SpellEffectHandler(SpellEffectType.Damage)]
        public static void HandleEffectDamage(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // Mind Burst has one mutually exclusive damage row for each of the
            // five possible Psi Point counts. Execute only the row matching the
            // points that CostSpell will consume after effect execution.
            if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id == 19019u
                && spell.Caster is IPlayer { Class: Game.Static.Entity.Class.Esper } esper)
            {
                uint psiPoints = (uint)Math.Clamp(
                    (int)MathF.Floor(esper.GetVitalValue(Vital.Resource1)), 1, 5);
                if (info.Entry.OrderIndex != psiPoints - 1u)
                {
                    info.DropEffect = true;
                    return;
                }
            }

            // The normal Assassinate child contains mutually exclusive damage
            // rows for targets above and below 30% health. Apply exactly the
            // matching row; the surged child has a single unconditional row.
            if (spell.Parameters.SpellInfo.Entry.Id is 39324u or 39325u)
            {
                bool executeDamage = target.MaxHealth > 0u
                    && target.Health * 100u < target.MaxHealth * 30u;
                bool matchingRow = executeDamage
                    ? info.Entry.OrderIndex == 2u
                    : info.Entry.OrderIndex == 1u;
                if (!matchingRow)
                {
                    info.DropEffect = true;
                    return;
                }
            }

            void ApplyDamage(ISpellTargetEffectInfo tickInfo)
            {
                if (!target.CanAttack(spell.Caster))
                    return;

                // TODO: once spell effect handlers aren't static, this should be injected without the factory
                var factory = LegacyServiceProvider.Provider.GetService<IFactory<IDamageCalculator>>();
                var damageCalculator = factory.Resolve();
                damageCalculator.CalculateDamage(spell.Caster, target, spell, tickInfo);

                if (tickInfo.Damage == null)
                    return;

                target.TakeDamage(spell.Caster, tickInfo.Damage);

                if (tickInfo.Entry.ThreatMultiplier > 1f)
                {
                    float bonus = tickInfo.Damage.RawDamage
                        * (tickInfo.Entry.ThreatMultiplier - 1f);
                    target.ThreatManager.UpdateThreat(spell.Caster,
                        (int)Math.Clamp(bonus, 0f, int.MaxValue));
                }
                spell.RegisterSuccessfulHit();
            }

            SchedulePeriodicEffect(spell, target, info, ApplyDamage);

            // Menacing Strike is a left/right two-hit builder. Spell4 stores
            // one damage row in a repeated client phase, which game_rework
            // otherwise executes only once.
            if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id == 39339u
                && info.Entry.TickTime == 0u)
            {
                spell.ScheduleAction(0.25d, () =>
                {
                    var secondHit = new SpellTargetInfo.SpellTargetEffectInfo(
                        GlobalSpellManager.Instance.NextEffectId, info.Entry);
                    ApplyDamage(secondHit);
                    spell.SendEffectGo(target, secondHit);
                });
            }
        }

        [SpellEffectHandler(SpellEffectType.DistributedDamage)]
        public static void HandleEffectDistributedDamage(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectDamage(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.Heal)]
        public static void HandleEffectHeal(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // Regenerative Pulse contains mutually exclusive rows for allies
            // above/below 30% health. The generic target prerequisite path can
            // only evaluate player caster prerequisites, so select the row
            // from the actual target health here.
            if (spell.Parameters.SpellInfo.Entry.Id == 39646u)
            {
                bool lowHealth = target.MaxHealth > 0u
                    && target.Health * 100u < target.MaxHealth * 30u;
                bool matchingRow = lowHealth
                    ? info.Entry.OrderIndex == 1u
                    : info.Entry.OrderIndex == 0u;
                if (!matchingRow)
                {
                    info.DropEffect = true;
                    return;
                }
            }

            // Friendly healing uses the same table-driven scaling calculation
            // as damage, but is applied directly to health. Hostile units in a
            // mixed damage/healing telegraph (for example Dual Fire) must not
            // receive the healing half of the spell.
            if (target.CanAttack(spell.Caster) || !target.IsAlive)
            {
                info.DropEffect = true;
                return;
            }

            void ApplyHeal(ISpellTargetEffectInfo tickInfo)
            {
                var factory = LegacyServiceProvider.Provider.GetService<IFactory<IDamageCalculator>>();
                var damageCalculator = factory.Resolve();
                damageCalculator.CalculateDamage(spell.Caster, target, spell, tickInfo);

                if (tickInfo.Damage != null)
                    target.ModifyHealth(tickInfo.Damage.AdjustedDamage, DamageType.Heal, spell.Caster);
            }

            SchedulePeriodicEffect(spell, target, info, ApplyHeal);
        }

        [SpellEffectHandler(SpellEffectType.HealShields)]
        public static void HandleEffectHealShields(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target.CanAttack(spell.Caster) || !target.IsAlive)
            {
                info.DropEffect = true;
                return;
            }

            var factory = LegacyServiceProvider.Provider.GetService<IFactory<IDamageCalculator>>();
            var damageCalculator = factory.Resolve();
            damageCalculator.CalculateDamage(spell.Caster, target, spell, info);
            if (info.Damage != null)
                target.Shield = Math.Min(target.MaxShieldCapacity,
                    target.Shield + info.Damage.AdjustedDamage);
        }

        [SpellEffectHandler(SpellEffectType.Absorption)]
        public static void HandleEffectAbsorption(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target.CanAttack(spell.Caster) || !target.IsAlive)
            {
                info.DropEffect = true;
                return;
            }

            var factory = LegacyServiceProvider.Provider.GetService<IFactory<IDamageCalculator>>();
            var damageCalculator = factory.Resolve();
            damageCalculator.CalculateDamage(spell.Caster, target, spell, info);
            if (info.Damage == null)
                return;

            uint amount = info.Damage.AdjustedDamage;
            target.Absorption += amount;
            if (info.Entry.DurationTime > 0u)
                spell.ScheduleAction(info.Entry.DurationTime / 1000d,
                    () => target.Absorption = target.Absorption > amount
                        ? target.Absorption - amount
                        : 0u);
        }

        private static void SchedulePeriodicEffect(ISpell spell, IUnitEntity target,
            ISpellTargetEffectInfo initialInfo,
            Action<ISpellTargetEffectInfo> apply)
        {
            Spell4EffectsEntry entry = initialInfo.Entry;
            double initialDelay = entry.DelayTime / 1000d;

            if (initialDelay == 0d)
                apply(initialInfo);
            else
            {
                initialInfo.DropEffect = true;
                spell.ScheduleAction(initialDelay, () =>
                {
                    var delayedInfo = new SpellTargetInfo.SpellTargetEffectInfo(
                        GlobalSpellManager.Instance.NextEffectId, entry);
                    apply(delayedInfo);
                    spell.SendEffectGo(target, delayedInfo);
                });
            }

            if (entry.TickTime == 0u || entry.DurationTime <= entry.TickTime)
                return;

            uint tickCount = (entry.DurationTime + entry.TickTime - 1u) / entry.TickTime;
            double tickInterval = entry.TickTime / 1000d;
            for (uint tick = 1u; tick < tickCount; tick++)
            {
                spell.ScheduleAction(initialDelay + tick * tickInterval, () =>
                {
                    var tickInfo = new SpellTargetInfo.SpellTargetEffectInfo(
                        GlobalSpellManager.Instance.NextEffectId, entry);
                    apply(tickInfo);
                    spell.SendEffectGo(target, tickInfo);
                });
            }
        }

        [SpellEffectHandler(SpellEffectType.Resurrect)]
        public static void HandleEffectResurrect(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.ResurrectionManager.ResurrectRequest(spell.Caster.Guid);
        }

        [SpellEffectHandler(SpellEffectType.Proxy)]
        public static void HandleEffectProxy(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            uint parentSpellId = spell.Parameters.SpellInfo.Entry.Id;
            uint proxySpellId  = info.Entry.DataBits00;
            uint parentBaseId  = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;

            // Polarity Field persists for ten seconds. Its child aura has one
            // one-second pulse and relies on the missing field lifecycle to
            // repeat it, so recreate that lifecycle from the root spell.
            if (parentBaseId == 23328u && proxySpellId == 38975u)
            {
                for (uint tick = 0u; tick < 10u; tick++)
                    spell.CastProxySpell(proxySpellId, target, tick);
                return;
            }

            // Augmented Blade and Power Link are persistent toggles. Their
            // drain loops are owned by Spell so they can stop cleanly when KE
            // runs out or the player toggles them off.
            if (parentBaseId == 30896u
                && spell.Caster is IPlayer augmentedBladeWarrior)
            {
                if (!augmentedBladeWarrior.WarriorAugmentedBladeActive
                    && proxySpellId == 57357u)
                    spell.CastProxySpell(proxySpellId, target);
                return;
            }

            if (parentBaseId == 35146u
                && spell.Caster is IPlayer powerLinkWarrior)
            {
                if (powerLinkWarrior.WarriorPowerLinkActive && proxySpellId == 79790u)
                    spell.CastProxySpell(proxySpellId, target);
                else if (!powerLinkWarrior.WarriorPowerLinkActive && proxySpellId == 79789u)
                    spell.CastProxySpell(proxySpellId, target);
                return;
            }

            // These proxy rows store their payload in DataBits01. The known
            // persistent Warrior toggles are scheduled explicitly above; a
            // zero DataBits00 must never be interpreted as spell id zero.
            if (proxySpellId == 0u)
            {
                if (parentBaseId == 23169u && info.Entry.DataBits01 == 87477u)
                    for (uint tick = 1u; tick <= 10u; tick++)
                        spell.CastProxySpell(87477u, spell.Caster, tick * 0.5d);
                return;
            }

            // Quick Draw has three channel phases. The two mutually exclusive
            // UnderSpell rows alternate the pistols; without phase/persistent
            // effect support every row fires together. Recreate the table's
            // three 0.33-second impacts explicitly.
            if (parentBaseId is 27638u or 52215u)
            {
                if (info.Entry.OrderIndex != 0u)
                    return;

                uint firstImpact  = parentBaseId == 27638u ? 43498u : 76003u;
                uint secondImpact = parentBaseId == 27638u ? 43499u : 76004u;
                spell.CastProxySpell(firstImpact, target);
                spell.CastProxySpell(secondImpact, target, 0.33d);
                spell.CastProxySpell(firstImpact, target, 0.66d);
                return;
            }

            // Rapid Fire fires three impacts over roughly one second. Its tap
            // and channel phases are client-driven and are not advanced by the
            // old spell runtime, so reproduce the base three-shot sequence.
            if (parentSpellId is 35356u or 76834u)
            {
                if (info.Entry.OrderIndex != 0u)
                    return;

                uint impactSpellId = parentSpellId == 35356u ? 35360u : 76838u;
                spell.CastProxySpell(impactSpellId, target);
                spell.CastProxySpell(impactSpellId, target, 0.33d);
                spell.CastProxySpell(impactSpellId, target, 0.66d);
                return;
            }

            // Assassinate alternates its pistol visual and selects normal or
            // surged damage through UnderSpell prerequisites. Persistent buff
            // prerequisites are not available on game_rework, so make that
            // table choice explicitly and execute only one damage proxy.
            if (parentSpellId == 38905u)
            {
                if (info.Entry.OrderIndex != 0u)
                    return;

                bool surged = spell.Caster is IPlayer { SpellSurgeActive: true };
                spell.CastProxySpell(surged ? 76927u : 39324u, target);
                if (!surged)
                    spell.CastProxySpell(38907u, target);
                return;
            }

            // Spell Surge is a server-side toggle. Only apply its buff proxy
            // when it has just been activated; the deactivation cast removes
            // the state without immediately adding it again.
            if (parentBaseId == 31213u && proxySpellId == 47439u
                && spell.Caster is IPlayer spellslinger)
            {
                if (spellslinger.SpellSurgeActive)
                    spell.CastProxySpell(proxySpellId, target);
                return;
            }

            // Warrior builders target every foe with their resource proxy. Cast
            // it only once after at least one successful hit, not once per foe.
            if (proxySpellId is 53865u or 54587u or 79652u or 81699u
                && spell.Caster is IPlayer { Class: Game.Static.Entity.Class.Warrior })
            {
                if (spell.TryConsumeSuccessfulHit())
                    spell.CastProxySpell(proxySpellId, spell.Caster);
                return;
            }

            // Menacing Strike generates 150 KE with both its left and right
            // hit, once the damage portion connected with an enemy.
            if (parentSpellId == 61053u && proxySpellId == 53867u)
            {
                if (spell.TryConsumeSuccessfulHit())
                {
                    spell.CastProxySpell(proxySpellId, spell.Caster);
                    spell.CastProxySpell(proxySpellId, spell.Caster, 0.25d);
                }
                return;
            }

            // Discharge's Power Charge proxy targets the caster, so its table
            // target alone cannot tell whether the preceding damage tick hit.
            if (parentSpellId == 58832u && proxySpellId == 80382u)
            {
                spell.CastProxySpell(proxySpellId, target,
                    parentSpellSuccessfulHit: spell.TryConsumeSuccessfulHit());
                return;
            }

            if (parentSpellId == 42276u && proxySpellId == 37302u)
            {
                spell.CastProxySpell(proxySpellId, target);
                spell.CastProxySpell(42148u, spell.Caster);
                return;
            }

            // Telekinetic Strike can hit up to five targets, but generates one
            // Psi Point per successful cast rather than one point per target.
            if (parentSpellId == 32893u && proxySpellId == 30900u
                && spell.Caster is IPlayer { Class: Game.Static.Entity.Class.Esper })
            {
                if (spell.TryConsumeSuccessfulHit())
                    spell.CastProxySpell(proxySpellId, spell.Caster);
                return;
            }

            // Mode: Eradicate stores its periodic proxy in DataBits01 rather
            // than DataBits00. It grants 10 Volatility once per second for the
            // ten-second ExoSuit duration.
            if (parentSpellId == 47860u
                && proxySpellId == 0u
                && info.Entry.DataBits01 == 71371u
                && info.Entry.TickTime > 0u)
            {
                uint tickCount = info.Entry.DurationTime / info.Entry.TickTime;
                double tickDuration = info.Entry.TickTime / 1000d;
                for (uint tick = 1u; tick <= tickCount; tick++)
                    spell.CastProxySpell(info.Entry.DataBits01, spell.Caster, tick * tickDuration);
                return;
            }

            if (parentSpellId == 80382u)
            {
                if (proxySpellId == 80383u
                    && spell.Parameters.ParentSpellSuccessfulHit
                    && spell.Caster is Player medic)
                    medic.AddMedicPowerCharge();
                return;
            }

            // Stalker Shred has three sequential strikes, although its table
            // data contains only one proxy effect.
            if ((parentSpellId, proxySpellId) is (38765u, 38767u) or (38766u, 39467u))
            {
                spell.CastProxySpell(proxySpellId, target, 0d);
                spell.CastProxySpell(proxySpellId, target, 0.14d);
                spell.CastProxySpell(proxySpellId, target, 0.28d);
                return;
            }

            // Impale contains four mutually exclusive normal/stealth/behind
            // variants. Effect prerequisites are not evaluated by game_rework
            // yet, so execute only the normal damage variant for now.
            if (parentSpellId == 38779u)
            {
                if (proxySpellId == 39426u)
                    spell.CastProxySpell(proxySpellId, target);

                return;
            }

            // Stagger/Skull Crack's four damage proxies form the alternating
            // left-right strike sequence.
            if (parentSpellId == 38780u && proxySpellId == 38781u)
            {
                spell.CastProxySpell(proxySpellId, target, info.Entry.OrderIndex * 0.12d);
                return;
            }

            // Conditional proxies (rune sets, AMPs and tier upgrades) must not
            // execute unless their caster prerequisite is active. Previously
            // Mind Burst always triggered its optional rune-set damage proc.
            if (info.Entry.PrerequisiteIdCasterApply != 0u
                && spell.Caster is IPlayer player
                && !PrerequisiteManager.Instance.Meets(player, info.Entry.PrerequisiteIdCasterApply))
                return;

            spell.CastProxySpell(proxySpellId, target, info.Entry.DelayTime / 1000d);
        }

        [SpellEffectHandler(SpellEffectType.Disguise)]
        public static void HandleEffectDisguise(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            Creature2Entry creature2 = GameTableManager.Instance.Creature2.GetEntry(info.Entry.DataBits02);
            if (creature2 == null)
                return;

            Creature2DisplayGroupEntryEntry displayGroupEntry = GameTableManager.Instance.Creature2DisplayGroupEntry.Entries.FirstOrDefault(d => d.Creature2DisplayGroupId == creature2.Creature2DisplayGroupId);
            if (displayGroupEntry == null)
                return;

            target.DisplayInfo = displayGroupEntry.Creature2DisplayInfoId;
        }

        [SpellEffectHandler(SpellEffectType.SummonMount)]
        public static void HandleEffectSummonMount(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // TODO: handle NPC mounting?
            if (target is not IPlayer player)
                return;

            if (!player.CanMount())
                return;

            // TODO: needs to be replaced once spell effect handlers aren't static
            var factory = LegacyServiceProvider.Provider.GetService<IEntityFactory>();

            var mount = factory.CreateEntity<IMountEntity>();
            mount.Initialise(player, spell.Parameters.SpellInfo.Entry.Id, info.Entry.DataBits00, info.Entry.DataBits01, info.Entry.DataBits04);
            mount.EnqueuePassengerAdd(player, VehicleSeatType.Pilot, 0);

            // usually for hover boards
            /*if (info.Entry.DataBits04 > 0u)
            {
                mount.SetAppearance(new ItemVisual
                {
                    Slot      = ItemSlot.Mount,
                    DisplayId = (ushort)info.Entry.DataBits04
                });
            }*/

            var position = new MapPosition
            {
                Position = player.Position
            };

            if (player.Map.CanEnter(mount, position))
                player.Map.EnqueueAdd(mount, position);

            // FIXME: also cast 52539,Riding License - Riding Skill 1 - SWC - Tier 1,34464
            // FIXME: also cast 80530,Mount Sprint  - Tier 2,36122

            player.CastSpell(52539, new SpellParameters());
            player.CastSpell(80530, new SpellParameters());
        }

        [SpellEffectHandler(SpellEffectType.Teleport)]
        public static void HandleEffectTeleport(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            WorldLocation2Entry locationEntry = GameTableManager.Instance.WorldLocation2.GetEntry(info.Entry.DataBits00);
            if (locationEntry == null)
                return;

            if (target is IPlayer player)
                if (player.CanTeleport())
                    player.TeleportTo((ushort)locationEntry.WorldId, locationEntry.Position0, locationEntry.Position1, locationEntry.Position2);
        }

        [SpellEffectHandler(SpellEffectType.FullScreenEffect)]
        public static void HandleFullScreenEffect(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // TODO/FIXME: Add duration into the queue so that the spell will automatically finish at the correct time. This is a workaround for Full Screen Effects.
            //events.EnqueueEvent(new Event.SpellEvent(info.Entry.DurationTime / 1000d, () => { status = SpellStatus.Finished; SendSpellFinish(); }));
        }

        [SpellEffectHandler(SpellEffectType.RapidTransport)]
        public static void HandleEffectRapidTransport(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            TaxiNodeEntry taxiNode = GameTableManager.Instance.TaxiNode.GetEntry(spell.Parameters.TaxiNode);
            if (taxiNode == null)
                return;

            WorldLocation2Entry worldLocation = GameTableManager.Instance.WorldLocation2.GetEntry(taxiNode.WorldLocation2Id);
            if (worldLocation == null)
                return;

            if (target is not IPlayer player)
                return;

            if (!player.CanTeleport())
                return;

            var rotation = new Quaternion(worldLocation.Facing0, worldLocation.Facing0, worldLocation.Facing2, worldLocation.Facing3);
            player.Rotation = rotation.ToEuler();
            player.TeleportTo((ushort)worldLocation.WorldId, worldLocation.Position0, worldLocation.Position1, worldLocation.Position2);
        }

        [SpellEffectHandler(SpellEffectType.LearnDyeColor)]
        public static void HandleEffectLearnDyeColor(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.Account.GenericUnlockManager.Unlock((ushort)info.Entry.DataBits00);
        }

        [SpellEffectHandler(SpellEffectType.UnlockMount)]
        public static void HandleEffectUnlockMount(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(info.Entry.DataBits00);
            player.SpellManager.AddSpell(spell4Entry.Spell4BaseIdBaseSpell);

            player.Session.EnqueueMessageEncrypted(new ServerUnlockMount
            {
                Spell4Id = info.Entry.DataBits00
            });
        }

        [SpellEffectHandler(SpellEffectType.UnlockPetFlair)]
        public static void HandleEffectUnlockPetFlair(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.PetCustomisationManager.UnlockFlair((ushort)info.Entry.DataBits00);
        }

        [SpellEffectHandler(SpellEffectType.UnlockVanityPet)]
        public static void HandleEffectUnlockVanityPet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(info.Entry.DataBits00);
            player.SpellManager.AddSpell(spell4Entry.Spell4BaseIdBaseSpell);

            player.Session.EnqueueMessageEncrypted(new ServerUnlockMount
            {
                Spell4Id = info.Entry.DataBits00
            });
        }

        [SpellEffectHandler(SpellEffectType.SummonVanityPet)]
        public static void HandleEffectSummonVanityPet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            // enqueue removal of existing vanity pet if summoned
            if (player.VanityPetGuid != null)
            {
                IPetEntity oldVanityPet = player.GetVisible<IPetEntity>(player.VanityPetGuid.Value);
                oldVanityPet?.RemoveFromMap();
                player.VanityPetGuid = 0u;
            }

            // TODO: needs to be replaced once spell effect handlers aren't static
            var factory = LegacyServiceProvider.Provider.GetService<IEntityFactory>();

            var pet = factory.CreateEntity<IPetEntity>();
            pet.Initialise(player, info.Entry.DataBits00);

            var position = new MapPosition
            {
                Position = player.Position
            };

            if (player.Map.CanEnter(pet, position))
                player.Map.EnqueueAdd(pet, position);
        }

        [SpellEffectHandler(SpellEffectType.TitleGrant)]
        public static void HandleEffectTitleGrant(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.TitleManager.AddTitle((ushort)info.Entry.DataBits00);
        }

        [SpellEffectHandler(SpellEffectType.Fluff)]
        public static void HandleEffectFluff(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
        }

        [SpellEffectHandler(SpellEffectType.UnitPropertyModifier)]
        public static void HandleEffectPropertyModifier(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
            if (baseId == 30896u
                && spell.Caster is IPlayer augmentedBladeWarrior
                && !augmentedBladeWarrior.WarriorAugmentedBladeActive)
            {
                target.RemoveSpellProperty((Property)info.Entry.DataBits00,
                    spell.Parameters.SpellInfo.Entry.Id);
                info.DropEffect = true;
                return;
            }

            if (info.Entry.PrerequisiteIdCasterApply != 0u
                && spell.Caster is IPlayer prerequisitePlayer
                && baseId != 30896u
                && !PrerequisiteManager.Instance.Meets(prerequisitePlayer,
                    info.Entry.PrerequisiteIdCasterApply))
            {
                info.DropEffect = true;
                return;
            }

            // TODO: I suppose these could be cached somewhere instead of generating them every single effect?
            SpellPropertyModifier modifier = 
                new SpellPropertyModifier((Property)info.Entry.DataBits00, 
                    info.Entry.DataBits01, 
                    BitConverter.UInt32BitsToSingle(info.Entry.DataBits02), 
                    BitConverter.UInt32BitsToSingle(info.Entry.DataBits03), 
                    BitConverter.UInt32BitsToSingle(info.Entry.DataBits04));
            target.AddSpellModifierProperty(modifier, spell.Parameters.SpellInfo.Entry.Id);

            uint duration = info.Entry.DurationTime;
            if (baseId == 35526u)
                duration = 10000u;
            else if (baseId == 23345u)
                duration = 1100u;
            else if (baseId == 55436u)
                duration = 500u;

            if (duration > 0u)
                spell.ScheduleAction(duration / 1000d,
                    () => target.RemoveSpellProperty((Property)info.Entry.DataBits00,
                        spell.Parameters.SpellInfo.Entry.Id));

            // Gather Focus restores Focus immediately and grants the documented
            // six one-second Spell Power pulses in addition to its table-driven
            // temporary Focus recovery modifier.
            if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id == 23664u
                && target is IPlayer { Class: Game.Static.Entity.Class.Spellslinger })
            {
                target.ModifyVital(Vital.Focus, 60f);
                for (uint tick = 1u; tick <= 6u; tick++)
                    spell.ScheduleAction(tick, () => target.ModifyVital(Vital.Resource4, 3f));
            }
        }
    }
}
