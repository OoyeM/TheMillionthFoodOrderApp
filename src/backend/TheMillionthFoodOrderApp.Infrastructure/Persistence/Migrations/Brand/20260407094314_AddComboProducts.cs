using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Migrations.Brand
{
    /// <inheritdoc />
    public partial class AddComboProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductType",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ComboItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComboProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComboItems_Products_ComboProductId",
                        column: x => x.ComboProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComboItems_Products_ComponentProductId",
                        column: x => x.ComponentProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComboItems_ComboProductId_ComponentProductId",
                table: "ComboItems",
                columns: new[] { "ComboProductId", "ComponentProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComboItems_ComboProductId_SortOrder",
                table: "ComboItems",
                columns: new[] { "ComboProductId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ComboItems_ComponentProductId",
                table: "ComboItems",
                column: "ComponentProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComboItems");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "Products");
        }
    }
}
