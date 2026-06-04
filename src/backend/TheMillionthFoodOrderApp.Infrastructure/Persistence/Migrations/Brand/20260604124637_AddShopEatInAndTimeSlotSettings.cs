using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Migrations.Brand
{
    /// <inheritdoc />
    public partial class AddShopEatInAndTimeSlotSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EatIn_IsEnabled",
                table: "Shops",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EatIn_RequiresTableNumber",
                table: "Shops",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeSlotOrdering_Interval",
                table: "Shops",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TimeSlotOrdering_IsEnabled",
                table: "Shops",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TimeSlotOrdering_MaxOrdersPerInterval",
                table: "Shops",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EatIn_IsEnabled",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "EatIn_RequiresTableNumber",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "TimeSlotOrdering_Interval",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "TimeSlotOrdering_IsEnabled",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "TimeSlotOrdering_MaxOrdersPerInterval",
                table: "Shops");
        }
    }
}
