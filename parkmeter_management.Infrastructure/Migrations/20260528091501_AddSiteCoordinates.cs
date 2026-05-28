using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace parkmeter_management.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Sites",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Sites",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Sites");
        }
    }
}
