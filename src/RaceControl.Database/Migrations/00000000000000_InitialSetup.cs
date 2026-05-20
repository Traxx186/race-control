using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceControl.Migrations
{
    /// <inheritdoc />
    public partial class InitialSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    priority = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("key_pkey", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "session",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    category_key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("session_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_session_category",
                        column: x => x.category_key,
                        principalTable: "category",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_session_category_key",
                table: "session",
                column: "category_key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "session");

            migrationBuilder.DropTable(
                name: "category");
        }
    }
}
