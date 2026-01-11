using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabApp.Migrations
{
    public partial class FillOrderPublicIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Orders
                SET PublicId = 'ORD-' + SUBSTRING(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), 1, 10)
                WHERE PublicId IS NULL OR PublicId = '';
            """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op
        }
    }
}
