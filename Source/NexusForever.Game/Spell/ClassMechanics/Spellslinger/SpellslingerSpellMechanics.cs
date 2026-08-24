using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Spell.ClassMechanics.Spellslinger;

public static class SpellslingerSpellMechanics
{
    public static bool TryCheckPrerequisites(Spell spell, IPlayer player, out CastResult result)
    {
        result = CastResult.Ok;
        if (player.Class != Class.Spellslinger)
            return false;

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

    public static void BeforeExecute(Spell spell, IPlayer player)
    {
        if (player.Class != Class.Spellslinger)
            return;

        SpellslingerState state = SpellslingerState.For(player);
        uint baseId = spell.Parameters.SpellInfo.BaseInfo.Entry.Id;
        if (baseId == SpellslingerSpellIds.SpellSurgeBuff)
            state.SpellSurgeBuffCastingId = spell.CastingId;
        else if (baseId == SpellslingerSpellIds.FlameBurstBuff)
            state.FlameBurstBuffCastingId = spell.CastingId;
    }

    public static void AfterEffects(Spell spell, IPlayer player)
    {
        if (player.Class == Class.Spellslinger
            && spell.Parameters.SpellInfo.BaseInfo.Entry.Id == SpellslingerSpellIds.FlameBurst)
            SpellslingerState.For(player).ConsumeFlameBurst(player);
    }
}
