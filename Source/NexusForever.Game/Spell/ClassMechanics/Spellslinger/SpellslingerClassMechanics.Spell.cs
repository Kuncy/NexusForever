using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Spell.ClassMechanics.Spellslinger;

public sealed partial class SpellslingerClassMechanics
{
    public bool IsServerExecutedChannel(Spell spell)
        => spell.Parameters.SpellInfo.BaseInfo.Entry.Id is 20734u or 20735u
            or 27736u or 27784u;

    public bool TryCheckPrerequisites(Spell spell, IPlayer player, out CastResult result)
    {
        result = CastResult.Ok;
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (baseId == SpellslingerSpellIds.SpellSurge)
            return true;

        if (baseId == SpellslingerSpellIds.FlameBurst)
        {
            result = SpellslingerState.For(player).FlameBurstAvailable
                ? CastResult.Ok
                : CastResult.PrereqCasterCast;
            return true;
        }

        return false;
    }

    public void BeforeExecute(Spell spell, IPlayer player)
    {
        SpellslingerState state = SpellslingerState.For(player);
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (baseId == SpellslingerSpellIds.SpellSurgeBuff)
            state.SpellSurgeBuffCastingId = spell.CastingId;
        else if (baseId == SpellslingerSpellIds.FlameBurstBuff)
            state.FlameBurstBuffCastingId = spell.CastingId;
    }

    public void AfterEffects(Spell spell, IPlayer player)
    {
        if (spell.Parameters.SpellInfo.BaseInfo.Entry.Id == SpellslingerSpellIds.FlameBurst)
            SpellslingerState.For(player).ConsumeFlameBurst(player);
    }
}
