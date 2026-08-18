using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Domain.Punches;

namespace Modules.Attendence.Application.Shared; 

public interface IAttendanceDbContext
{
    DbSet<Punch> Punches { get; }

    DbSet<AttendanceRecord> AttendanceRecords { get; }

    DbSet<AttendenceMachine> Machines { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
