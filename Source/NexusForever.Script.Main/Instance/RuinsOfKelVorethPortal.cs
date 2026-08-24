using NexusForever.Game.Abstract.Entity;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance
{
    /// <summary>
    /// Connects the physical Kel Voreth portal in Auroria with the dungeon's
    /// entrance portal. Matching-created instances keep their own return point.
    /// </summary>
    [ScriptFilterCreatureId(33528)]
    public class RuinsOfKelVorethPortal : IWorldEntityScript, IOwnedScript<IInstancePortalEntity>
    {
        private const ushort AuroriaWorldId = 22;
        private const ushort KelVorethWorldId = 1336;

        private IInstancePortalEntity owner;

        public void OnLoad(IInstancePortalEntity owner)
        {
            this.owner = owner;
        }

        public void OnActivate(IPlayer activator)
        {
            Teleport(activator);
        }

        public void OnActivateCast(IPlayer activator)
        {
            Teleport(activator);
        }

        private void Teleport(IPlayer activator)
        {
            if (!activator.CanTeleport())
                return;

            switch (owner.Map?.Entry.Id)
            {
                case AuroriaWorldId:
                    // WorldLocation2 18557 - The Blood Pit entrance.
                    activator.TeleportTo(KelVorethWorldId, 61.21086f, -854.3312f, 84.63019f);
                    break;
                case KelVorethWorldId:
                    // WorldLocation2 24997 - immediately outside the Auroria portal.
                    activator.TeleportTo(AuroriaWorldId, -761.1812f, -817.8074f, -438.3093f);
                    break;
            }
        }
    }
}
