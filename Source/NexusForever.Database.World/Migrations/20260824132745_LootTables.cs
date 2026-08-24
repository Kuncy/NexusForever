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

            // Schema only. Loot content lives in the NexusForever.WorldDatabase repository under Loot/, alongside
            // the rest of the world data, so it can grow zone by zone without a schema migration per change.
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
