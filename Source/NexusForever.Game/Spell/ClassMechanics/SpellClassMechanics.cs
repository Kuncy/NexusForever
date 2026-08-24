using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Spell.ClassMechanics;

/// <summary>
/// Entry point for the spell cast related <see cref="IClassMechanics"/> hooks.
/// </summary>
/// <remarks>
/// Hooks that carry the casting <see cref="IPlayer"/> are dispatched to that player's class, hooks that are keyed
/// on the spell alone are offered to every implementation as the caster isn't necessarily a player.
/// </remarks>
public static class SpellClassMechanics
{
    public static bool TryCheckPrerequisites(Spell spell, IPlayer player, out CastResult result)
    {
        IClassMechanics mechanics = ClassMechanicsRegistry.For(player.Class);
        if (mechanics != null)
            return mechanics.TryCheckPrerequisites(spell, player, out result);

        result = CastResult.Ok;
        return false;
    }

    public static void BeforeExecute(Spell spell, IPlayer player)
    {
        ClassMechanicsRegistry.For(player.Class)?.BeforeExecute(spell, player);
    }

    public static Spell4Entry SelectCooldownEntry(Spell spell, IPlayer player, Spell4Entry entry)
    {
        IClassMechanics mechanics = ClassMechanicsRegistry.For(player.Class);
        return mechanics != null ? mechanics.SelectCooldownEntry(spell, player, entry) : entry;
    }

    public static void AfterEffects(Spell spell, IPlayer player)
    {
        ClassMechanicsRegistry.For(player.Class)?.AfterEffects(spell, player);
    }

    public static void AfterSpellGo(Spell spell, IPlayer player)
    {
        ClassMechanicsRegistry.For(player.Class)?.AfterSpellGo(spell, player);
    }

    public static bool TryCostResources(Spell spell, IPlayer player, Spell4Entry entry)
    {
        return ClassMechanicsRegistry.For(player.Class)?.TryCostResources(spell, player, entry) ?? false;
    }

    public static bool IsServerExecutedChannel(Spell spell)
    {
        return ClassMechanicsRegistry.All.Any(m => m.IsServerExecutedChannel(spell));
    }
}
