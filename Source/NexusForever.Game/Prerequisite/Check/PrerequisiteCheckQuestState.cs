using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.QuestState)]
    public class PrerequisiteCheckQuestState : IPrerequisiteCheck
    {
        #region Dependency Injection

        private readonly ILogger<PrerequisiteCheckQuestState> log;

        public PrerequisiteCheckQuestState(
            ILogger<PrerequisiteCheckQuestState> log)
        {
            this.log = log;
        }

        #endregion

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            // A quest without saved state has never been started and is therefore Unknown.
            // Several client prerequisites explicitly compare against Unknown to make quests
            // available before an optional prerequisite quest has been accepted.
            QuestState state = player.QuestManager.GetQuestState((ushort)objectId) ?? QuestState.Unknown;

            switch (comparison)
            {
                case PrerequisiteComparison.Equal:
                    return state == (QuestState)value;
                case PrerequisiteComparison.NotEqual:
                    return state != (QuestState)value;
                default:
                    log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.QuestState}!");
                    return false;
            }
        }
    }
}
