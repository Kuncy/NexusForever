namespace NexusForever.Game.Static.Combat.CrowdControl
{
    /// <summary>
    /// Bits of the Flags column in the CCStates table.
    /// </summary>
    public static class CCStateFlags
    {
        /// <summary>
        /// The affected unit cannot move under its own power.
        /// </summary>
        /// <remarks>
        /// The table has no column saying so outright, this is read from the Flags bit that is set on exactly the
        /// states that stop movement and on no others: Stun, Sleep, Root, Hold, Knockdown, Disable, Knockback,
        /// Pushback, Pull and PositionSwitch have it, while Snare, Tether, Fear, Disorient, Silence, Disarm and
        /// Blind do not. Rows whose Flags are zero altogether are not covered by this.
        /// </remarks>
        public const uint PreventsMovement = 0x02;
    }
}
