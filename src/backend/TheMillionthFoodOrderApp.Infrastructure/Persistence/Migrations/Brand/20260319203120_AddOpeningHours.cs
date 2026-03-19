using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Migrations.Brand
{
    /// <inheritdoc />
    public partial class AddOpeningHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Shops",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Europe/Brussels");

            migrationBuilder.CreateTable(
                name: "OpeningHoursTimeBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time(7)", nullable: false),
                    CloseTime = table.Column<TimeOnly>(type: "time(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpeningHoursTimeBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpeningHoursTimeBlocks_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpeningHoursTimeBlocks_ShopId_DayOfWeek",
                table: "OpeningHoursTimeBlocks",
                columns: new[] { "ShopId", "DayOfWeek" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpeningHoursTimeBlocks");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Shops");
        }
    }
}
