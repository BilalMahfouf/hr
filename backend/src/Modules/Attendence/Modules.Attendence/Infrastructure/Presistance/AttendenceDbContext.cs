using System;
using System.Collections.Generic;
using System.Text;
using global::Modules.Attendence.Application.Shared;
using global::Modules.Attendence.Domain.AttendenceRecords;
using global::Modules.Attendence.Domain.Punches;
using global::Modules.Shared.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;


namespace Modules.Attendence.Infrastructure.Presistance;

public sealed class AttendanceDbContext
    : DbContext, IAttendanceDbContext
{
    public AttendanceDbContext(
        DbContextOptions<AttendanceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Punch> Punches { get; set; } = null!;

    public DbSet<AttendanceRecord> AttendanceRecords { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("attendance");

        modelBuilder.ConfigureOutboxMessage(
            excludeFromMigrations: true);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PunchConfiguration).Assembly);
    }
}
