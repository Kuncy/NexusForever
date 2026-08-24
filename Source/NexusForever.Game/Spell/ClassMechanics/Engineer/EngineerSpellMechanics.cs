namespace NexusForever.Game.Spell.ClassMechanics.Engineer;

public static class EngineerSpellMechanics
{
    public static bool IsServerExecutedChannel(Spell spell)
        => spell.Parameters.SpellInfo.Entry.Id == 41276u;

    public static bool ShouldFinishRoot(Spell spell)
        => spell.Parameters.RootSpellInfo.Entry.Id == EngineerSpellIds.PulseBlast;
}
