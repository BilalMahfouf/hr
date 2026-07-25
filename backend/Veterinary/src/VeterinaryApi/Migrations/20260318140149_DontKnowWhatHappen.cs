using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeterinaryApi.Migrations
{
    /// <inheritdoc />
    public partial class DontKnowWhatHappen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_clinics_doctor_id",
                table: "clinics");

            migrationBuilder.CreateIndex(
                name: "IX_clinics_doctor_id",
                table: "clinics",
                column: "doctor_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_clinics_doctor_id",
                table: "clinics");

            migrationBuilder.CreateIndex(
                name: "IX_clinics_doctor_id",
                table: "clinics",
                column: "doctor_id");
        }
    }
}
