using NexusForever.Game.Static.Combat.CrowdControl;

namespace NexusForever.Game.Abstract.Spell
{
    /// <summary>
    /// An <see cref="IAura"/> that holds a crowd control state on its target.
    /// </summary>
    public interface ICCStateAura : IAura
    {
        CCState State { get; }
    }
}
