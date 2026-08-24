namespace NexusForever.Game.Abstract.Entity
{
    public interface ITrapEntity : IWorldEntity
    {
        void Initialise(IPlayer owner, uint creatureId, uint triggerSpellId,
            uint duration, float triggerRadius);
    }
}
