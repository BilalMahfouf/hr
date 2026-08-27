using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;

namespace Modules.Employees.Application.Abstractions;

public interface IEmployeeGroupRepository
{
    Task<EmployeeGroup?> GetByIdAsync(EmployeeGroupId id, CancellationToken ct = default);
    Task<EmployeeGroup?> GetByIdWithDetailsAsync(EmployeeGroupId id, CancellationToken ct = default);
    Task<EmployeeGroup?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeGroup>> GetAllAsync(CancellationToken ct = default);
    void Add(EmployeeGroup group);
    void Remove(EmployeeGroup group);
}