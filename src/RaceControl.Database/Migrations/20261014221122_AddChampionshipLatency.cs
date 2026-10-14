using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceControl.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddChampionshipLatency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "latency",
                table: "championships",
                type: "integer",
                nullable: false,
                defaultValue: 35);
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
