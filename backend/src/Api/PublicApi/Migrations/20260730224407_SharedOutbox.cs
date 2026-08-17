using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicApi.Migrations
{
    /// <inheritdoc />
    public partial class SharedOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_processed_on_utc",
                table: "outbox_messages");

            migrationBuilder.EnsureSchema(
                name: "shared");

            migrationBuilder.RenameTable(
                name: "outbox_messages",
                newName: "outbox_messages",
                newSchema: "shared");

            migrationBuilder.RenameColumn(
                name: "errors",
                schema: "shared",
                table: "outbox_messages",
                newName: "last_error");

            migrationBuilder.Sql(
                "ALTER TABLE shared.outbox_messages ALTER COLUMN content TYPE jsonb USING content::jsonb;");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_attempt_on_utc",
                schema: "shared",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "retry_count",
                schema: "shared",
                table: "outbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_on_utc",
                schema: "shared",
                table: "outbox_messages",
                column: "processed_on_utc",
                filter: "\"processed_on_utc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_processed_on_utc",
                schema: "shared",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "last_attempt_on_utc",
                schema: "shared",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "retry_count",
                schema: "shared",
                table: "outbox_messages");

            migrationBuilder.RenameTable(
                name: "outbox_messages",
                schema: "shared",
                newName: "outbox_messages");

            migrationBuilder.RenameColumn(
                name: "last_error",
                table: "outbox_messages",
                newName: "errors");

            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "outbox_messages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_on_utc",
                table: "outbox_messages",
                column: "processed_on_utc",
                filter: "processed_on_utc IS NULL");
        }
    }
}
