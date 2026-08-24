using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell.ClassMechanics.Stalker;

public static class StalkerCharacterSpellMechanics
{
    public static bool CanBeginCast(IPlayer owner, ICharacterSpell characterSpell)
    {
        if (owner.Class != Class.Stalker
            || characterSpell.BaseInfo.Entry.Id != StalkerSpellIds.FalseRetreatBase)
            return true;

        StalkerState state = StalkerState.For(owner);
        if (!state.FalseRetreatActive)
            return true;

        owner.MovementManager.SetPosition(state.FalseRetreatPosition.Value, false);
        state.EndFalseRetreat();
        owner.CastSpell(StalkerSpellIds.FalseRetreatReturn, new SpellParameters());
        return false;
    }

    public static ISpellInfo SelectSpell(IPlayer owner, ICharacterSpell characterSpell,
        ISpellInfo current)
    {
        if (owner.Class != Class.Stalker)
            return current;

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
