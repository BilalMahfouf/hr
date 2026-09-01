using Microsoft.EntityFrameworkCore;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Contracts;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.Results;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Modules.Employees.Application;

public sealed class EmployeeApi(
    IEmployeeRepository employeeRepo,
    IEmployeeDbContext dbContext) : IEmployeeApi
{
    public async Task<Result<EmployeeResponse>> GetEmployeeByBadgeAsync(int badge, DateOnly punchDate, CancellationToken ct = default)
    {
        var employee = await employeeRepo.GetEmployeeByBgdeAsync(badge.ToString(), ct);
        if (employee is null)
        {
            return Result<EmployeeResponse>.Failure(EmployeeErrors.NotFound);
        }

        return Result<EmployeeResponse>.Success(await MapToResponse(employee, punchDate));
    }

    public async Task<Result<EmployeeResponse>> GetEmployeeByIdAsync(string id, CancellationToken ct = default)
    {
        var employee = await employeeRepo.GetEmployeeByIdAsync(id, ct);
        if (employee is null)
        {
            return Result<EmployeeResponse>.Failure(EmployeeErrors.NotFound);
        }

        return Result<EmployeeResponse>.Success(await MapToResponse(employee, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    public async Task<Result<IReadOnlyList<EmployeeResponse>>> GetEmployeesByBadgesAsync(
        IReadOnlyCollection<int> badges,
        CancellationToken ct = default)
    {
        if (badges is null || badges.Count == 0)
        {
            return Result<IReadOnlyList<EmployeeResponse>>.Success([]);
        }
        var stringBadges = badges.Select(b => b.ToString()).ToList();

        var employees = await employeeRepo.GetEmployeesByBgdesAsync(stringBadges, ct);
        var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var responses = new List<EmployeeResponse>();
        foreach (var e in employees)
        {
            responses.Add(await MapToResponse(e, currentDate));
        }
        return Result<IReadOnlyList<EmployeeResponse>>.Success(responses);
    }

    public async Task<Result<IReadOnlyList<EmployeeResponse>>> GetEmployeesByIdsAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken ct = default)
    {
        if (ids is null || ids.Count == 0)
        {
            return Result<IReadOnlyList<EmployeeResponse>>.Success([]);
        }

        var employees = await employeeRepo.GetEmployeesByIdsAsync(ids, ct);
        var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var responses = new List<EmployeeResponse>();
        foreach (var e in employees)
        {
            responses.Add(await MapToResponse(e, currentDate));
        }
        return Result<IReadOnlyList<EmployeeResponse>>.Success(responses);
    }

    private async Task<EmployeeResponse> MapToResponse(EmployeeDto employee, DateOnly punchDate)
    {
        var badge = int.TryParse(employee.Bdge, out int parsedBadge) ? parsedBadge : 0;

        if (string.IsNullOrWhiteSpace(employee.EmployeeGroup))
        {
            return new EmployeeResponse(
                employee.EmployeeId,
                badge,
                employee.FullName,
                null);
        }

        var group = await dbContext.EmployeeGroups
            .AsNoTracking()
            .Include(g => g.WorkSchedules)
            .Include(g => g.RotationEntries)
                .ThenInclude(re => re.WorkSchedule)
            .FirstOrDefaultAsync(g => g.EmployeeGroupNumber == employee.EmployeeGroup, CancellationToken.None);
        if (group is null)
        {
            return new EmployeeResponse(
                employee.EmployeeId,
                badge,
                employee.FullName,
                null);
        }

        var scheduleDto = BuildWorkScheduleReadDto(group, punchDate);
        return new EmployeeResponse(
            employee.EmployeeId,
            badge,
            employee.FullName,
            scheduleDto);
    }

    private WorkScheduleReadDto? BuildWorkScheduleReadDto(EmployeeGroup group, DateOnly punchDate)
    {
        var scheduleResponse = group.GetGroupWorkScheduleInDateTime(punchDate);
        if (scheduleResponse is null)
        {
            return null;
        }

        var rotation = group.RotationEntries
            .FirstOrDefault(re => re.Position == group.GetRotation(punchDate)?.Position);

        var workSchedule = rotation?.WorkSchedule;
        if (workSchedule is null)
        {
            return new WorkScheduleReadDto(
                Guid.Empty,
                group.Id.Value,
                default,
                default,
                TimeSpan.Zero,
                0,
                default,
                default,
                0,
                0,
                false,
                scheduleResponse.ExpectedCheckInAt,
                scheduleResponse.ExpectedCheckoutAt,
                default,
                default,
                EmployeeWorkStatus.Rest);
        }

        var workStatus = rotation?.Status == RotationStatus.Work ? EmployeeWorkStatus.Work : EmployeeWorkStatus.Rest;
        var breakStartDateTime = punchDate.ToDateTime(workSchedule.BreakStartTime);
        var breakEndDateTime = punchDate.ToDateTime(workSchedule.BreakEndTime);

        return new WorkScheduleReadDto(
            workSchedule.Id.Value,
            group.Id.Value,
            workSchedule.ShiftStartTime,
            workSchedule.ShiftEndTime,
            workSchedule.WorkTime,
            workSchedule.EndDayOffset,
            workSchedule.BreakStartTime,
            workSchedule.BreakEndTime,
            workSchedule.AllowedCheckInLatenessMinutes,
            workSchedule.AllowedCheckOutEarlinessMinutes,
            workSchedule.IsActive,
            scheduleResponse.ExpectedCheckInAt,
            scheduleResponse.ExpectedCheckoutAt,
            breakStartDateTime,
            breakEndDateTime,
            workStatus);
    }

    public async Task<Result<WorkScheduleReadDto>> GetEmployeeWorkSchedule(Guid employeeGroupId, CancellationToken ct = default)
    {
        var group = await dbContext.EmployeeGroups
            .Include(g => g.WorkSchedules)
            .Include(g => g.RotationEntries)
                .ThenInclude(re => re.WorkSchedule)
            .FirstOrDefaultAsync(g => g.Id == new EmployeeGroupId(employeeGroupId), ct);
        if (group is null)
        {
            return Result<WorkScheduleReadDto>.Failure(EmployeeGroupErrors.NotFound);
        }

        var punchDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var scheduleDto = BuildWorkScheduleReadDto(group, punchDate);
        if (scheduleDto is null)
        {
            return Result<WorkScheduleReadDto>.Failure(EmployeeGroupErrors.RotationEntryNotFound);
        }

        return Result<WorkScheduleReadDto>.Success(scheduleDto);
    }

    public async Task<Result<EmployeeReponseForAttendance>> GetEmployeeForAttendance(int badge, DateOnly punchDate, CancellationToken ct = default)
    {
        var employee = await employeeRepo.GetEmployeeByBgdeAsync(badge.ToString(), ct);
        if (employee is null)
        {
            return Result<EmployeeReponseForAttendance>.Failure(EmployeeErrors.NotFound);
        }
        var group = await dbContext.EmployeeGroups
                   .Include(g => g.WorkSchedules)
                   .Include(g => g.RotationEntries)
                       .ThenInclude(re => re.WorkSchedule)
                   .FirstOrDefaultAsync(g => g.EmployeeGroupNumber == employee.EmployeeGroup, ct);
        if (group is null)
        {
            return Result<EmployeeReponseForAttendance>.Failure(EmployeeGroupErrors.NotFound);
        }
        var groupWorkSchedule = group.GetGroupWorkScheduleInDateTime(punchDate);
        if (groupWorkSchedule is null)
        {

            var r = new EmployeeReponseForAttendance(
                employee.EmployeeId,
                EmployeeWorkStatus.Rest,
                DateTime.MinValue,
                DateTime.MaxValue,
                TimeSpan.Zero);
            return Result<EmployeeReponseForAttendance>.Success(r);
        }
        var workStatus = groupWorkSchedule.RotationStatus == RotationStatus.Work ? EmployeeWorkStatus.Work : EmployeeWorkStatus.Rest;
        var response = new EmployeeReponseForAttendance(
            employee.EmployeeId,
            workStatus,
            groupWorkSchedule.ExpectedCheckInAt,
            groupWorkSchedule.ExpectedCheckoutAt,
            groupWorkSchedule.WorkTime);
        return Result<EmployeeReponseForAttendance>.Success(response);
    }
}