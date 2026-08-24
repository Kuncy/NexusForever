using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Party;
using NexusForever.Shared;

namespace NexusForever.Game.Tests
{
    /// <summary>
    /// Backs the <see cref="Singleton{T}"/> pattern for tests.
    /// </summary>
    /// <remarks>
    /// Production code resolves singletons through <see cref="LegacyServiceProvider"/>, which the host fills in
    /// at startup. Unit tests have no host, so any access to <c>Something.Instance</c> would throw. Only the
    /// managers the tests actually reach are registered here, and they stay uninitialised - an uninitialised
    /// <see cref="ItemManager"/> reports every item as unknown, which is all the loot tests need.
    /// </remarks>
    internal static class TestServiceProvider
    {
        [ModuleInitializer]
        internal static void Initialise()
        {
            var services = new ServiceCollection();
            services.AddSingletonLegacy<IItemManager, ItemManager>();
            services.AddSingleton<PartyRewardManager>();

            LegacyServiceProvider.Provider = services.BuildServiceProvider();
        }
    }
}
