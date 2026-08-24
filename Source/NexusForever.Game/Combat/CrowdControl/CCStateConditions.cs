using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Combat.CrowdControl
{
    /// <summary>
    /// Evaluates a Spell4CCConditions row against the crowd control currently on a unit.
    /// </summary>
    /// <remarks>
    /// The row holds a mask of the <see cref="CCState"/> that matter for a spell and the values they are required to
    /// have. Every row in the table requires the masked states to be absent, so in practice this is the list of
    /// crowd control that stops the spell, but the check below is written in the general form the columns describe.
    /// </remarks>
    public static class CCStateConditions
    {
        /// <summary>
        /// Returns whether <paramref name="conditions"/> are violated by <paramref name="ccStateMask"/>.
        /// </summary>
        /// <param name="result">The <see cref="CastResult"/> naming the state responsible, when blocked.</param>
        public static bool IsBlocked(uint ccStateMask, Spell4CCConditionsEntry conditions, out CastResult result)
        {
            result = CastResult.Ok;
            if (conditions == null || conditions.CcStateMask == 0u)
                return false;

            uint mismatched = (ccStateMask ^ conditions.CcStateFlagsRequired) & conditions.CcStateMask;
            if (mismatched == 0u)
                return false;

            // name a state the unit is actually under, so the client can say which one stopped the cast
            uint present = mismatched & ccStateMask;
            result = GetCastResult(present != 0u ? present : mismatched);
            return true;
        }

        /// <summary>
        /// Return the <see cref="CastResult"/> naming the lowest <see cref="CCState"/> set in <paramref name="mask"/>.
        /// </summary>
        private static CastResult GetCastResult(uint mask)
        {
            for (int bit = 0; bit < 32; bit++)
            {
                if ((mask & (1u << bit)) == 0u)
                    continue;

                // every CCState has a matching CC prefixed CastResult
                return Enum.TryParse($"CC{(CCState)bit}", out CastResult result)
                    ? result
                    : CastResult.PrereqCasterCast;
            }

            return CastResult.PrereqCasterCast;
        }
    }
}
