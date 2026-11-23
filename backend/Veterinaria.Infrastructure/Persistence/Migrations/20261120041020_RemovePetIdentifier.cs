using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veterinaria.Infrastructure.Persistence.Migrations
{

    public partial class RemovePetIdentifier : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pets_Identifier",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "Identifier",
                table: "Pets");
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Identifier",
                table: "Pets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Pets_Identifier",
                table: "Pets",
                column: "Identifier",
                unique: true);
        }
    }
}
