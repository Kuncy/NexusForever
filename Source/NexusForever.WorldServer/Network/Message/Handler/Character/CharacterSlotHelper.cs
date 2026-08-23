using System;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Static.Reward;

namespace NexusForever.WorldServer.Network.Message.Handler.Character
{
    internal static class CharacterSlotHelper
    {
        public const uint DefaultCharacterSlots = 10u;

        public static uint GetMaximumCharacterSlots(IRewardPropertyManager rewardPropertyManager)
        {
            uint rewardSlots = (uint)(rewardPropertyManager
                .GetRewardProperty(RewardPropertyType.CharacterSlots)?
                .GetValue(0u) ?? 0u);

            return Math.Max(DefaultCharacterSlots, rewardSlots);
        }
    }
}
