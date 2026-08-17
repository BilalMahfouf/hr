using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Attendence.Domain.Punches;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence.Application.Shared; 

public interface IAttendanceDbContext
{
    DbSet<Punch> Punches { get; }

    DbSet<AttendanceRecord> AttendanceRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
