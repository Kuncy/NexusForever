using System.Collections;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Spell
{
    public class AuraManager : IAuraManager
    {
        private readonly IUnitEntity owner;
        private readonly Dictionary<AuraKey, IAura> auras = [];

        /// <summary>
        /// Keys collected during <see cref="Update"/>, reused to avoid allocating on every tick.
        /// </summary>
        private readonly List<AuraKey> expired = [];

        public AuraManager(IUnitEntity owner)
        {
            this.owner = owner;
        }

        public IAura Apply(IAura aura)
        {
            if (auras.TryGetValue(aura.Key, out IAura existing))
            {
                existing.Refresh();
                return existing;
            }

            auras.Add(aura.Key, aura);
            aura.Apply(owner);
            return aura;
        }

        public IAura Get(AuraKey key)
        {
            return auras.GetValueOrDefault(key);
        }

        public bool Remove(AuraKey key, AuraRemoveReason reason)
        {
            if (!auras.Remove(key, out IAura aura))
                return false;

            aura.Remove(owner, reason);
            return true;
        }

        public void RemoveByCaster(uint casterGuid, AuraRemoveReason reason)
        {
            foreach (AuraKey key in auras.Values
                .Where(a => a.Caster?.Guid == casterGuid)
                .Select(a => a.Key)
                .ToList())
                Remove(key, reason);
        }

        public void RemoveAll(AuraRemoveReason reason)
        {
            foreach (AuraKey key in auras.Keys.ToList())
                Remove(key, reason);
        }

        public void Update(double lastTick)
        {
            if (auras.Count == 0)
                return;

            // an aura can remove others from its tick, so collect first and remove afterwards
            foreach (IAura aura in auras.Values.ToList())
                if (!aura.Update(owner, lastTick))
                    expired.Add(aura.Key);

            foreach (AuraKey key in expired)
                Remove(key, AuraRemoveReason.Expired);

            expired.Clear();
        }

        public IEnumerator<IAura> GetEnumerator()
        {
            return auras.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
