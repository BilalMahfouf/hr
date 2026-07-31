using Microsoft.EntityFrameworkCore;
using Modules.Identity.Abstracions;
using Modules.Identity.Domain.Users;
using Modules.Identity.Infrastructure.Persistence.Configurations;
using Modules.Shared.Infrastructure.Outbox;

namespace Modules.Identity.Infrastructure.Persistence;

public class IdentityDbContext : DbContext, IIdentityApplicationDbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;

    public DbSet<UserSession> UserSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.ConfigureOutboxMessage(excludeFromMigrations: true);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
    }
}
