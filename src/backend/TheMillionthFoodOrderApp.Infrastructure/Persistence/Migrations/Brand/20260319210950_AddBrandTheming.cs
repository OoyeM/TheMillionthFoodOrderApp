using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Migrations.Brand
{
    /// <inheritdoc />
    public partial class AddBrandTheming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Colors_Accent",
                table: "BrandSettings",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Colors_Primary",
                table: "BrandSettings",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Colors_Secondary",
                table: "BrandSettings",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomDomain",
                table: "BrandSettings",
                type: "nvarchar(253)",
                maxLength: 253,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "BrandSettings",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Typography_BodyFontFamily",
                table: "BrandSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Typography_HeadingFontFamily",
                table: "BrandSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Colors_Accent",
                table: "BrandSettings");

            migrationBuilder.DropColumn(
                name: "Colors_Primary",
                table: "BrandSettings");

            migrationBuilder.DropColumn(
                name: "Colors_Secondary",
                table: "BrandSettings");

            migrationBuilder.DropColumn(
                name: "CustomDomain",
                table: "BrandSettings");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "BrandSettings");

            migrationBuilder.DropColumn(
                name: "Typography_BodyFontFamily",
                table: "BrandSettings");

            migrationBuilder.DropColumn(
                name: "Typography_HeadingFontFamily",
                table: "BrandSettings");
        }
    }
}
