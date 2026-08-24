using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Warrior;

public sealed partial class WarriorClassMechanics
{
    public bool CanBeginCast(IPlayer owner, ICharacterSpell characterSpell)
    {
        uint baseId = characterSpell.BaseInfo.Entry.Id;
        if (baseId is WarriorSpellIds.RelentlessStrikes or WarriorSpellIds.Rampage
            && owner.SpellManager.GetSpellCooldown(characterSpell.SpellInfo.Entry.Id) > 0d)
            return false;

        WarriorState state = WarriorState.For(owner);
        if (baseId == WarriorSpellIds.AugmentedBlade)
        {
            if (!state.AugmentedBladeActive && owner.GetVitalValue(Vital.KineticCell) < 250f)
                return false;
            state.AugmentedBladeActive = !state.AugmentedBladeActive;
        }
        else if (baseId == WarriorSpellIds.PowerLink)
        {
            if (!state.PowerLinkActive && owner.GetVitalValue(Vital.KineticCell) < 250f)
                return false;
            state.PowerLinkActive = !state.PowerLinkActive;
        }

        return true;
    }

    public ISpellInfo SelectSpell(IPlayer owner, ICharacterSpell characterSpell, ISpellInfo current)
    {
        return characterSpell.BaseInfo.Entry.Id switch
        {
            WarriorSpellIds.RelentlessStrikes => SelectRelentless(owner, characterSpell),
            WarriorSpellIds.Rampage           => SelectRampage(owner, characterSpell),
            _                                 => current
        };
    }

    private static ISpellInfo SelectRelentless(IPlayer owner, ICharacterSpell characterSpell)
    {
        WarriorState state = WarriorState.For(owner);
        long now = Environment.TickCount64;
        if (now > state.RelentlessExpiresAt)
            state.RelentlessStage = 0;

        uint[] baseIds = characterSpell.Tier >= 4
            ? [WarriorSpellIds.RelentlessStrikes, WarriorSpellIds.RelentlessStrikesStage2,
                WarriorSpellIds.RelentlessStrikesStage3, WarriorSpellIds.RelentlessStrikesStage4]
            : [WarriorSpellIds.RelentlessStrikes, WarriorSpellIds.RelentlessStrikesStage2,
                WarriorSpellIds.RelentlessStrikesStage3];

        uint baseId = baseIds[state.RelentlessStage];
        state.RelentlessStage = (byte)((state.RelentlessStage + 1) % baseIds.Length);
        state.RelentlessExpiresAt = now + 2500L;
        return GlobalSpellManager.Instance.GetSpellBaseInfo(baseId).GetSpellInfo(characterSpell.Tier);
    }

    private static ISpellInfo SelectRampage(IPlayer owner, ICharacterSpell characterSpell)
    {
        WarriorState state = WarriorState.For(owner);
        long now = Environment.TickCount64;
        if (now > state.RampageExpiresAt || owner.GetVitalValue(Vital.KineticCell) < 250f)
            state.RampageStage = 0;

        uint[] baseIds = [WarriorSpellIds.Rampage, WarriorSpellIds.RampageStage2,
            WarriorSpellIds.RampageStage3, WarriorSpellIds.RampageStage4];
        uint baseId = baseIds[state.RampageStage];
        state.RampageStage = (byte)((state.RampageStage + 1) % baseIds.Length);
        state.RampageExpiresAt = now + 2500L;
        return GlobalSpellManager.Instance.GetSpellBaseInfo(baseId).GetSpellInfo(characterSpell.Tier);
    }
}
