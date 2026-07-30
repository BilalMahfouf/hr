using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicApi.Migrations
{
    /// <inheritdoc />
    public partial class InitVaccinations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vaccinations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    animal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    given_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vaccinations", x => x.id);
                    table.ForeignKey(
                        name: "fk_vaccination_animal",
                        column: x => x.animal_id,
                        principalTable: "animals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vaccination_visit",
                        column: x => x.visit_id,
                        principalTable: "visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vaccination_animal_id",
                table: "vaccinations",
                column: "animal_id");

            migrationBuilder.CreateIndex(
                name: "ix_vaccination_given_at",
                table: "vaccinations",
                column: "given_at");

            migrationBuilder.CreateIndex(
                name: "ix_vaccination_is_deleted",
                table: "vaccinations",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_vaccination_tenant_id",
                table: "vaccinations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_vaccination_tenant_is_deleted",
                table: "vaccinations",
                columns: new[] { "tenant_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_vaccination_visit_id",
                table: "vaccinations",
                column: "visit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vaccinations");
        }
    }
}
