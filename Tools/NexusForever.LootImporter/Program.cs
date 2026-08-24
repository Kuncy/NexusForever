using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

try
{
    CliOptions options = CliOptions.Parse(args);
    if (options.Command == "inventory")
    {
        ZoneInventory inventory = ZoneSqlParser.Parse(options.ZoneSql!);
        if (options.TablePath != null)
        {
            GameData inventoryGameData = GameData.Load(options.TablePath);
            foreach (ZoneSpawn spawn in inventory.Spawns.Values)
            {
                if (!inventoryGameData.Creatures.TryGetValue(spawn.CreatureId, out Creature2Entry? creature))
                    continue;
                spawn.RaceId = creature.UnitRaceId;
                spawn.TierId = creature.Creature2TierId;
                spawn.DifficultyId = creature.Creature2DifficultyId;
            }
        }
        Console.WriteLine(JsonSerializer.Serialize(inventory.Spawns.Values.OrderBy(spawn => spawn.CreatureId), Json.Options));
        return 0;
    }

    LootManifest manifest = JsonSerializer.Deserialize<LootManifest>(File.ReadAllText(options.Manifest!), Json.Options)
        ?? throw new InvalidDataException("The manifest is empty.");
    string manifestDirectory = Path.GetDirectoryName(Path.GetFullPath(options.Manifest!))!;
    string zoneSql = ResolvePath(manifestDirectory, manifest.ZoneSql);
    string output = ResolvePath(manifestDirectory, manifest.Output);
    ZoneInventory zone = ZoneSqlParser.Parse(zoneSql);
    GameData? gameData = options.TablePath == null ? null : GameData.Load(options.TablePath);

    ManifestValidator.Validate(manifest, zone, gameData);
    string sql = SqlGenerator.Generate(manifest, zone);

    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
    File.WriteAllText(output, sql, new UTF8Encoding(false));

    int lootable = manifest.Assignments.Count;
    int ignored = manifest.Ignored.Count;
    Console.WriteLine($"Generated {output}");
    Console.WriteLine($"Zone: {manifest.Zone} | spawned ids: {zone.Spawns.Count} | loot assignments: {lootable} | intentionally ignored: {ignored} | tables: {manifest.Tables.Count}");
    if (gameData == null)
        Console.WriteLine("WARNING: item and creature ids were not checked because --table-path was omitted.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Loot import failed: {exception.Message}");
    return 1;
}

static string ResolvePath(string basePath, string path) =>
    Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(basePath, path));

internal sealed record CliOptions(string Command, string? Manifest, string? ZoneSql, string? TablePath)
{
    public static CliOptions Parse(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("""
                NexusForever Loot Importer

                Generate and validate a zone loot dump:
                  dotnet run --project Tools/NexusForever.LootImporter -- generate --manifest <file.json> [--table-path <tbl directory>]

                Print the spawned creature inventory for a zone dump:
                  dotnet run --project Tools/NexusForever.LootImporter -- inventory --zone-sql <zone.sql>
                """);
            Environment.Exit(0);
        }

        string command = args[0].ToLowerInvariant();
        if (command is not ("generate" or "inventory"))
            throw new ArgumentException($"Unknown command '{args[0]}'.");

        Dictionary<string, string> values = [];
        for (int i = 1; i < args.Length; i += 2)
        {
            if (i + 1 >= args.Length || !args[i].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Expected --name value near argument {i + 1}.");
            values[args[i][2..]] = args[i + 1];
        }

        values.TryGetValue("manifest", out string? manifest);
        values.TryGetValue("zone-sql", out string? zoneSql);
        values.TryGetValue("table-path", out string? tablePath);
        if (command == "generate" && string.IsNullOrWhiteSpace(manifest))
            throw new ArgumentException("generate requires --manifest.");
        if (command == "inventory" && string.IsNullOrWhiteSpace(zoneSql))
            throw new ArgumentException("inventory requires --zone-sql.");

        return new CliOptions(command, manifest, zoneSql, tablePath);
    }
}

internal static class ZoneSqlParser
{
    private static readonly Regex HeadingRegex = new(@"^--\s+(?<name>.+?)\s*$", RegexOptions.Compiled);
    private static readonly Regex CreatureRegex = new(@"\(@GUID\+\d+,\s*0,\s*(?<id>\d+),", RegexOptions.Compiled);

    public static ZoneInventory Parse(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Zone SQL was not found.", path);

        Dictionary<uint, ZoneSpawn> spawns = [];
        string heading = "Unknown";
        foreach (string line in File.ReadLines(path))
        {
            Match headingMatch = HeadingRegex.Match(line);
            if (headingMatch.Success && !headingMatch.Groups["name"].Value.All(character => character == '-'))
                heading = CleanName(headingMatch.Groups["name"].Value);

            foreach (Match creatureMatch in CreatureRegex.Matches(line))
            {
                uint id = uint.Parse(creatureMatch.Groups["id"].Value);
                if (spawns.TryGetValue(id, out ZoneSpawn? existing))
                {
                    existing.SpawnCount++;
                    if (existing.Name == "Unknown" && heading != "Unknown")
                        existing.Name = heading;
                }
                else
                {
                    spawns.Add(id, new ZoneSpawn(id, heading, 1));
                }
            }
        }

        if (spawns.Count == 0)
            throw new InvalidDataException($"No creature spawns were found in {path}.");
        return new ZoneInventory(path, spawns);
    }

    private static string CleanName(string value)
    {
        int pluralMarker = value.IndexOf("{p:", StringComparison.Ordinal);
        return (pluralMarker < 0 ? value : value[..pluralMarker]).Trim();
    }
}

internal static class ManifestValidator
{
    private const uint ChanceScale = 1_000_000u;

    public static void Validate(LootManifest manifest, ZoneInventory zone, GameData? gameData)
    {
        if (string.IsNullOrWhiteSpace(manifest.Zone))
            throw new InvalidDataException("zone is required.");
        if (manifest.Tables.Count == 0)
            throw new InvalidDataException("At least one loot table is required.");

        EnsureUnique(manifest.Tables.Select(table => table.Id), "loot table id");
        EnsureUnique(manifest.Assignments.Select(assignment => assignment.CreatureId), "assigned creature id");
        EnsureUnique(manifest.Ignored.Select(ignored => ignored.CreatureId), "ignored creature id");

        HashSet<uint> tableIds = manifest.Tables.Select(table => table.Id).ToHashSet();
        HashSet<uint> classifiedIds = manifest.Assignments.Select(assignment => assignment.CreatureId)
            .Concat(manifest.Ignored.Select(ignored => ignored.CreatureId))
            .ToHashSet();
        uint[] missing = zone.Spawns.Keys.Except(classifiedIds).Order().ToArray();
        uint[] foreign = classifiedIds.Except(zone.Spawns.Keys).Order().ToArray();
        if (missing.Length > 0)
            throw new InvalidDataException($"Zone coverage is incomplete. Unclassified creature ids: {string.Join(", ", missing)}.");
        if (foreign.Length > 0)
            throw new InvalidDataException($"Manifest contains creature ids not spawned in the zone: {string.Join(", ", foreign)}.");

        HashSet<uint> both = manifest.Assignments.Select(assignment => assignment.CreatureId)
            .Intersect(manifest.Ignored.Select(ignored => ignored.CreatureId)).ToHashSet();
        if (both.Count > 0)
            throw new InvalidDataException($"Creature ids cannot be both assigned and ignored: {string.Join(", ", both.Order())}.");

        foreach (LootAssignment assignment in manifest.Assignments)
        {
            if (!tableIds.Contains(assignment.TableId))
                throw new InvalidDataException($"Creature {assignment.CreatureId} references missing loot table {assignment.TableId}.");
            ValidateName(assignment.CreatureId, assignment.Name, zone, gameData);
        }
        foreach (IgnoredCreature ignored in manifest.Ignored)
        {
            if (string.IsNullOrWhiteSpace(ignored.Reason))
                throw new InvalidDataException($"Ignored creature {ignored.CreatureId} requires a reason.");
            ValidateName(ignored.CreatureId, ignored.Name, zone, gameData);
        }

        foreach (LootTableSource table in manifest.Tables)
        {
            if (string.IsNullOrWhiteSpace(table.Description) || string.IsNullOrWhiteSpace(table.Source) || string.IsNullOrWhiteSpace(table.SourceVersion))
                throw new InvalidDataException($"Loot table {table.Id} requires description, source and sourceVersion.");
            if (table.Entries.Count == 0)
                throw new InvalidDataException($"Loot table {table.Id} has no entries.");

            foreach (LootEntrySource entry in table.Entries)
            {
                if (entry.MinAmount == 0 || entry.MaxAmount < entry.MinAmount)
                    throw new InvalidDataException($"Loot table {table.Id}, item {entry.ItemId} has an invalid amount range.");
                if (entry.Chance > ChanceScale)
                    throw new InvalidDataException($"Loot table {table.Id}, item {entry.ItemId} has chance above {ChanceScale}.");
                if (entry.ObservedDrops.HasValue != entry.ObservedAttempts.HasValue || entry.ObservedDrops > entry.ObservedAttempts || entry.ObservedAttempts == 0)
                    throw new InvalidDataException($"Loot table {table.Id}, item {entry.ItemId} has invalid observation data.");
                if (entry.ObservedDrops.HasValue)
                {
                    uint expected = (uint)Math.Round((double)entry.ObservedDrops.Value / entry.ObservedAttempts!.Value * ChanceScale);
                    if (entry.Chance != expected)
                        throw new InvalidDataException($"Loot table {table.Id}, item {entry.ItemId}: chance {entry.Chance} does not match observations ({expected}).");
                }
                if (gameData != null && !gameData.Items.ContainsKey(entry.ItemId))
                    throw new InvalidDataException($"Loot table {table.Id} references item {entry.ItemId}, which is absent from Item2.tbl.");
            }

            foreach (IGrouping<uint, LootEntrySource> group in table.Entries.Where(entry => entry.GroupId != 0).GroupBy(entry => entry.GroupId))
                if (group.Aggregate<LootEntrySource, ulong>(0, (sum, entry) => sum + entry.Chance) > ChanceScale)
                    throw new InvalidDataException($"Loot table {table.Id}, group {group.Key} exceeds {ChanceScale} total chance.");
        }
    }

    private static void ValidateName(uint creatureId, string name, ZoneInventory zone, GameData? gameData)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidDataException($"Creature {creatureId} requires a name.");
        if (gameData != null && !gameData.Creatures.ContainsKey(creatureId))
            throw new InvalidDataException($"Creature {creatureId} is absent from Creature2.tbl.");
        if (!string.Equals(zone.Spawns[creatureId].Name, name, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Creature {creatureId} is named '{zone.Spawns[creatureId].Name}' in the zone dump, not '{name}'.");
    }

    private static void EnsureUnique(IEnumerable<uint> values, string label)
    {
        uint[] duplicates = values.GroupBy(value => value).Where(group => group.Count() > 1).Select(group => group.Key).Order().ToArray();
        if (duplicates.Length > 0)
            throw new InvalidDataException($"Duplicate {label}s: {string.Join(", ", duplicates)}.");
    }
}

internal static class SqlGenerator
{
    public static string Generate(LootManifest manifest, ZoneInventory zone)
    {
        var sql = new StringBuilder();
        sql.AppendLine("-- --------------------------------------");
        sql.AppendLine($"-- {manifest.Zone} Loot Dump");
        sql.AppendLine("-- --------------------------------------");
        sql.AppendLine($"-- Generated from {Path.GetFileName(zone.Path)} and {manifest.Tables.Count} source table(s).");
        sql.AppendLine("-- Every spawned creature id is explicitly assigned or documented as intentionally lootless in the source manifest.");
        sql.AppendLine("-- Reapplying is safe: deleting the owned loot tables cascades to assignments and entries.");
        sql.AppendLine("-- --------------------------------------");
        sql.AppendLine($"DELETE FROM `loot_table` WHERE `id` IN ({string.Join(", ", manifest.Tables.Select(table => table.Id).Order())});");
        sql.AppendLine();

        foreach (LootTableSource table in manifest.Tables.OrderBy(table => table.Id))
        {
            sql.AppendLine("-- --------------------------------------");
            sql.AppendLine($"-- {EscapeComment(table.Description)} ({table.Id})");
            sql.AppendLine("-- --------------------------------------");
            sql.AppendLine($"SET @SOURCE = '{EscapeSql(table.Source)}';");
            sql.AppendLine($"SET @SOURCE_VERSION = '{EscapeSql(table.SourceVersion)}';");
            sql.AppendLine();
            sql.AppendLine("INSERT INTO `loot_table` (`id`, `description`) VALUES");
            sql.AppendLine($"    ({table.Id}, '{EscapeSql(table.Description)}');");
            sql.AppendLine();

            LootAssignment[] assignments = manifest.Assignments.Where(assignment => assignment.TableId == table.Id)
                .OrderBy(assignment => assignment.CreatureId).ToArray();
            sql.AppendLine("INSERT INTO `creature_loot` (`creatureId`, `lootTableId`) VALUES");
            for (int i = 0; i < assignments.Length; i++)
            {
                LootAssignment assignment = assignments[i];
                sql.AppendLine($"    ({assignment.CreatureId}, {table.Id}){(i == assignments.Length - 1 ? ";" : ",")} -- {EscapeComment(assignment.Name)}");
            }
            sql.AppendLine();
            sql.AppendLine("INSERT INTO `loot_table_entry`");
            sql.AppendLine("    (`lootTableId`, `type`, `itemId`, `minAmount`, `maxAmount`, `chance`, `groupId`, `source`, `sourceVersion`, `observedDrops`, `observedAttempts`) VALUES");
            for (int i = 0; i < table.Entries.Count; i++)
            {
                LootEntrySource entry = table.Entries[i];
                string drops = entry.ObservedDrops?.ToString() ?? "NULL";
                string attempts = entry.ObservedAttempts?.ToString() ?? "NULL";
                sql.AppendLine($"    ({table.Id}, 0, {entry.ItemId}, {entry.MinAmount}, {entry.MaxAmount}, {entry.Chance}, {entry.GroupId}, @SOURCE, @SOURCE_VERSION, {drops}, {attempts}){(i == table.Entries.Count - 1 ? ";" : ",")}");
            }
            sql.AppendLine();
        }

        sql.AppendLine("-- Intentionally lootless/non-combat zone entities are documented in the JSON source manifest.");
        return sql.ToString();
    }

    private static string EscapeSql(string value) => value.Replace("'", "''", StringComparison.Ordinal);
    private static string EscapeComment(string value) => value.Replace("\r", " ").Replace("\n", " ");
}

internal sealed class GameData
{
    public required IReadOnlyDictionary<uint, Item2Entry> Items { get; init; }
    public required IReadOnlyDictionary<uint, Creature2Entry> Creatures { get; init; }

    public static GameData Load(string path)
    {
        string itemPath = Path.Combine(path, "Item2.tbl");
        string creaturePath = Path.Combine(path, "Creature2.tbl");
        if (!File.Exists(itemPath) || !File.Exists(creaturePath))
            throw new DirectoryNotFoundException($"Expected Item2.tbl and Creature2.tbl in {path}.");
        return new GameData
        {
            Items = new GameTable<Item2Entry>(itemPath).Entries.ToDictionary(entry => entry.Id),
            Creatures = new GameTable<Creature2Entry>(creaturePath).Entries.ToDictionary(entry => entry.Id)
        };
    }
}

internal static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
}

internal sealed record ZoneInventory(string Path, Dictionary<uint, ZoneSpawn> Spawns);
internal sealed class ZoneSpawn(uint creatureId, string name, int spawnCount)
{
    public uint CreatureId { get; } = creatureId;
    public string Name { get; set; } = name;
    public int SpawnCount { get; set; } = spawnCount;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? RaceId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? TierId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? DifficultyId { get; set; }
}

internal sealed class LootManifest
{
    public string Zone { get; init; } = "";
    public string ZoneSql { get; init; } = "";
    public string Output { get; init; } = "";
    public List<LootTableSource> Tables { get; init; } = [];
    public List<LootAssignment> Assignments { get; init; } = [];
    public List<IgnoredCreature> Ignored { get; init; } = [];
}

internal sealed class LootTableSource
{
    public uint Id { get; init; }
    public string Description { get; init; } = "";
    public string Source { get; init; } = "";
    public string SourceVersion { get; init; } = "";
    public List<LootEntrySource> Entries { get; init; } = [];
}

internal sealed class LootEntrySource
{
    public uint ItemId { get; init; }
    public uint MinAmount { get; init; } = 1;
    public uint MaxAmount { get; init; } = 1;
    public uint Chance { get; init; }
    public uint GroupId { get; init; }
    public uint? ObservedDrops { get; init; }
    public uint? ObservedAttempts { get; init; }
}

internal sealed record LootAssignment(uint CreatureId, string Name, uint TableId);
internal sealed record IgnoredCreature(uint CreatureId, string Name, string Reason);
