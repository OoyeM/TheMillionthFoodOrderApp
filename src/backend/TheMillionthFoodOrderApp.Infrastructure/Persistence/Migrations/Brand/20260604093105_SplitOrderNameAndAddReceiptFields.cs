using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Migrations.Brand
{
    /// <inheritdoc />
    public partial class SplitOrderNameAndAddReceiptFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the split-name + receipt columns first, backfill the existing single name into
            // the first-name column, then drop the old column — so no order data is lost (US-FP-051).
            migrationBuilder.AddColumn<string>(
                name: "CustomerFirstName",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerLastName",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                table: "Orders",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "nl-BE");

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptEmailSent",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE [Orders] SET [CustomerFirstName] = [CustomerName] WHERE [CustomerName] IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // Recombine first + last back into the single name column.
            migrationBuilder.Sql(
                "UPDATE [Orders] SET [CustomerName] = NULLIF(LTRIM(RTRIM(" +
                "ISNULL([CustomerFirstName], '') + ' ' + ISNULL([CustomerLastName], ''))), '');");

            migrationBuilder.DropColumn(
                name: "CustomerFirstName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerLastName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReceiptEmailSent",
                table: "Orders");
        }
    }
}
