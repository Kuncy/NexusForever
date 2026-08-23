using NexusForever.Game.Abstract.Spell;
using NexusForever.Network.World.Entity;

namespace NexusForever.Game.Spell
{
    public class SpellParameters : ISpellParameters
    {
        public ICharacterSpell CharacterSpell { get; set; }
        public ISpellInfo SpellInfo { get; set; }
        public ISpellInfo ParentSpellInfo { get; set; }
        public ISpellInfo RootSpellInfo { get; set; }
        public bool UserInitiatedSpellCast { get; set; }
        public bool ParentSpellSuccessfulHit { get; set; }
        public int CastTimeOverride { get; set; }
        public uint ActivationTargetGuid { get; set; }
        public bool QuestEntityActivation { get; set; }
        public uint ClientUniqueId { get; set; }
        public uint PrimaryTargetId { get; set; }
        public Position Position { get; set; }
        public ushort TaxiNode { get; set; }
    }
}
