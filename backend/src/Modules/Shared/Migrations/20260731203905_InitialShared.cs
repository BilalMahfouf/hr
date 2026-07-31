using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Shared.Migrations
{
    /// <inheritdoc />
    public partial class InitialShared : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty.
            // The shared.outbox_messages table already exists in every database: it is created
            // by the PublicApi historical migrations (OutboxMessageInit + SharedOutbox), which
            // always run before this one. From now on SharedDbContext (Modules.Shared) owns the
            // outbox schema — all future outbox changes are authored as migrations in this
            // project. This migration only records the baseline in the shared-schema
            // migrations history table and establishes the model snapshot.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty (see Up).
        }
    }
}
