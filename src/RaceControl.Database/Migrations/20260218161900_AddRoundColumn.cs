using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceControl.Migrations
{
    /// <inheritdoc />
    public partial class AddRoundColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "round",
                table: "session",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "round",
                table: "session");
        }
    }
}
