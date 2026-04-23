using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Migrations.Brand
{
    /// <inheritdoc />
    public partial class UpdateOrderStatusTransitionCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderStatusTransitions_OrderStatuses_FromStatusId",
                table: "OrderStatusTransitions");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderStatusTransitions_OrderStatuses_ToStatusId",
                table: "OrderStatusTransitions");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderStatusTransitions_OrderStatuses_FromStatusId",
                table: "OrderStatusTransitions",
                column: "FromStatusId",
                principalTable: "OrderStatuses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderStatusTransitions_OrderStatuses_ToStatusId",
                table: "OrderStatusTransitions",
                column: "ToStatusId",
                principalTable: "OrderStatuses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderStatusTransitions_OrderStatuses_FromStatusId",
                table: "OrderStatusTransitions");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderStatusTransitions_OrderStatuses_ToStatusId",
                table: "OrderStatusTransitions");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderStatusTransitions_OrderStatuses_FromStatusId",
                table: "OrderStatusTransitions",
                column: "FromStatusId",
                principalTable: "OrderStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderStatusTransitions_OrderStatuses_ToStatusId",
                table: "OrderStatusTransitions",
                column: "ToStatusId",
                principalTable: "OrderStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
