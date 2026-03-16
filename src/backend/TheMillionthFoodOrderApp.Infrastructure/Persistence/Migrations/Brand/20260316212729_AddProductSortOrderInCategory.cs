using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Migrations.Brand
{
    /// <inheritdoc />
    public partial class AddProductSortOrderInCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_MenuCategoryId",
                table: "Products");

            migrationBuilder.AddColumn<int>(
                name: "SortOrderInCategory",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Products_MenuCategoryId_SortOrderInCategory",
                table: "Products",
                columns: new[] { "MenuCategoryId", "SortOrderInCategory" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_MenuCategoryId_SortOrderInCategory",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SortOrderInCategory",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_MenuCategoryId",
                table: "Products",
                column: "MenuCategoryId");
        }
    }
}
