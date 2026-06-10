using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Migrations.Brand
{
    /// <inheritdoc />
    public partial class AddOrderTimeSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeSlotEnd",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeSlotStart",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShopId_TimeSlotStart",
                table: "Orders",
                columns: new[] { "ShopId", "TimeSlotStart" },
                filter: "[TimeSlotStart] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_ShopId_TimeSlotStart",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TimeSlotEnd",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TimeSlotStart",
                table: "Orders");
        }
    }
}
