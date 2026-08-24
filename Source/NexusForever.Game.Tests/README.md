# NexusForever.Game.Tests

Unit tests for game logic that can be exercised without a running server, a client data directory or a database.

## Running

```
dotnet run --project Source/NexusForever.Game.Tests
```

`dotnet test` does not work here yet: xunit.v3 runs on Microsoft.Testing.Platform, and the .NET 10 SDK removed the
VSTest bridge that `dotnet test` still tries to use for this project. The `dotnet.config` at the repository root
already contains the documented `[dotnet.test.runner]` opt-in, but SDK 10.0.400 does not honour it. Until that is
fixed, run the test project directly with `dotnet run` (the test assembly is a self-hosting executable).

## What is covered

| Area | Tests |
| --- | --- |
| `LootTableRoller` | Independent vs. grouped entries, chance bounds, group exclusivity, amount ranges, seed determinism |
| `LootCurrencySplitter` | Even splits, remainder distribution, sum invariants, argument validation |
| `LootInstanceItem` | Delivery into a full bag, partial delivery, retry, currency, unsupported loot types |
| `LootManager.IsInLootRange` | The 35 unit loot leash, despawned owner, player without a map |
| `SearchCheckRange` | The vision-range filter that decides which party members share a kill |
| `PartyRewardManager` | Solo path, duplicate participants, party membership, out-of-range parties, member ordering |
| `AuraManager` | Apply and expiry, refresh on recast, key separation, durationless auras, removal by caster |
| `CCStateAura` | Telling the client which effect ended, silence while running, early removal, CastResult coverage |

## Conventions

* Anything involving randomness uses a fixed seed, so statistical assertions are reproducible. A failing
  distribution test means the roller changed, not that the dice were unlucky.
* Singletons resolve through `LegacyServiceProvider`, which `TestServiceProvider` fills with a minimal container.
  Add a registration there when a test reaches a new `Something.Instance`.
* Test doubles use NSubstitute against the `NexusForever.Game.Abstract` interfaces rather than real entities.
* Internals of `NexusForever.Game` are visible here via `InternalsVisibleTo` in `NexusForever.Game.csproj`.
