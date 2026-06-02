using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Migrations.Brand
{
    /// <inheritdoc />
    public partial class AddInStoreOrderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByStaffId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TableNumber",
                table: "Orders",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByStaffId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TableNumber",
                table: "Orders");
        }
    }
}
