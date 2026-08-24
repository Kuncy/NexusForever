using System.Reflection;
using NexusForever.GameTable;

namespace NexusForever.Game.Tests.GameTable;

public class GameTableManagerTests
{
    [Fact]
    public void CrowdControlStatesAreLoadedDuringDefaultInitialisation()
    {
        PropertyInfo property = typeof(GameTableManager).GetProperty(nameof(GameTableManager.CCStates));

        Assert.NotNull(property);
        Assert.NotNull(property.GetCustomAttribute<GameDataAttribute>());
    }
}
