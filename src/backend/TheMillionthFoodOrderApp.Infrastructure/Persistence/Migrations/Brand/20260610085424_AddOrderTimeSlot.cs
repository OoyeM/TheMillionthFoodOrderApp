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
            migrationBuilder.AddColumn<string>(
                name: "TimeSlot",
                table: "Orders",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeSlotStart",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShopId_TimeSlotStart",
                table: "Orders",
                columns: new[] { "ShopId", "TimeSlotStart" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_ShopId_TimeSlotStart",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TimeSlot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TimeSlotStart",
                table: "Orders");
        }
    }
}
