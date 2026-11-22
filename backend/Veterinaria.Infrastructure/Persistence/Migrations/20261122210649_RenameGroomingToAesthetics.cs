using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veterinaria.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameGroomingToAesthetics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Services_Type",
                table: "Services");

            migrationBuilder.Sql("UPDATE \"Services\" SET \"Type\" = 'Aesthetics' WHERE \"Type\" = 'Grooming';");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Services_Type",
                table: "Services",
                sql: "\"Type\" IN ('Clinic', 'Aesthetics', 'Recreation')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Services_Type",
                table: "Services");

            migrationBuilder.Sql("UPDATE \"Services\" SET \"Type\" = 'Grooming' WHERE \"Type\" = 'Aesthetics';");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Services_Type",
                table: "Services",
                sql: "\"Type\" IN ('Clinic', 'Grooming', 'Recreation')");
        }
    }
}
