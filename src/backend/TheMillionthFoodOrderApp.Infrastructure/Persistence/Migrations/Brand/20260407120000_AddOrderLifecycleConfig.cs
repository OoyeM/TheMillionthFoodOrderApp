using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Migrations.Brand
{
    /// <inheritdoc />
    public partial class AddOrderLifecycleConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderLifecycleConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLifecycleConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderLifecycleConfigId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SystemKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsTerminal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ColorHex = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatuses_OrderLifecycleConfigs_OrderLifecycleConfigId",
                        column: x => x.OrderLifecycleConfigId,
                        principalTable: "OrderLifecycleConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderStatusTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderLifecycleConfigId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatusTransitions_OrderLifecycleConfigs_OrderLifecycleConfigId",
                        column: x => x.OrderLifecycleConfigId,
                        principalTable: "OrderLifecycleConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderStatusTransitions_OrderStatuses_FromStatusId",
                        column: x => x.FromStatusId,
                        principalTable: "OrderStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderStatusTransitions_OrderStatuses_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "OrderStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLifecycleConfigs_ShopId",
                table: "OrderLifecycleConfigs",
                column: "ShopId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatuses_ConfigId_SortOrder",
                table: "OrderStatuses",
                columns: new[] { "OrderLifecycleConfigId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusTransitions_ConfigId_From_To",
                table: "OrderStatusTransitions",
                columns: new[] { "OrderLifecycleConfigId", "FromStatusId", "ToStatusId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusTransitions_FromStatusId",
                table: "OrderStatusTransitions",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusTransitions_ToStatusId",
                table: "OrderStatusTransitions",
                column: "ToStatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderStatusTransitions");

            migrationBuilder.DropTable(
                name: "OrderStatuses");

            migrationBuilder.DropTable(
                name: "OrderLifecycleConfigs");
        }
    }
}
