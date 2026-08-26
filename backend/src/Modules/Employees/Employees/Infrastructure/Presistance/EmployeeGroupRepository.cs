using Microsoft.EntityFrameworkCore;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;

namespace Modules.Employees.Infrastructure.Presistance;

public sealed class EmployeeGroupRepository : IEmployeeGroupRepository
{
    private readonly EmployeeDbContext _dbContext;

    public EmployeeGroupRepository(EmployeeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EmployeeGroup?> GetByIdAsync(EmployeeGroupId id, CancellationToken ct = default)
    {
        return await _dbContext.EmployeeGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, ct);
    }

    public async Task<EmployeeGroup?> GetByIdWithDetailsAsync(EmployeeGroupId id, CancellationToken ct = default)
    {
        return await _dbContext.EmployeeGroups
            .Include(g => g.WorkSchedules)
            .Include(g => g.RotationEntries)
                .ThenInclude(re => re.WorkSchedule)
            .FirstOrDefaultAsync(g => g.Id == id, ct);
    }

    public async Task<IReadOnlyList<EmployeeGroup>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbContext.EmployeeGroups
            .AsNoTracking()
            .Include(g => g.WorkSchedules)
            .Include(g => g.RotationEntries)
                .ThenInclude(re => re.WorkSchedule)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
    {
        return await _dbContext.EmployeeGroups
            .AsNoTracking()
            .AnyAsync(g => g.Name.ToLower() == name.ToLower(), ct);
    }

    public void Add(EmployeeGroup group)
    {
        _dbContext.EmployeeGroups.Add(group);
    }

    public void Remove(EmployeeGroup group)
    {
        _dbContext.EmployeeGroups.Remove(group);
    }

    public async Task<WorkSchedule?> GetWorkScheduleByIdAsync(WorkScheduleId id, CancellationToken ct = default)
    {
        return await _dbContext.WorkSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(ws => ws.Id == id, ct);
    }

    public async Task<IReadOnlyList<WorkSchedule>> GetWorkSchedulesByGroupIdAsync(EmployeeGroupId groupId, CancellationToken ct = default)
    {
        return await _dbContext.WorkSchedules
            .AsNoTracking()
            .Where(ws => ws.EmployeeGroupId == groupId)
            .ToListAsync(ct);
    }

    public void AddWorkSchedule(WorkSchedule schedule)
    {
        _dbContext.WorkSchedules.Add(schedule);
    }

    public void RemoveWorkSchedule(WorkSchedule schedule)
    {
        _dbContext.WorkSchedules.Remove(schedule);
    }

    public async Task<RotationEntry?> GetRotationEntryByIdAsync(RotationEntryId id, CancellationToken ct = default)
    {
        return await _dbContext.RotationEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(re => re.Id == id, ct);
    }

    public async Task<IReadOnlyList<RotationEntry>> GetRotationEntriesByGroupIdAsync(EmployeeGroupId groupId, CancellationToken ct = default)
    {
        return await _dbContext.RotationEntries
            .AsNoTracking()
            .Where(re => re.EmployeeGroupId == groupId)
            .OrderBy(re => re.Position)
            .ToListAsync(ct);
    }

    public async Task<RotationEntry?> GetRotationEntryByPositionAsync(EmployeeGroupId groupId, int position, CancellationToken ct = default)
    {
        return await _dbContext.RotationEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(re => re.EmployeeGroupId == groupId && re.Position == position, ct);
    }

    public void AddRotationEntry(RotationEntry entry)
    {
        _dbContext.RotationEntries.Add(entry);
    }

    public void RemoveRotationEntry(RotationEntry entry)
    {
        _dbContext.RotationEntries.Remove(entry);
    }
}