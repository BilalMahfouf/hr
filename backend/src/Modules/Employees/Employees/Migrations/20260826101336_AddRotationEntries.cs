using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Employees.Migrations
{
    /// <inheritdoc />
    public partial class AddRotationEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "number_of_rotations",
                schema: "employees",
                table: "employee_groups");

            migrationBuilder.AddColumn<DateOnly>(
                name: "rotation_start_date",
                schema: "employees",
                table: "employee_groups",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateTable(
                name: "rotation_entries",
                schema: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    work_schedule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rotation_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_rotation_entries_employee_groups_employee_group_id",
                        column: x => x.employee_group_id,
                        principalSchema: "employees",
                        principalTable: "employee_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rotation_entries_work_schedules_work_schedule_id",
                        column: x => x.work_schedule_id,
                        principalSchema: "employees",
                        principalTable: "work_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_rotation_entries_employee_group_id_position",
                schema: "employees",
                table: "rotation_entries",
                columns: new[] { "employee_group_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rotation_entries_work_schedule_id",
                schema: "employees",
                table: "rotation_entries",
                column: "work_schedule_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rotation_entries",
                schema: "employees");

            migrationBuilder.DropColumn(
                name: "rotation_start_date",
                schema: "employees",
                table: "employee_groups");

            migrationBuilder.AddColumn<byte>(
                name: "number_of_rotations",
                schema: "employees",
                table: "employee_groups",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);
        }
    }
}
