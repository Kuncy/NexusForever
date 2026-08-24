namespace NexusForever.Game.Spell.ClassMechanics.Esper;

public static class EsperSpellIds
{
    public const uint MindBurst = 19019u;
    public const uint TelekineticStrike = 32893u;
    public const uint TelekineticStrikePsiPoint = 30900u;
    public const uint PsiPointBuilder = 32822u;
    public const uint ReapBase = 19136u;
    public const uint PsychicFrenzyBase = 21613u;
    public const uint ReverieBase = 19218u;
    public const uint TelekineticStormBase = 19024u;
    public const uint BladeDanceBase = 19163u;
    public const uint MentalBoonBase = 19258u;
    public const uint MendingBannerBase = 19341u;
    public const uint FixationBase = 19273u;
    public const uint SpectralFormBase = 38012u;
    public const uint FadeOutBase = 19190u;
    public const uint ProjectedSpiritBase = 21812u;

    public static bool IsPsiFinisher(uint baseId)
        => baseId is MindBurst or ReverieBase or TelekineticStormBase
            or BladeDanceBase or MentalBoonBase or MendingBannerBase;
}
