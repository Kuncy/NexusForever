using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game;

namespace NexusForever.Game.Spell
{
    public class CharacterSpell : ICharacterSpell
    {
        [Flags]
        public enum UnlockedSpellSaveMask
        {
            None   = 0x0000,
            Create = 0x0001,
            Tier   = 0x0002
        }

        public IPlayer Owner { get; }
        public ISpellBaseInfo BaseInfo { get; }
        public ISpellInfo SpellInfo { get; private set; }
        public IItem Item { get; }

        public byte Tier
        {
            get => tier;
            set
            {
                if (tier != value)
                    SpellInfo = BaseInfo.GetSpellInfo(tier);

                tier = value;
                saveMask |= UnlockedSpellSaveMask.Tier;
            }
        }
        private byte tier;

        public uint AbilityCharges { get; private set; }
        public uint MaxAbilityCharges => SpellInfo.Entry.AbilityChargeCount;

        private UnlockedSpellSaveMask saveMask;

        private UpdateTimer rechargeTimer;
        private ISpell chargingSpell;
        private long chargeStartedAt;

        /// <summary>
        /// Create a new <see cref="ICharacterSpell"/> from an existing database model.
        /// </summary>
        public CharacterSpell(IPlayer player, CharacterSpellModel model, ISpellBaseInfo baseInfo, IItem item)
        {
            Owner     = player;
            BaseInfo  = baseInfo;
            SpellInfo = baseInfo.GetSpellInfo(tier);
            Item      = item;
            tier      = model.Tier;

            InitialiseAbilityCharges();
        }

        /// <summary>
        /// Create a new <see cref="ICharacterSpell"/> from a <see cref="ISpellBaseInfo"/>.
        /// </summary>
        public CharacterSpell(IPlayer player, ISpellBaseInfo baseInfo, byte tier, IItem item)
        {
            Owner     = player;
            BaseInfo  = baseInfo ?? throw new ArgumentNullException();
            SpellInfo = baseInfo.GetSpellInfo(tier);
            Item      = item;
            this.tier = tier;

            InitialiseAbilityCharges();

            saveMask = UnlockedSpellSaveMask.Create;
        }

        private void InitialiseAbilityCharges()
        {
            if (MaxAbilityCharges == 0u)
                return;

            rechargeTimer  = new UpdateTimer(SpellInfo.Entry.AbilityRechargeTime / 1000d);
            AbilityCharges = MaxAbilityCharges;
            SendChargeUpdate();
        }

        public void Update(double lastTick)
        {
            if (MaxAbilityCharges > 0 && AbilityCharges < MaxAbilityCharges)
            {
                rechargeTimer.Update(lastTick);
                if (rechargeTimer.HasElapsed)
                {
                    AbilityCharges = Math.Clamp(AbilityCharges + SpellInfo.Entry.AbilityRechargeCount, 0u, MaxAbilityCharges);
                    SendChargeUpdate();
                    rechargeTimer.Reset();
                }
            }
        }

        public void Save(CharacterContext context)
        {
            if (saveMask == UnlockedSpellSaveMask.None)
                return;

            if ((saveMask & UnlockedSpellSaveMask.Create) != 0)
            {
                var model = new CharacterSpellModel
                {
                    Id           = Owner.CharacterId,
                    Spell4BaseId = BaseInfo.Entry.Id,
                    Tier         = tier
                };

                context.Add(model);
            }
            else
            {
                var model = new CharacterSpellModel
                {
                    Id           = Owner.CharacterId,
                    Spell4BaseId = BaseInfo.Entry.Id,
                };

                EntityEntry<CharacterSpellModel> entity = context.Attach(model);
                if ((saveMask & UnlockedSpellSaveMask.Tier) != 0)
                {
                    model.Tier = tier;
                    entity.Property(p => p.Tier).IsModified = true;
                }
            }

            saveMask = UnlockedSpellSaveMask.None;
        }

        /// <summary>
        /// Used for when the client does not have continuous casting enabled
        /// </summary>
        public void Cast()
        {
            CastSpell();
        }

        /// <summary>
        /// Used for continuous casting when the client has it enabled, or spells with Cast Methods like ChargeRelease
        /// </summary>
        public void Cast(bool buttonPressed)
        {
            if (BaseInfo.Entry.CastMethod == 7u)
            {
                HandleChargeRelease(buttonPressed);
                return;
            }

            // If the player depresses button after the spell had exceeded its threshold, don't try and recast the spell until button is pressed down again.
            if (!buttonPressed)
                return;

            CastSpell();
        }

        private void CastSpell()
        {
            if (Owner.Class == Class.Spellslinger && BaseInfo.Entry.Id == 31213u)
            {
                if (Owner.SpellManager.GetSpellCooldown(SpellInfo.Entry.Id) > 0d)
                    return;

                if (!Owner.SpellSurgeActive && Owner.GetVitalValue(Vital.SpellSurge) < 25f)
                    return;

                Owner.SetSpellSurgeActive(!Owner.SpellSurgeActive);
            }

            CastSpell(GetSpellInfoForCast());
        }

        private void CastSpell(ISpellInfo spellInfo)
        {
            Owner.CastSpell(new SpellParameters
            {
                CharacterSpell         = this,
                SpellInfo              = spellInfo,
                UserInitiatedSpellCast = true
            });
        }

        private ISpellInfo GetSpellInfoForCast()
        {
            if (Owner.Class != Class.Spellslinger
                || !Owner.SpellSurgeActive
                || BaseInfo.Entry.Id == 31213u
                || SpellInfo.Entry.Spell4IdMechanicAlternateSpell == 0u)
                return SpellInfo;

            if (Owner.GetVitalValue(Vital.SpellSurge) < 25f)
            {
                Owner.SetSpellSurgeActive(false);
                return SpellInfo;
            }

            Spell4Entry alternateEntry = GameTableManager.Instance.Spell4.GetEntry(SpellInfo.Entry.Spell4IdMechanicAlternateSpell);
            return GlobalSpellManager.Instance
                .GetSpellBaseInfo(alternateEntry.Spell4BaseIdBaseSpell)
                .GetSpellInfo((byte)alternateEntry.TierIndex);
        }

        private void HandleChargeRelease(bool buttonPressed)
        {
            if (buttonPressed)
            {
                if (chargingSpell is { IsFinished: false })
                    return;

                ISpellInfo spellInfo = GetSpellInfoForCast();
                CastSpell(spellInfo);

                chargingSpell = Owner.GetActiveSpell(s => s.Parameters.CharacterSpell == this
                    && s.Parameters.SpellInfo == spellInfo
                    && s.IsCasting);
                if (chargingSpell != null)
                    chargeStartedAt = Environment.TickCount64;
                return;
            }

            if (chargingSpell is not { IsCasting: true })
                return;

            long elapsed = Environment.TickCount64 - chargeStartedAt;
            Spell4ThresholdsEntry threshold = GameTableManager.Instance.Spell4Thresholds.Entries
                .Where(t => t.Spell4IdParent == chargingSpell.Parameters.SpellInfo.Entry.Id
                    && t.ThresholdDuration <= elapsed)
                .OrderBy(t => t.ThresholdDuration)
                .LastOrDefault();
            if (threshold == null)
                return;

            Spell4ThresholdsEntry resourceCost = GameTableManager.Instance.Spell4Thresholds.Entries
                .FirstOrDefault(t => t.Spell4IdParent == chargingSpell.Parameters.SpellInfo.Entry.Id
                    && t.VitalEnumCostType00 != 0u
                    && t.VitalCostValue00 != 0u);
            if (resourceCost != null)
                Owner.ModifyVital((Vital)resourceCost.VitalEnumCostType00, -resourceCost.VitalCostValue00);

            double cooldown = threshold.OrderIndex switch
            {
                0u => 2d,
                1u => 5d,
                _  => 10d
            };
            chargingSpell.ReleaseCharge(threshold.Spell4IdToCast, cooldown);
            chargingSpell = null;
        }

        public void UseCharge()
        {
            if (AbilityCharges == 0)
                throw new SpellException("No charges available.");

            AbilityCharges -= 1;
            SendChargeUpdate();
        }

        private void SendChargeUpdate()
        {
            Owner.Session.EnqueueMessageEncrypted(new ServerSpellAbilityCharges
            {
                SpellId            = Item.Id,
                AbilityChargeCount = AbilityCharges
            });
        }
    }
}
