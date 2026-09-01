using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Employees.Migrations
{
    /// <inheritdoc />
    public partial class RenameGroupNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "employee_group_number",
                schema: "employees",
                table: "employee_groups",
                newName: "group_number");

            migrationBuilder.RenameIndex(
                name: "ix_employee_groups_employee_group_number",
                schema: "employees",
                table: "employee_groups",
                newName: "ix_employee_groups_group_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "group_number",
                schema: "employees",
                table: "employee_groups",
                newName: "employee_group_number");

            migrationBuilder.RenameIndex(
                name: "ix_employee_groups_group_number",
                schema: "employees",
                table: "employee_groups",
                newName: "ix_employee_groups_employee_group_number");
        }
    }
}
