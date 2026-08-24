# NexusForever Loot Importer

Generates idempotent zone loot SQL from a reviewed JSON manifest. The importer deliberately does not scrape live
web pages: source sites are historical, occasionally unavailable, and their HTML is not a stable data contract.

The manifest must classify every creature id spawned by the zone dump. An id is either assigned to a loot table or
listed under `ignored` with a reason. Generation fails on partial coverage, unknown client item/creature ids,
inconsistent observed drop rates, invalid probability groups, or duplicate ids.

```powershell
dotnet run --project Tools/NexusForever.LootImporter -- inventory `
  --zone-sql ..\NexusForever.WorldDatabase\Olyssia\CrimsonIsle.sql

dotnet run --project Tools/NexusForever.LootImporter -- generate `
  --manifest ..\NexusForever.WorldDatabase\Loot\Sources\CrimsonIsle.json `
  --table-path ..\server-data\tbl
```

The JSON manifest is the reviewable source of truth. Generated `.Loot.sql` files remain the only files consumed by
the world database migration service.
