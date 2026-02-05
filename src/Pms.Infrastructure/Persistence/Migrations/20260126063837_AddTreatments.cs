using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TreatmentRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentRooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    BufferMinutes = table.Column<int>(type: "int", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RequiresTherapist = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentAppointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TreatmentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TreatmentRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TherapistStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SeatsUsed = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentAppointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentAppointments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreatmentAppointments_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TreatmentAppointments_StaffProfiles_TherapistStaffId",
                        column: x => x.TherapistStaffId,
                        principalTable: "StaffProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TreatmentAppointments_TreatmentRooms_TreatmentRoomId",
                        column: x => x.TreatmentRoomId,
                        principalTable: "TreatmentRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreatmentAppointments_TreatmentTypes_TreatmentTypeId",
                        column: x => x.TreatmentTypeId,
                        principalTable: "TreatmentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentAppointments_CustomerId",
                table: "TreatmentAppointments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentAppointments_ReservationId",
                table: "TreatmentAppointments",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentAppointments_Status",
                table: "TreatmentAppointments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentAppointments_TherapistStaffId_StartAtUtc_EndAtUtc",
                table: "TreatmentAppointments",
                columns: new[] { "TherapistStaffId", "StartAtUtc", "EndAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentAppointments_TreatmentRoomId_StartAtUtc_EndAtUtc",
                table: "TreatmentAppointments",
                columns: new[] { "TreatmentRoomId", "StartAtUtc", "EndAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentAppointments_TreatmentTypeId",
                table: "TreatmentAppointments",
                column: "TreatmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentRooms_IsActive",
                table: "TreatmentRooms",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentRooms_Name",
                table: "TreatmentRooms",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentTypes_IsActive",
                table: "TreatmentTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentTypes_Name",
                table: "TreatmentTypes",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TreatmentAppointments");

            migrationBuilder.DropTable(
                name: "TreatmentRooms");

            migrationBuilder.DropTable(
                name: "TreatmentTypes");
        }
    }
}
