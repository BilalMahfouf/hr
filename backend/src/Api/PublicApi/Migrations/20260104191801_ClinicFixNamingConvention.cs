using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicApi.Migrations
{
    /// <inheritdoc />
    public partial class ClinicFixNamingConvention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "clinics",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "DeletedOnUtc",
                table: "clinics",
                newName: "deleted_on_utc");

            migrationBuilder.AddColumn<int>(
                name: "staff_count",
                table: "clinics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_clinics_doctor_id",
                table: "clinics",
                column: "doctor_id");

            migrationBuilder.AddForeignKey(
                name: "FK_clinics_users_doctor_id",
                table: "clinics",
                column: "doctor_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_clinics_users_doctor_id",
                table: "clinics");

            migrationBuilder.DropIndex(
                name: "IX_clinics_doctor_id",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "staff_count",
                table: "clinics");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "clinics",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "deleted_on_utc",
                table: "clinics",
                newName: "DeletedOnUtc");
        }
    }
}
