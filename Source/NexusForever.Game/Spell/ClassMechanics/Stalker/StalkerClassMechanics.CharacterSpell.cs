using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell.ClassMechanics.Stalker;

public sealed partial class StalkerClassMechanics
{
    public bool CanBeginCast(IPlayer owner, ICharacterSpell characterSpell)
    {
        if (characterSpell.BaseInfo.Entry.Id != StalkerSpellIds.FalseRetreatBase)
            return true;

        StalkerState state = StalkerState.For(owner);
        if (!state.FalseRetreatActive)
            return true;

        owner.MovementManager.SetPosition(state.FalseRetreatPosition.Value, false);
        state.EndFalseRetreat();
        owner.CastSpell(StalkerSpellIds.FalseRetreatReturn, new SpellParameters());
        return false;
    }

    public ISpellInfo SelectSpell(IPlayer owner, ICharacterSpell characterSpell,
        ISpellInfo current)
    {
        StalkerState state = StalkerState.For(owner);
        if (characterSpell.BaseInfo.Entry.Id == StalkerSpellIds.ShredBase
            && state.StealthActive)
            return GetSpellInfo(StalkerSpellIds.ShredStealthBase, characterSpell.Tier);

        if (characterSpell.BaseInfo.Entry.Id == StalkerSpellIds.NanoFieldBase
            && state.NanoFieldActive)
            return GetSpellInfo(StalkerSpellIds.NanoFieldEndBase, characterSpell.Tier);

        return current;
    }

    private static ISpellInfo GetSpellInfo(uint baseId, byte tier)
        => GlobalSpellManager.Instance.GetSpellBaseInfo(baseId).GetSpellInfo(tier);
}
