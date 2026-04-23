using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Migrations.Brand
{
    /// <inheritdoc />
    public partial class AddTaxConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guard: this index only exists on databases created before AddComboProducts
            // added compound indexes directly. Fresh databases never have it.
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_ComboItems_ComboProductId'
                      AND object_id = OBJECT_ID('ComboItems')
                )
                    DROP INDEX [IX_ComboItems_ComboProductId] ON [ComboItems];
                """);

            migrationBuilder.CreateTable(
                name: "TaxConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VatRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsumptionMode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RatePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VatRates_TaxConfigurations_TaxConfigurationId",
                        column: x => x.TaxConfigurationId,
                        principalTable: "TaxConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VatRates_TaxConfigurationId_ConsumptionMode",
                table: "VatRates",
                columns: new[] { "TaxConfigurationId", "ConsumptionMode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VatRates");

            migrationBuilder.DropTable(
                name: "TaxConfigurations");

        }
    }
}
