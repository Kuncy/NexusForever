namespace NexusForever.Game.Abstract.Spell
{
    /// <summary>
    /// Identity of an <see cref="IAura"/> on a single target.
    /// </summary>
    /// <param name="Spell4Id">Spell that applied the aura.</param>
    /// <param name="EffectEntryId">
    /// Spell4Effects row that applied the aura, so that a spell applying several effects gets an aura per effect.
    /// Zero for an aura that isn't driven by an effect row.
    /// </param>
    /// <param name="CasterGuid">Guid of the caster, so that two casters don't overwrite each other.</param>
    public readonly record struct AuraKey(uint Spell4Id, uint EffectEntryId, uint CasterGuid);
}
