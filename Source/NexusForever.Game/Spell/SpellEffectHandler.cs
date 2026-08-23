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

            var questObjectives = spell.Parameters.QuestEntityActivation
                ? player.QuestManager.GetActiveQuests()
                    .Where(q => q.State == Game.Static.Quest.QuestState.Accepted)
                    .SelectMany(q => q)
                    .Where(o => !o.IsComplete()
                        && o.ObjectiveInfo.Entry.Data == target.CreatureId
                        && o.ObjectiveInfo.Type is Game.Static.Quest.QuestObjectiveType.ActivateEntity
                            or Game.Static.Quest.QuestObjectiveType.ActivateEntity2
                            or Game.Static.Quest.QuestObjectiveType.SucceedCSI)
                    .Select(o => (Objective: o, Progress: o.Progress))
                    .ToArray()
                : [];

            player.QuestManager.ObjectiveUpdate(Game.Static.Quest.QuestObjectiveType.ActivateEntity, target.CreatureId, 1u);
            player.QuestManager.ObjectiveUpdate(Game.Static.Quest.QuestObjectiveType.ActivateEntity2, target.CreatureId, 1u);
            player.QuestManager.ObjectiveUpdate(Game.Static.Quest.QuestObjectiveType.SucceedCSI, target.CreatureId, 1u);
            foreach (uint targetGroupId in AssetManager.Instance.GetTargetGroupsForCreatureId(target.CreatureId) ?? Enumerable.Empty<uint>())
                player.QuestManager.ObjectiveUpdate(Game.Static.Quest.QuestObjectiveType.ActivateTargetGroup, targetGroupId, 1u);

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
            if (info.Entry.PrerequisiteIdCasterApply != 0u
                && spell.Caster is IPlayer player
                && !PrerequisiteManager.Instance.Meets(player, info.Entry.PrerequisiteIdCasterApply))
                return;

            int amount = unchecked((int)info.Entry.DataBits01);
            if (amount == 0)
                return;

            target.ModifyVital((Vital)info.Entry.DataBits00, amount);
        }

        [SpellEffectHandler(SpellEffectType.Damage)]
        public static void HandleEffectDamage(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!target.CanAttack(spell.Caster))
                return;

            // TODO: once spell effect handlers aren't static, this should be injected without the factory
            var factory = LegacyServiceProvider.Provider.GetService<IFactory<IDamageCalculator>>();
            var damageCalculator = factory.Resolve();
            damageCalculator.CalculateDamage(spell.Caster, target, spell, info);

            target.TakeDamage(spell.Caster, info.Damage);
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

            if (parentSpellId == 42276u && proxySpellId == 37302u)
            {
                spell.CastProxySpell(proxySpellId, target);
                spell.CastProxySpell(42148u, spell.Caster);
                return;
            }

            if (parentSpellId == 80382u)
            {
                if (proxySpellId == 80383u
                    && spell.TryRegisterTrigger(proxySpellId)
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

            spell.CastProxySpell(proxySpellId, target);
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
            // TODO: I suppose these could be cached somewhere instead of generating them every single effect?
            SpellPropertyModifier modifier = 
                new SpellPropertyModifier((Property)info.Entry.DataBits00, 
                    info.Entry.DataBits01, 
                    BitConverter.UInt32BitsToSingle(info.Entry.DataBits02), 
                    BitConverter.UInt32BitsToSingle(info.Entry.DataBits03), 
                    BitConverter.UInt32BitsToSingle(info.Entry.DataBits04));
            target.AddSpellModifierProperty(modifier, spell.Parameters.SpellInfo.Entry.Id);

            // TODO: Handle removing spell modifiers

            //if (info.Entry.DurationTime > 0d)
            //    events.EnqueueEvent(new SpellEvent(info.Entry.DurationTime / 1000d, () =>
            //    {
            //        player.RemoveSpellProperty((Property)info.Entry.DataBits00, parameters.SpellInfo.Entry.Id);
            //    }));
        }
    }
}
