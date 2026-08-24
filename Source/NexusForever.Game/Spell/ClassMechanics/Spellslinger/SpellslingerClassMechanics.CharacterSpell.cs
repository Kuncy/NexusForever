using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell.ClassMechanics.Spellslinger;

public sealed partial class SpellslingerClassMechanics
{
    public bool CanBeginCast(IPlayer owner, ICharacterSpell characterSpell)
    {
        if (characterSpell.BaseInfo.Entry.Id != SpellslingerSpellIds.SpellSurge)
            return true;

        if (owner.SpellManager.GetSpellCooldown(characterSpell.SpellInfo.Entry.Id) > 0d)
            return false;

        SpellslingerState state = SpellslingerState.For(owner);
        if (!state.SpellSurgeActive && owner.GetVitalValue(Vital.SpellSurge) < 25f)
            return false;

        state.SetSpellSurgeActive(owner, !state.SpellSurgeActive);
        return true;
    }

    public ISpellInfo SelectSpell(IPlayer owner, ICharacterSpell characterSpell, ISpellInfo current)
    {
        if (characterSpell.BaseInfo.Entry.Id == SpellslingerSpellIds.TrueShot)
            return SelectTrueShot(owner);

        SpellslingerState state = SpellslingerState.For(owner);
        if (!state.SpellSurgeActive
            || characterSpell.BaseInfo.Entry.Id == SpellslingerSpellIds.SpellSurge
            || current.Entry.Spell4IdMechanicAlternateSpell == 0u)
            return current;

        if (owner.GetVitalValue(Vital.SpellSurge) < 25f)
        {
            state.SetSpellSurgeActive(owner, false);
            return current;
        }

        return GetSpellInfo(current.Entry.Spell4IdMechanicAlternateSpell);
    }

    private static ISpellInfo SelectTrueShot(IPlayer owner)
    {
        SpellslingerState state = SpellslingerState.For(owner);
        if (!state.IsPending(state.TrueShotTapExpiresAt))
            state.TrueShotTap = 0;

        bool surged = state.SpellSurgeActive && owner.GetVitalValue(Vital.SpellSurge) >= 25f;
        if (state.SpellSurgeActive && !surged)
            state.SetSpellSurgeActive(owner, false);

        uint[] sequence = surged
            ? [36085u, 36054u, 36089u]
            : [36052u, 36053u, 36055u];
        uint spell4Id = sequence[state.TrueShotTap];
        state.TrueShotTap = (byte)((state.TrueShotTap + 1) % sequence.Length);
        state.TrueShotTapExpiresAt = state.DeadlineIn(4d);
        return GetSpellInfo(spell4Id);
    }

    private static ISpellInfo GetSpellInfo(uint spell4Id)
    {
        Spell4Entry entry = GameTableManager.Instance.Spell4.GetEntry(spell4Id);
        return GlobalSpellManager.Instance.GetSpellBaseInfo(entry.Spell4BaseIdBaseSpell)
            .GetSpellInfo((byte)entry.TierIndex);
    }
}
