using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Engineer;

public sealed class EngineerState
{
    private static readonly ConditionalWeakTable<IPlayer, EngineerState> states = new();

    public static EngineerState For(IPlayer player) => states.GetOrCreateValue(player);

    public bool QuickBurstAvailable => quickBurstTime > 0d;
    public bool FeedbackAvailable => feedbackTime > 0d;
    public bool ExoSuitActive => exoSuitTime > 0d;

    private double quickBurstTime;
    private double feedbackTime;
    private double exoSuitTime;

    public void EnableQuickBurst() => quickBurstTime = 8d;
    public void ConsumeQuickBurst() => quickBurstTime = 0d;
    public void EnableFeedback() => feedbackTime = 8d;
    public void ConsumeFeedback() => feedbackTime = 0d;
    public void EnableExoSuit() => exoSuitTime = 10d;

    public void Update(double lastTick)
    {
        quickBurstTime = Math.Max(0d, quickBurstTime - lastTick);
        feedbackTime = Math.Max(0d, feedbackTime - lastTick);
        exoSuitTime = Math.Max(0d, exoSuitTime - lastTick);
    }
}
