using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceControl.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddNewLatencyColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "latency",
                table: "championships",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 35, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "latency",
                table: "championships");
        }
    }
}
