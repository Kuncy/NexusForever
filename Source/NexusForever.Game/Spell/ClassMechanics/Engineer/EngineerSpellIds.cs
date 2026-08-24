namespace NexusForever.Game.Spell.ClassMechanics.Engineer;

public static class EngineerSpellIds
{
    public const uint PulseBlastBase = 26468u;
    public const uint PulseBlast = 42276u;
    public const uint PulseBlastImpact = 37302u;
    public const uint PulseBlastVolatility = 42148u;
    public const uint ModeEradicate = 47860u;
    public const uint ModeEradicateVolatility = 71371u;
    public const uint ModeEradicateActive = 47860u;
    public const uint ModeProvokeActive = 51513u;
    public const uint ElectrocuteBase = 25473u;
    public const uint ParticleEjectorBase = 20628u;
    public const uint FlakCannonBase = 25623u;
    public const uint QuickBurstBase = 25673u;
    public const uint FeedbackBase = 26059u;
    public const uint BoltCasterBase = 20763u;
    public const uint MortarStrikeBase = 25739u;
    public const uint TargetAcquisitionBase = 22652u;
    public const uint ShockPulseBase = 26775u;
    public const uint UrgentWithdrawalBase = 20492u;

    public static bool IsBotBase(uint baseId)
        => baseId is 27002u or 27082u or 26998u or 27021u;
}
