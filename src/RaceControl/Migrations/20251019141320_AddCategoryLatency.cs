using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceControl.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryLatency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "latency",
                table: "category",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "latency",
                table: "category");
        }
    }
}
