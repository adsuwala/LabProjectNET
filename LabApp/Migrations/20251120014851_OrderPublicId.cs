using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabApp.Migrations
{
    /// <inheritdoc />
    public partial class OrderPublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE Orders
                SET PublicId = CONCAT('ORD-', RIGHT(CONVERT(varchar(36), NEWID()), 10))
                WHERE PublicId = '' OR PublicId IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Orders");
        }
    }
}
