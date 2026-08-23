using Microsoft.EntityFrameworkCore;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;

namespace Modules.Employees.Application.Abstractions;

public interface IEmployeeDbContext
{
    DbSet<EmployeeGroup> EmployeeGroups { get; }

    DbSet<WorkSchedule> WorkSchedules { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}