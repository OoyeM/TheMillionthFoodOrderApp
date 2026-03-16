using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Migrations.Platform
{
    /// <inheritdoc />
    public partial class RenameEntraObjectIdToExternalIdentityId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EntraObjectId",
                schema: "platform",
                table: "PlatformUsers",
                newName: "ExternalIdentityId");

            migrationBuilder.RenameIndex(
                name: "IX_PlatformUsers_EntraObjectId",
                schema: "platform",
                table: "PlatformUsers",
                newName: "IX_PlatformUsers_ExternalIdentityId");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalIdentityId",
                schema: "platform",
                table: "PlatformUsers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(36)",
                oldMaxLength: 36);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // WARNING: Narrows nvarchar(128) to nvarchar(36). If any values exceed 36 chars,
            // this will fail with truncation error. Use a new forward migration instead.
            migrationBuilder.AlterColumn<string>(
                name: "ExternalIdentityId",
                schema: "platform",
                table: "PlatformUsers",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.RenameColumn(
                name: "ExternalIdentityId",
                schema: "platform",
                table: "PlatformUsers",
                newName: "EntraObjectId");

            migrationBuilder.RenameIndex(
                name: "IX_PlatformUsers_ExternalIdentityId",
                schema: "platform",
                table: "PlatformUsers",
                newName: "IX_PlatformUsers_EntraObjectId");
        }
    }
}
