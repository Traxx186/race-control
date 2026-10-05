using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RaceControl.Database.Migrations
{
    /// <inheritdoc />
    public partial class CreateSessionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    key = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cancelled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    championship_id = table.Column<string>(type: "character varying(32)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("id_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_sessions_championship",
                        column: x => x.championship_id,
                        principalTable: "championships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sessions_championship_id",
                table: "sessions",
                column: "championship_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sessions");
        }
    }
}
