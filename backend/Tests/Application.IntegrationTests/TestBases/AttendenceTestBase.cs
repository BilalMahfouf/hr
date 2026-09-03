using Application.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modules.Employees.Application;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Employees.Infrastructure.Presistance;
using Modules.Attendence.Application.AttendenceRerords;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Infrastructure.Presistance;
using Modules.Employees.Contracts;
using Modules.Shared.Infrastructure.Outbox;

namespace Application.IntegrationTests.TestBases;

public abstract class AttendenceTestBase : IntegrationTestBase
{
    protected AttendenceTestBase(PostgresFixture fixture) : base(fixture)
    {
    }

    protected TestEmployeeRepository EmployeeRepository { get; } = new();

    protected MachineId MachineId { get; } = MachineId.New();

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await using var scope = RootProvider.CreateAsyncScope();
        var attendanceDb = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        await attendanceDb.Database.MigrateAsync();
        var employeeDb = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
        await employeeDb.Database.MigrateAsync();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<AttendanceDbContext>((sp, options) =>
        {
            options.UseNpgsql(ConnectionString)
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptors>());
        });
        services.AddScoped<IAttendanceDbContext>(sp => sp.GetRequiredService<AttendanceDbContext>());

        services.AddDbContext<EmployeeDbContext>((sp, options) =>
        {
            options.UseNpgsql(ConnectionString)
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptors>());
        });
        services.AddScoped<IEmployeeDbContext>(sp => sp.GetRequiredService<EmployeeDbContext>());

        services.AddSingleton<IEmployeeRepository>(EmployeeRepository);
        services.AddScoped<IEmployeeApi, EmployeeApi>();
    }

    protected CreateAttendenceRecord.CommandHandler CreateCreateAttendenceRecordHandler(
        IServiceProvider services)
    {
        return new CreateAttendenceRecord.CommandHandler(
            services.GetRequiredService<IEmployeeApi>(),
            services.GetRequiredService<IAttendanceDbContext>());
    }

    protected async Task SeedEmployeeAsync(int badge, string employeeId, string groupNumber)
    {
        EmployeeRepository.AddEmployee(new EmployeeDto(
            employeeId,
            badge.ToString(),
            groupNumber,
            $"Employee {badge}"));
    }

    protected async Task<EmployeeGroup> SeedSecurityGroupWWRRAsync(
        DateOnly rotationStartDate,
        string groupNumber = "SEC-01",
        int allowedLatenessMinutes = 0,
        int allowedEarlinessMinutes = 0)
    {
        await using var scope = RootProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();

        var group = EmployeeGroup.Create(
            groupNumber,
            $"Security Group {groupNumber}",
            isSecurity: true,
            rotationStartDate);

        var dayScheduleId = WorkScheduleId.New();
        var nightScheduleId = WorkScheduleId.New();

        group.ReplaceSchedulesAndRotations(
            [
                new CreateWorkScheduleDto(
                    group.Id,
                    ShiftStartTime: new TimeOnly(6, 0),
                    ShiftEndTime: new TimeOnly(18, 0),
                    EndDayOffset: 0,
                    BreakStartTime: new TimeOnly(12, 0),
                    BreakEndTime: new TimeOnly(13, 0),
                    AllowedCheckInLatenessMinutes: allowedLatenessMinutes,
                    AllowedCheckOutEarlinessMinutes: allowedEarlinessMinutes),
                new CreateWorkScheduleDto(
                    group.Id,
                    ShiftStartTime: new TimeOnly(18, 0),
                    ShiftEndTime: new TimeOnly(6, 0),
                    EndDayOffset: 1,
                    BreakStartTime: new TimeOnly(23, 0),
                    BreakEndTime: new TimeOnly(0, 0),
                    AllowedCheckInLatenessMinutes: allowedLatenessMinutes,
                    AllowedCheckOutEarlinessMinutes: allowedEarlinessMinutes)
            ],
            [
                (1, 0),  // Day 1: Work (day schedule)
                (2, 1),  // Day 2: Work (night schedule)
                (3, null), // Day 3: Rest
                (4, null)  // Day 4: Rest
            ]);

        db.EmployeeGroups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }

    protected async Task<EmployeeGroup> SeedAlternatingGroupRWRWAsync(
        DateOnly rotationStartDate,
        string groupNumber = "ALT-01",
        int allowedLatenessMinutes = 0,
        int allowedEarlinessMinutes = 0)
    {
        await using var scope = RootProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();

        var group = EmployeeGroup.Create(
            groupNumber,
            $"Alternating Group {groupNumber}",
            isSecurity: true,
            rotationStartDate);

        group.ReplaceSchedulesAndRotations(
            [
                new CreateWorkScheduleDto(
                    group.Id,
                    ShiftStartTime: new TimeOnly(6, 0),
                    ShiftEndTime: new TimeOnly(18, 0),
                    EndDayOffset: 0,
                    BreakStartTime: new TimeOnly(12, 0),
                    BreakEndTime: new TimeOnly(13, 0),
                    AllowedCheckInLatenessMinutes: allowedLatenessMinutes,
                    AllowedCheckOutEarlinessMinutes: allowedEarlinessMinutes),
                new CreateWorkScheduleDto(
                    group.Id,
                    ShiftStartTime: new TimeOnly(18, 0),
                    ShiftEndTime: new TimeOnly(6, 0),
                    EndDayOffset: 1,
                    BreakStartTime: new TimeOnly(23, 0),
                    BreakEndTime: new TimeOnly(0, 0),
                    AllowedCheckInLatenessMinutes: allowedLatenessMinutes,
                    AllowedCheckOutEarlinessMinutes: allowedEarlinessMinutes)
            ],
            [
                (1, null), // Day 1: Rest
                (2, 0),    // Day 2: Work (day schedule)
                (3, null), // Day 3: Rest
                (4, 1)     // Day 4: Work (night schedule)
            ]);

        db.EmployeeGroups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }

    protected async Task SeedPunchAsync(int employeeBadge, DateTime punchAt)
    {
        var utcPunch = punchAt.Kind == DateTimeKind.Utc ? punchAt : DateTime.SpecifyKind(punchAt, DateTimeKind.Utc);
        await using var scope = RootProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        db.Punches.Add(Modules.Attendence.Domain.Punches.Punch.Create(
            MachineId, employeeBadge, utcPunch, DateTime.UtcNow));
        await db.SaveChangesAsync();
    }
}
