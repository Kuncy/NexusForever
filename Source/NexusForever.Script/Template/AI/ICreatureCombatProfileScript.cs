using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Script.Template.AI
{
    /// <summary>
    /// Supplies a combat profile to the generic creature combat AI.
    /// Implementations can use the normal script filters to bind a profile to
    /// creature templates without coupling the AI to zone content.
    /// </summary>
    public interface ICreatureCombatProfileScript : IOwnedScript<ICreatureEntity>
    {
        CombatProfile Profile { get; }
    }
}
