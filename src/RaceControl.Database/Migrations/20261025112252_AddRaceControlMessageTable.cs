using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using RaceControl.Database.Entities;

#nullable disable

namespace RaceControl.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRaceControlMessageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:message_category", "car_event,drs,flag,other,safety_car,session_status")
                .Annotation("Npgsql:Enum:message_scope", "driver,sector,track");

            migrationBuilder.CreateTable(
                name: "race_control_messages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    category = table.Column<MessageCategory>(type: "message_category", nullable: false),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    flag = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    scope = table.Column<MessageScope>(type: "message_scope", nullable: true),
                    driver_number = table.Column<int>(type: "integer", nullable: true),
                    lap = table.Column<int>(type: "integer", nullable: true),
                    sector = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("race_control_messages_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_race_control_messages_session",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_race_control_messages_session_id",
                table: "race_control_messages",
                column: "session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "race_control_messages");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:message_category", "car_event,drs,flag,other,safety_car,session_status")
                .OldAnnotation("Npgsql:Enum:message_scope", "driver,sector,track");
        }
    }
}
