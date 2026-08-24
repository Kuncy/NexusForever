namespace NexusForever.Game.Loot
{
    /// <summary>
    /// Divides a currency pool between party members without losing or inventing any amount.
    /// </summary>
    internal static class LootCurrencySplitter
    {
        /// <summary>
        /// Split <paramref name="total"/> into <paramref name="memberCount"/> shares.
        /// </summary>
        /// <remarks>
        /// The remainder is spread one unit at a time over the leading shares, so the shares always
        /// sum back to <paramref name="total"/> and differ by at most one.
        /// </remarks>
        public static ulong[] Split(ulong total, int memberCount)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(memberCount, 1);

            ulong amountPerMember = total / (ulong)memberCount;
            ulong remainder = total % (ulong)memberCount;

            var shares = new ulong[memberCount];
            for (int i = 0; i < memberCount; i++)
                shares[i] = amountPerMember + ((ulong)i < remainder ? 1ul : 0ul);

            return shares;
        }
    }
}
