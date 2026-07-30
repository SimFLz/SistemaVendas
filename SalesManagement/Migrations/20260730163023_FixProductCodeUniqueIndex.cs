using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesManagement.Migrations
{
    /// <inheritdoc />
    public partial class FixProductCodeUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Code",
                table: "Products");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "100000.U2FsZXNVUFNlZWRTYWx0IQ==.CmffvGW4uivI56zsXPb26IjZJYRiIwezvNC6MvEQJwo=");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code_UserId",
                table: "Products",
                columns: new[] { "Code", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Code_UserId",
                table: "Products");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code",
                table: "Products",
                column: "Code",
                unique: true);
        }
    }
}
