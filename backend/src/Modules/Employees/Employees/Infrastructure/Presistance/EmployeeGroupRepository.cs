using Microsoft.EntityFrameworkCore;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;

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
            .FirstOrDefaultAsync(g => g.Id == id, ct);
    }

    public async Task<EmployeeGroup?> GetByIdWithDetailsAsync(EmployeeGroupId id, CancellationToken ct = default)
    {
        return await _dbContext.EmployeeGroups
            .Include(g => g.WorkSchedules)
            .Include(g => g.RotationEntries)
            .FirstOrDefaultAsync(g => g.Id == id, ct);
    }

    public async Task<EmployeeGroup?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _dbContext.EmployeeGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Name.ToLower() == name.ToLower(), ct);
    }

    public async Task<IReadOnlyList<EmployeeGroup>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbContext.EmployeeGroups
            .AsNoTracking()
            .Include(g => g.WorkSchedules)
            .Include(g => g.RotationEntries)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);
    }

    public void Add(EmployeeGroup group)
    {
        _dbContext.EmployeeGroups.Add(group);
    }

    public void Remove(EmployeeGroup group)
    {
        _dbContext.EmployeeGroups.Remove(group);
    }
}