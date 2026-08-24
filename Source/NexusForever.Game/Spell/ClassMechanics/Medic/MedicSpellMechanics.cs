namespace NexusForever.Game.Spell.ClassMechanics.Medic;

public static class MedicSpellMechanics
{
    public static bool ShouldFinishRoot(Spell spell)
        => spell.Parameters.RootSpellInfo.Entry.Id == MedicSpellIds.Discharge;
}
