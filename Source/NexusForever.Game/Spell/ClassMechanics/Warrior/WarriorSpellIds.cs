namespace NexusForever.Game.Spell.ClassMechanics.Warrior;

public static class WarriorSpellIds
{
    public const uint RelentlessStrikes = 18309u;
    public const uint RelentlessStrikesStage2 = 18310u;
    public const uint RelentlessStrikesStage3 = 18311u;
    public const uint RelentlessStrikesStage4 = 55309u;

    public const uint Rampage = 37968u;
    public const uint RampageStage2 = 44605u;
    public const uint RampageStage3 = 47921u;
    public const uint RampageStage4 = 47922u;

    public const uint Whirlwind = 19778u;
    public const uint BolsteringStrike = 18572u;
    public const uint ShieldBurst = 37245u;
    public const uint PlasmaWall = 23169u;
    public const uint PolarityField = 23328u;
    public const uint PolarityFieldAura = 23345u;
    public const uint BreachingStrikes = 18580u;
    public const uint AtomicSpear = 18360u;
    public const uint AugmentedBlade = 30896u;
    public const uint PowerLink = 35146u;
    public const uint Onslaught = 30828u;
    public const uint Juggernaut = 30978u;
    public const uint MenacingStrike = 39339u;
    public const uint LeapMove = 27236u;
    public const uint BumRushMove = 37961u;
    public const uint DefenseGrid = 35526u;
    public const uint SentinelGuard = 37924u;

    public const uint BreachingStrikesBuff = 54378u;
    public const uint AtomicSpearBuff = 50150u;
    public const uint PlasmaWallDamage = 57852u;
    public const uint PlasmaWallDrain = 87477u;
    public const uint PolarityFieldPulse = 38975u;
    public const uint SentinelRetaliation = 54400u;
    public const uint AugmentedBladeOn = 79753u;
    public const uint AugmentedBladeOff = 57357u;
    public const uint PowerLinkBuff = 79787u;
    public const uint PowerLinkOn = 79790u;
    public const uint PowerLinkOff = 79789u;

    public static bool IsRampageStage(uint baseId)
        => baseId is Rampage or RampageStage2 or RampageStage3 or RampageStage4;
}
