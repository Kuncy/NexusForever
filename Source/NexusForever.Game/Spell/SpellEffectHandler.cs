using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Combat;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Spell.ClassMechanics;
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
            if (ClassEffectMechanics.TryHandleVitalModifier(spell, target, info))
                return;

            if (info.Entry.PrerequisiteIdCasterApply != 0u
                && spell.Caster is IPlayer player
                && !PrerequisiteManager.Instance.Meets(player, info.Entry.PrerequisiteIdCasterApply))
                return;

            int amount = unchecked((int)info.Entry.DataBits01);
            if (amount == 0)
                return;

            target.ModifyVital((Vital)info.Entry.DataBits00, amount);
        }

        [SpellEffectHandler(SpellEffectType.ForcedMove)]
        public static void HandleEffectForcedMove(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            ClassEffectMechanics.HandleForcedMove(spell, target, info);
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

            ClassEffectMechanics.AfterCcStateApplied(spell, target, info);
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
            if (ClassEffectMechanics.TryHandleProc(spell, target, info))
                return;

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
            if (!ClassEffectMechanics.ShouldApplyDamage(spell, target, info))
                return;

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
                ClassCombatMechanics.OnDamageResolved(spell.Caster, target);

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
            uint repeatCount = ClassEffectMechanics.GetAdditionalDamageRepeatCount(spell);
            if (repeatCount > 0u && info.Entry.TickTime == 0u)
            {
                double interval = ClassEffectMechanics.GetAdditionalDamageRepeatInterval(spell);
                for (uint repeat = 1u; repeat <= repeatCount; repeat++)
                {
                    spell.ScheduleAction(repeat * interval, () =>
                    {
                        var repeatedHit = new SpellTargetInfo.SpellTargetEffectInfo(
                            GlobalSpellManager.Instance.NextEffectId, info.Entry);
                        ApplyDamage(repeatedHit);
                        spell.SendEffectGo(target, repeatedHit);
                    });
                }
            }
        }

        [SpellEffectHandler(SpellEffectType.DistributedDamage)]
        public static void HandleEffectDistributedDamage(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectDamage(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.Transference)]
        public static void HandleEffectTransference(ISpell spell, IUnitEntity target,
            ISpellTargetEffectInfo info)
        {
            if (!target.CanAttack(spell.Caster))
            {
                info.DropEffect = true;
                return;
            }

            void ApplyTransfer(ISpellTargetEffectInfo tickInfo)
            {
                var factory = LegacyServiceProvider.Provider.GetService<IFactory<IDamageCalculator>>();
                var damageCalculator = factory.Resolve();
                damageCalculator.CalculateDamage(spell.Caster, target, spell, tickInfo);
                if (tickInfo.Damage == null)
                    return;

                target.TakeDamage(spell.Caster, tickInfo.Damage);
                ClassCombatMechanics.OnDamageResolved(spell.Caster, target);

                float transferPercent = BitConverter.UInt32BitsToSingle(
                    tickInfo.Entry.DataBits05);
                uint healing = (uint)MathF.Max(0f,
                    tickInfo.Damage.AdjustedDamage * transferPercent);
                if (healing > 0u)
                    spell.Caster.ModifyHealth(healing, DamageType.Heal, spell.Caster);

                if (tickInfo.Entry.ThreatMultiplier > 1f)
                {
                    float bonus = tickInfo.Damage.RawDamage
                        * (tickInfo.Entry.ThreatMultiplier - 1f);
                    target.ThreatManager.UpdateThreat(spell.Caster,
                        (int)Math.Clamp(bonus, 0f, int.MaxValue));
                }
                spell.RegisterSuccessfulHit();
            }

            SchedulePeriodicEffect(spell, target, info, ApplyTransfer);
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
            if (ClassEffectMechanics.TryHandleProxy(spell, target, info))
                return;

            uint proxySpellId = info.Entry.DataBits00;

            // Conditional proxies (rune sets, AMPs and tier upgrades) must not
            // execute unless their caster prerequisite is active. Previously
            // Mind Burst always triggered its optional rune-set damage proc.
            if (info.Entry.PrerequisiteIdCasterApply != 0u
                && spell.Caster is IPlayer player
                && !PrerequisiteManager.Instance.Meets(player, info.Entry.PrerequisiteIdCasterApply))
                return;

            if (proxySpellId == 0u)
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
            if (!ClassEffectMechanics.ShouldApplyProperty(spell, target, info))
                return;

            if (info.Entry.PrerequisiteIdCasterApply != 0u
                && spell.Caster is IPlayer prerequisitePlayer
                && ClassEffectMechanics.ShouldEvaluatePropertyPrerequisite(spell)
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

            uint duration = ClassEffectMechanics.GetPropertyDuration(spell,
                info.Entry.DurationTime);

            if (duration > 0u)
                spell.ScheduleAction(duration / 1000d,
                    () => target.RemoveSpellProperty((Property)info.Entry.DataBits00,
                        spell.Parameters.SpellInfo.Entry.Id));

            ClassEffectMechanics.AfterPropertyApplied(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.SummonTrap)]
        public static void HandleEffectSummonTrap(ISpell spell, IUnitEntity target,
            ISpellTargetEffectInfo info)
        {
            if (spell.Caster is not IPlayer player)
                return;

            var factory = LegacyServiceProvider.Provider.GetService<IEntityFactory>();
            ITrapEntity trap = factory.CreateEntity<ITrapEntity>();
            float triggerRadius = BitConverter.UInt32BitsToSingle(info.Entry.DataBits04);
            if (triggerRadius <= 0f)
                triggerRadius = 3f;
            trap.Initialise(player, info.Entry.DataBits00, info.Entry.DataBits01,
                info.Entry.DurationTime, triggerRadius);

            Vector3 position = spell.Parameters.Position?.Vector ?? target.Position;
            var mapPosition = new MapPosition { Position = position };
            if (player.Map.CanEnter(trap, mapPosition))
                player.Map.EnqueueAdd(trap, mapPosition);
        }
    }
}
