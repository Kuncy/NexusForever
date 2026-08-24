namespace NexusForever.Game.Spell.ClassMechanics.Stalker;

public static class StalkerSpellIds
{
    public const uint ShredBase = 23148u;
    public const uint ShredStealthBase = 23149u;
    public const uint Shred = 38765u;
    public const uint ShredStealth = 38766u;
    public const uint ShredImpact = 38767u;
    public const uint ShredStealthImpact = 39467u;
    public const uint Impale = 38779u;
    public const uint ImpaleNormalImpact = 39426u;
    public const uint Stagger = 38780u;
    public const uint StaggerImpact = 38781u;

    public const uint AnalyzeWeaknessBase = 23183u;
    public const uint AnalyzeWeakness = 38803u;
    public const uint AnalyzeWeaknessMark = 60738u;
    public const uint PunishBase = 32336u;
    public const uint Punish = 48584u;
    public const uint PunishAvailableBuff = 48585u;
    public const uint Whiplash = 39157u;
    public const uint WhiplashFirstImpact = 39167u;
    public const uint WhiplashSecondImpact = 39169u;
    public const uint FrenzyBase = 23699u;
    public const uint RazorStormBase = 23892u;
    public const uint PounceMove = 42677u;
    public const uint SteadfastBase = 31976u;
    public const uint NeutralizeBase = 23218u;
    public const uint RuinBase = 23221u;
    public const uint Decimate = 48033u;
    public const uint FalseRetreatBase = 23587u;
    public const uint FalseRetreatReturn = 39255u;
    public const uint TacticalRetreatBase = 23281u;
    public const uint CloneBase = 23337u;
    public const uint CloneSlash = 38985u;
    public const uint ClonePrecisionStrike = 38989u;
    public const uint NanoFieldBase = 23523u;
    public const uint NanoFieldEndBase = 23531u;
    public const uint NanoFieldPulse = 39185u;
    public const uint NanoFieldEnd = 39189u;
    public const uint CollapseBase = 23705u;
    public const uint ConcussiveKicksBase = 23984u;
    public const uint PreparationBase = 32921u;
    public const uint AmplificationSpikeBase = 23955u;

    public const uint NanoSkinAgileBase = 23164u;
    public const uint NanoSkinLethalBase = 30075u;
    public const uint NanoSkinEvasiveBase = 30076u;

    public static bool IsNanoSkin(uint baseId)
        => baseId is NanoSkinAgileBase or NanoSkinLethalBase or NanoSkinEvasiveBase;
}
