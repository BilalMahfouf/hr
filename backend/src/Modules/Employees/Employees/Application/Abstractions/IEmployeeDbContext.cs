using Microsoft.EntityFrameworkCore;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;

namespace Modules.Employees.Application.Abstractions;

public interface IEmployeeDbContext
{
    DbSet<EmployeeGroup> EmployeeGroups { get; }

    DbSet<WorkSchedule> WorkSchedules { get; }

    DbSet<RotationEntry> RotationEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}