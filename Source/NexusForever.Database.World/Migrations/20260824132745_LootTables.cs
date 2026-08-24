using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <inheritdoc />
    public partial class LootTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "loot_table",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    description = table.Column<string>(type: "varchar(200)", nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "creature_loot",
                columns: table => new
                {
                    creatureId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    lootTableId = table.Column<uint>(type: "int(10) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.creatureId, x.lootTableId });
                    table.ForeignKey(
                        name: "FK_creature_loot_loot_table",
                        column: x => x.lootTableId,
                        principalTable: "loot_table",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "loot_table_entry",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(10) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    lootTableId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    type = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    itemId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    minAmount = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 1u),
                    maxAmount = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 1u),
                    chance = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 1000000u),
                    groupId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    source = table.Column<string>(type: "varchar(500)", nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sourceVersion = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    observedDrops = table.Column<uint>(type: "int(10) unsigned", nullable: true),
                    observedAttempts = table.Column<uint>(type: "int(10) unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.CheckConstraint("CK_loot_table_entry_amount", "`minAmount` > 0 AND `maxAmount` >= `minAmount`");
                    table.CheckConstraint("CK_loot_table_entry_chance", "`chance` <= 1000000");
                    table.ForeignKey(
                        name: "FK_loot_table_entry_loot_table",
                        column: x => x.lootTableId,
                        principalTable: "loot_table",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_creature_loot_lootTableId",
                table: "creature_loot",
                column: "lootTableId");

            migrationBuilder.CreateIndex(
                name: "IX_loot_table_entry_lootTableId",
                table: "loot_table_entry",
                column: "lootTableId");

            migrationBuilder.InsertData(
                table: "loot_table",
                columns: new[] { "id", "description" },
                values: new object[,]
                {
                    { 1u, "Default creature currency" },
                    { 19349u, "Whistlewind Skulker observed drops" }
                });

            migrationBuilder.InsertData(
                table: "creature_loot",
                columns: new[] { "creatureId", "lootTableId" },
                values: new object[,]
                {
                    { 0u, 1u },
                    { 19349u, 19349u }
                });

            migrationBuilder.InsertData(
                table: "loot_table_entry",
                columns: new[]
                {
                    "id", "lootTableId", "type", "itemId", "minAmount", "maxAmount", "chance", "groupId",
                    "source", "sourceVersion", "observedDrops", "observedAttempts"
                },
                values: new object[,]
                {
                    {
                        1u, 1u, (byte)2, 1u, 5u, 15u, 1000000u, 0u,
                        "https://github.com/Bezgelor/bezgelor/blob/52ea15500a5fcba71a5ad1b566f8ad0e9e0b0083/apps/bezgelor_data/priv/data/loot_tables.json",
                        "community-emulator approximation", null, null
                    },
                    { 2u, 19349u, (byte)0, 7523u, 1u, 1u, 441758u, 0u, "https://www.jabbithole.com/npcs/whistlewind-skulker-16", "launch", 2503u, 5666u },
                    { 3u, 19349u, (byte)0, 14279u, 1u, 1u, 125309u, 0u, "https://www.jabbithole.com/npcs/whistlewind-skulker-16", "launch", 710u, 5666u },
                    { 4u, 19349u, (byte)0, 14235u, 1u, 1u, 68126u, 0u, "https://www.jabbithole.com/npcs/whistlewind-skulker-16", "launch", 386u, 5666u },
                    { 5u, 19349u, (byte)0, 14236u, 1u, 1u, 59125u, 0u, "https://www.jabbithole.com/npcs/whistlewind-skulker-16", "launch", 335u, 5666u },
                    { 6u, 19349u, (byte)0, 42996u, 1u, 1u, 24356u, 0u, "https://www.jabbithole.com/npcs/whistlewind-skulker-16", "launch", 138u, 5666u },
                    { 7u, 19349u, (byte)0, 42995u, 1u, 1u, 18002u, 0u, "https://www.jabbithole.com/npcs/whistlewind-skulker-16", "launch", 102u, 5666u },
                    { 8u, 19349u, (byte)0, 14247u, 1u, 1u, 3883u, 0u, "https://www.jabbithole.com/npcs/whistlewind-skulker-16", "launch", 22u, 5666u },
                    { 9u, 19349u, (byte)0, 14246u, 1u, 1u, 3706u, 0u, "https://www.jabbithole.com/npcs/whistlewind-skulker-16", "launch", 21u, 5666u }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "creature_loot");

            migrationBuilder.DropTable(
                name: "loot_table_entry");

            migrationBuilder.DropTable(
                name: "loot_table");
        }
    }
}
