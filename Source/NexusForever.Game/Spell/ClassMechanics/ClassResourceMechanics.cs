using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics;

public static class ClassResourceMechanics
{
    public static void Initialise(IPlayer player)
    {
        ClassMechanicsRegistry.For(player.Class)?.InitialiseResources(player);
    }

    public static void Update(IPlayer player, uint statUpdateTick, double outOfCombatTime)
    {
        ClassMechanicsRegistry.For(player.Class)?.UpdateResources(player, statUpdateTick, outOfCombatTime);
    }

    /// <summary>
    /// Invoked after a vital modification was applied.
    /// </summary>
    /// <param name="amount">Amount that was requested.</param>
    /// <param name="delta">Amount that was actually applied after clamping, can be 0 when the vital was already at its bound.</param>
    public static void OnVitalModified(IPlayer player, Vital vital, float amount, float delta)
    {
        ClassMechanicsRegistry.For(player.Class)?.OnVitalModified(player, vital, amount, delta);
    }
}
