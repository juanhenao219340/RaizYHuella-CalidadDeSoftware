using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veterinaria.Infrastructure.Persistence.Migrations
{

    public partial class AddSoftDelete : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Services",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Pets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AspNetUsers");
        }
    }
}
