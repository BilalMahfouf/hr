using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Domain.Punches;
using Modules.Attendence.Domain.PunchPolling;

namespace Modules.Attendence.Application.Shared; 

public interface IAttendanceDbContext
{
    DbSet<Punch> Punches { get; }

    DbSet<AttendanceRecord> AttendanceRecords { get; }

    DbSet<AttendenceMachine> Machines { get; }

    DbSet<PunchPollingSettings> PunchPollingSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
