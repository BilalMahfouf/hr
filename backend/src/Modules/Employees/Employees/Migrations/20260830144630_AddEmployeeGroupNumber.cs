using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Employees.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeGroupNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "employee_group_number",
                schema: "employees",
                table: "employee_groups",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_employee_groups_employee_group_number",
                schema: "employees",
                table: "employee_groups",
                column: "employee_group_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_employee_groups_employee_group_number",
                schema: "employees",
                table: "employee_groups");

            migrationBuilder.DropColumn(
                name: "employee_group_number",
                schema: "employees",
                table: "employee_groups");
        }
    }
}
