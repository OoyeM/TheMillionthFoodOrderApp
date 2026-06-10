using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Migrations.Brand
{
    /// <inheritdoc />
    public partial class RemoveShopOwnedBoolDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "TimeSlotOrdering_IsEnabled",
                table: "Shops",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "EatIn_RequiresTableNumber",
                table: "Shops",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "EatIn_IsEnabled",
                table: "Shops",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            // Heal rows corrupted by the sentinel bug this migration fixes: "enabled with a null
            // interval" is unrepresentable in the domain (TimeSlotOrderingSettings factories) and
            // can only result from the disable-toggle update having omitted the IsEnabled column.
            migrationBuilder.Sql(
                "UPDATE Shops SET TimeSlotOrdering_IsEnabled = 0 " +
                "WHERE TimeSlotOrdering_IsEnabled = 1 AND TimeSlotOrdering_Interval IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "TimeSlotOrdering_IsEnabled",
                table: "Shops",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "EatIn_RequiresTableNumber",
                table: "Shops",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "EatIn_IsEnabled",
                table: "Shops",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");
        }
    }
}
