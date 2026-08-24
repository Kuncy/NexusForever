namespace NexusForever.Script.Template.AI
{
    /// <summary>
    /// One spell in a creature combat profile and its server-side timing.
    /// </summary>
    public sealed record CombatAbility(
        uint Spell4Id,
        double CooldownSeconds,
        double InitialDelaySeconds);
}
