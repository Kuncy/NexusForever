namespace NexusForever.Script.Template.AI
{
    /// <summary>
    /// Describes the auto attacks, preferred combat range and special abilities
    /// used by one or more creature templates.
    /// </summary>
    public sealed record CombatProfile(
        IReadOnlyList<uint> AutoAttacks,
        float PreferredRange,
        IReadOnlyList<CombatAbility> Abilities);
}
