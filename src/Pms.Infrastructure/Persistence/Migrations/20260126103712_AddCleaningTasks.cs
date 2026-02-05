using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCleaningTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CleaningTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TaskType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedToStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CleaningTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CleaningTasks_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CleaningTasks_StaffProfiles_AssignedToStaffId",
                        column: x => x.AssignedToStaffId,
                        principalTable: "StaffProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CleaningTasks_AssignedToStaffId",
                table: "CleaningTasks",
                column: "AssignedToStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningTasks_RoomId",
                table: "CleaningTasks",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningTasks_ScheduledDate_Status",
                table: "CleaningTasks",
                columns: new[] { "ScheduledDate", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CleaningTasks");
        }
    }
}
