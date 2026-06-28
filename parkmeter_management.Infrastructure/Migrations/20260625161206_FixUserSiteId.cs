using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace parkmeter_management.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixUserSiteId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SiteId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SiteId",
                table: "Users",
                column: "SiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Sites_SiteId",
                table: "Users",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Sites_SiteId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SiteId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Users");
        }
    }
}
