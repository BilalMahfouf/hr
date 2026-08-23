using Microsoft.EntityFrameworkCore;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.Infrastructure.Outbox;

namespace Modules.Employees.Infrastructure.Presistance;

public sealed class EmployeeDbContext : DbContext, IEmployeeDbContext
{
    public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options) : base(options)
    {
    }

    public DbSet<EmployeeGroup> EmployeeGroups { get; set; } = null!;

    public DbSet<WorkSchedule> WorkSchedules { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("employees");
        modelBuilder.ConfigureOutboxMessage(excludeFromMigrations: true);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmployeeGroupConfiguration).Assembly);
    }
}