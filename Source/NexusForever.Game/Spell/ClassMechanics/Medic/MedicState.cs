using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell.ClassMechanics.Medic;

public sealed class MedicState
{
    private static readonly ConditionalWeakTable<IPlayer, MedicState> states = new();

    public static MedicState For(IPlayer player) => states.GetOrCreateValue(player);

    private byte powerChargeStacks;

    public void AddPowerCharge(IPlayer player)
    {
        powerChargeStacks++;
        if (powerChargeStacks < 3)
            return;

        powerChargeStacks = 0;
        player.ModifyVital(Vital.MedicCore, 1f);
    }
}
