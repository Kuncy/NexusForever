using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;

namespace NexusForever.Game.Spell.ClassMechanics.Engineer;

public sealed class EngineerState : ClassState
{
    public static EngineerState For(IPlayer player) => ClassStates.For<EngineerState>(player);

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

    protected override void OnUpdate(IPlayer player, double lastTick)
    {
        quickBurstTime = Math.Max(0d, quickBurstTime - lastTick);
        feedbackTime = Math.Max(0d, feedbackTime - lastTick);
        exoSuitTime = Math.Max(0d, exoSuitTime - lastTick);
    }
}
