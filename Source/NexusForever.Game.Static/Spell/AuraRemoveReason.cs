namespace NexusForever.Game.Static.Spell
{
    /// <summary>
    /// Reason an aura stopped affecting its target.
    /// </summary>
    public enum AuraRemoveReason
    {
        /// <summary>
        /// The duration ran out.
        /// </summary>
        Expired,

        /// <summary>
        /// The same aura was applied again by the same caster and took over.
        /// </summary>
        Refreshed,

        /// <summary>
        /// Removed early by another effect, such as a dispel or a crowd control break.
        /// </summary>
        Dispelled,

        /// <summary>
        /// The caster is no longer available to maintain the aura.
        /// </summary>
        CasterGone,

        /// <summary>
        /// The target died.
        /// </summary>
        Death
    }
}
