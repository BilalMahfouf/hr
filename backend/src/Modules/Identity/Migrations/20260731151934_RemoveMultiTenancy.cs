using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Identity.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_tenant_id",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_tenant_id_email",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_tenant_id_user_name",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_user_sessions_tenant_id",
                schema: "identity",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "identity",
                table: "user_sessions");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                schema: "identity",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_user_name",
                schema: "identity",
                table: "users",
                column: "user_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_user_name",
                schema: "identity",
                table: "users");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "user_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id",
                schema: "identity",
                table: "users",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id_email",
                schema: "identity",
                table: "users",
                columns: new[] { "tenant_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id_user_name",
                schema: "identity",
                table: "users",
                columns: new[] { "tenant_id", "user_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_tenant_id",
                schema: "identity",
                table: "user_sessions",
                column: "tenant_id");
        }
    }
}
