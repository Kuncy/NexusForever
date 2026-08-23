using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.Vital)]
    public class PrerequisiteCheckVital : IPrerequisiteCheck
    {
        #region Dependency Injection

        private readonly ILogger<PrerequisiteCheckVital> log;

        public PrerequisiteCheckVital(
            ILogger<PrerequisiteCheckVital> log)
        {
            this.log = log;
        }

        #endregion

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            float vitalValue = player.GetVitalValue((Vital)objectId);

            switch (comparison)
            {
                case PrerequisiteComparison.Equal:
                    return vitalValue == value;
                case PrerequisiteComparison.NotEqual:
                    return vitalValue != value;
                case PrerequisiteComparison.GreaterThanOrEqual:
                    return vitalValue >= value;
                case PrerequisiteComparison.GreaterThan:
                    return vitalValue > value;
                case PrerequisiteComparison.LessThanOrEqual:
                    return vitalValue <= value;
                case PrerequisiteComparison.LessThan:
                    return vitalValue < value;
                default:
                    log.LogWarning($"Unhandled {comparison} for {PrerequisiteType.Vital}!");
                    return false;
            }
        }
    }
}
