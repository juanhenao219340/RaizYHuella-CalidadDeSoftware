using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veterinaria.Infrastructure.Persistence.Migrations
{

    public partial class AddAppointmentHistory : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentHistoryEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PreviousDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PreviousStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    PreviousServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NewStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    NewServiceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentHistoryEntries", x => x.Id);
                    table.CheckConstraint("CK_AppointmentHistoryEntries_Action", "\"Action\" IN ('Rescheduled', 'Cancelled')");
                    table.CheckConstraint("CK_AppointmentHistoryEntries_NewValues", "(\"Action\" = 'Rescheduled' AND \"NewDate\" IS NOT NULL AND \"NewStartTime\" IS NOT NULL AND \"NewServiceId\" IS NOT NULL)\n                OR (\"Action\" = 'Cancelled' AND \"NewDate\" IS NULL AND \"NewStartTime\" IS NULL AND \"NewServiceId\" IS NULL)");
                    table.CheckConstraint("CK_AppointmentHistoryEntries_PreviousStartTime", "\"PreviousStartTime\" BETWEEN '08:00' AND '17:00' AND EXTRACT(MINUTE FROM \"PreviousStartTime\") = 0 AND EXTRACT(SECOND FROM \"PreviousStartTime\") = 0");
                    table.CheckConstraint("CK_AppointmentHistoryEntries_Reason", "LENGTH(BTRIM(\"Reason\")) > 0");
                    table.ForeignKey(
                        name: "FK_AppointmentHistoryEntries_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentHistoryEntries_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentHistoryEntries_Services_NewServiceId",
                        column: x => x.NewServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentHistoryEntries_Services_PreviousServiceId",
                        column: x => x.PreviousServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentHistoryEntries_AppointmentId_ChangedAt",
                table: "AppointmentHistoryEntries",
                columns: new[] { "AppointmentId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentHistoryEntries_ChangedByUserId",
                table: "AppointmentHistoryEntries",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentHistoryEntries_NewServiceId",
                table: "AppointmentHistoryEntries",
                column: "NewServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentHistoryEntries_PreviousServiceId",
                table: "AppointmentHistoryEntries",
                column: "PreviousServiceId");
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentHistoryEntries");
        }
    }
}
