using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;

namespace Modules.Employees.Application.Abstractions;

public interface IEmployeeGroupRepository
{
    Task<EmployeeGroup?> GetByIdAsync(EmployeeGroupId id, CancellationToken ct = default);
    Task<EmployeeGroup?> GetByIdWithDetailsAsync(EmployeeGroupId id, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeGroup>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    void Add(EmployeeGroup group);
    void Remove(EmployeeGroup group);

    Task<WorkSchedule?> GetWorkScheduleByIdAsync(WorkScheduleId id, CancellationToken ct = default);
    Task<IReadOnlyList<WorkSchedule>> GetWorkSchedulesByGroupIdAsync(EmployeeGroupId groupId, CancellationToken ct = default);
    void AddWorkSchedule(WorkSchedule schedule);
    void RemoveWorkSchedule(WorkSchedule schedule);

    Task<RotationEntry?> GetRotationEntryByIdAsync(RotationEntryId id, CancellationToken ct = default);
    Task<IReadOnlyList<RotationEntry>> GetRotationEntriesByGroupIdAsync(EmployeeGroupId groupId, CancellationToken ct = default);
    Task<RotationEntry?> GetRotationEntryByPositionAsync(EmployeeGroupId groupId, int position, CancellationToken ct = default);
    void AddRotationEntry(RotationEntry entry);
    void RemoveRotationEntry(RotationEntry entry);
}