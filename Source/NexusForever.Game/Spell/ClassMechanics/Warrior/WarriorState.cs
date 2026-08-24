using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;

namespace NexusForever.Game.Spell.ClassMechanics.Warrior;

public sealed class WarriorState : ClassState
{
    public static WarriorState For(IPlayer player) => ClassStates.For<WarriorState>(player);

    public bool BreachingStrikesAvailable => breachingStrikesTime > 0d;
    public uint? BreachingStrikesBuffCastingId { get; set; }
    public bool AtomicSpearAvailable => atomicSpearTime > 0d;
    public uint? AtomicSpearBuffCastingId { get; set; }
    public bool AugmentedBladeActive { get; set; }
    public bool PowerLinkActive { get; set; }
    public bool OverdriveActive => overdriveTime > 0d;

    public byte RelentlessStage { get; set; }
    public double RelentlessExpiresAt { get; set; }
    public byte RampageStage { get; set; }
    public double RampageExpiresAt { get; set; }

    private double breachingStrikesTime;
    private double atomicSpearTime;
    private double overdriveTime;
    private double kineticEnergyGraceTime;

    public void EnableBreachingStrikes(IPlayer player)
    {
        if (BreachingStrikesAvailable)
            return;

        breachingStrikesTime = 6d;
        player.CastSpell(WarriorSpellIds.BreachingStrikesBuff, new SpellParameters());
    }

    public void ConsumeBreachingStrikes(IPlayer player)
    {
        breachingStrikesTime = 0d;
        if (!BreachingStrikesBuffCastingId.HasValue)
            return;

        RemoveBuff(player, BreachingStrikesBuffCastingId.Value);
        BreachingStrikesBuffCastingId = null;
    }

    public void EnableAtomicSpear(IPlayer player)
    {
        if (AtomicSpearAvailable)
            return;

        atomicSpearTime = 5d;
        player.CastSpell(WarriorSpellIds.AtomicSpearBuff, new SpellParameters());
    }

    public void ConsumeAtomicSpear(IPlayer player)
    {
        atomicSpearTime = 0d;
        if (!AtomicSpearBuffCastingId.HasValue)
            return;

        RemoveBuff(player, AtomicSpearBuffCastingId.Value);
        AtomicSpearBuffCastingId = null;
    }

    public void EnableOverdrive() => overdriveTime = 8d;

    public void StartKineticEnergyGracePeriod() => kineticEnergyGraceTime = 1.5d;

    public bool CanDecayKineticEnergy => kineticEnergyGraceTime <= 0d && !OverdriveActive;

    protected override void OnUpdate(IPlayer player, double lastTick)
    {
        if (breachingStrikesTime > 0d)
        {
            breachingStrikesTime = Math.Max(0d, breachingStrikesTime - lastTick);
            if (breachingStrikesTime == 0d)
                ConsumeBreachingStrikes(player);
        }

        if (atomicSpearTime > 0d)
        {
            atomicSpearTime = Math.Max(0d, atomicSpearTime - lastTick);
            if (atomicSpearTime == 0d)
                ConsumeAtomicSpear(player);
        }

        overdriveTime = Math.Max(0d, overdriveTime - lastTick);
        kineticEnergyGraceTime = Math.Max(0d, kineticEnergyGraceTime - lastTick);
    }
}
