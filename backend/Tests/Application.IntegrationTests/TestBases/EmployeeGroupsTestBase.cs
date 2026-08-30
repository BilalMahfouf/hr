using Application.IntegrationTests.Infrastructure;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Application.EmployeeGroups;
using Modules.Employees.Application.EmployeeGroups.Rotations;
using Modules.Employees.Application.EmployeeGroups.WorkSchedules;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Employees.Infrastructure.Presistance;
using Modules.Shared.Infrastructure.Outbox;

namespace Application.IntegrationTests.TestBases;

public abstract class EmployeeGroupsTestBase : IntegrationTestBase
{
    protected EmployeeGroupsTestBase(PostgresFixture fixture) : base(fixture)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await using var scope = RootProvider.CreateAsyncScope();
        var employeeDb = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
        await employeeDb.Database.MigrateAsync();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<EmployeeDbContext>((sp, options) =>
        {
            options.UseNpgsql(ConnectionString)
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptors>());
        });
        services.AddScoped<IEmployeeDbContext>(sp => sp.GetRequiredService<EmployeeDbContext>());

        services.AddSingleton<IValidator<CreateEmployeeGroupCommand>>(new CreateEmployeeGroup.Validator());
        services.AddSingleton<IValidator<UpdateEmployeeGroupCommand>>(new UpdateEmployeeGroup.Validator());
        services.AddSingleton<IValidator<ReplaceSchedulesAndRotationsCommand>>(new ReplaceSchedulesAndRotations.Validator());
        services.AddSingleton<IValidator<CreateWorkScheduleCommand>>(new CreateWorkSchedule.Validator());
        services.AddSingleton<IValidator<UpdateWorkScheduleCommand>>(new UpdateWorkSchedule.Validator());
        services.AddSingleton<IValidator<CreateWorkRotationCommand>>(new CreateWorkRotation.Validator());
        services.AddSingleton<IValidator<CreateRestRotationCommand>>(new CreateRestRotation.Validator());
        services.AddSingleton<IValidator<UpdateRotationCommand>>(new UpdateRotationPosition.Validator());
    }

    protected static CreateEmployeeGroup.Handler CreateCreateHandler(IServiceProvider services)
    {
        return new CreateEmployeeGroup.Handler(
            services.GetRequiredService<IEmployeeDbContext>(),
            services.GetRequiredService<IValidator<CreateEmployeeGroupCommand>>());
    }

    protected static UpdateEmployeeGroup.Handler CreateUpdateHandler(IServiceProvider services)
    {
        return new UpdateEmployeeGroup.Handler(
            services.GetRequiredService<IEmployeeDbContext>(),
            services.GetRequiredService<IValidator<UpdateEmployeeGroupCommand>>());
    }

    protected static DeleteEmployeeGroup.Handler CreateDeleteHandler(IServiceProvider services)
    {
        return new DeleteEmployeeGroup.Handler(
            services.GetRequiredService<IEmployeeDbContext>());
    }

    protected static GetEmployeeGroupById.Handler CreateGetByIdHandler(IServiceProvider services)
    {
        return new GetEmployeeGroupById.Handler(
            services.GetRequiredService<IEmployeeDbContext>());
    }

    protected static GetAllEmployeeGroups.Handler CreateGetAllHandler(IServiceProvider services)
    {
        return new GetAllEmployeeGroups.Handler(
            services.GetRequiredService<IEmployeeDbContext>());
    }

    protected static ReplaceSchedulesAndRotations.Handler CreateReplaceHandler(IServiceProvider services)
    {
        return new ReplaceSchedulesAndRotations.Handler(
            services.GetRequiredService<IEmployeeDbContext>(),
            services.GetRequiredService<IValidator<ReplaceSchedulesAndRotationsCommand>>());
    }

    protected static CreateWorkSchedule.Handler CreateWorkScheduleHandler(IServiceProvider services)
    {
        return new CreateWorkSchedule.Handler(
            services.GetRequiredService<IEmployeeDbContext>(),
            services.GetRequiredService<IValidator<CreateWorkScheduleCommand>>());
    }

    protected static GetWorkScheduleById.Handler CreateGetWorkScheduleByIdHandler(IServiceProvider services)
    {
        return new GetWorkScheduleById.Handler(
            services.GetRequiredService<IEmployeeDbContext>());
    }

    protected static UpdateWorkSchedule.Handler CreateUpdateWorkScheduleHandler(IServiceProvider services)
    {
        return new UpdateWorkSchedule.Handler(
            services.GetRequiredService<IEmployeeDbContext>(),
            services.GetRequiredService<IValidator<UpdateWorkScheduleCommand>>());
    }

    protected static DeleteWorkSchedule.Handler CreateDeleteWorkScheduleHandler(IServiceProvider services)
    {
        return new DeleteWorkSchedule.Handler(
            services.GetRequiredService<IEmployeeDbContext>());
    }

    protected static ActivateWorkSchedule.Handler CreateActivateWorkScheduleHandler(IServiceProvider services)
    {
        return new ActivateWorkSchedule.Handler(
            services.GetRequiredService<IEmployeeDbContext>());
    }

    protected static DeactivateWorkSchedule.Handler CreateDeactivateWorkScheduleHandler(IServiceProvider services)
    {
        return new DeactivateWorkSchedule.Handler(
            services.GetRequiredService<IEmployeeDbContext>());
    }

    protected static CreateWorkRotation.Handler CreateWorkRotationHandler(IServiceProvider services)
    {
        return new CreateWorkRotation.Handler(
            services.GetRequiredService<IEmployeeDbContext>(),
            services.GetRequiredService<IValidator<CreateWorkRotationCommand>>());
    }

    protected static CreateRestRotation.Handler CreateRestRotationHandler(IServiceProvider services)
    {
        return new CreateRestRotation.Handler(
            services.GetRequiredService<IEmployeeDbContext>(),
            services.GetRequiredService<IValidator<CreateRestRotationCommand>>());
    }

    protected static GetAllRotations.Handler CreateGetAllRotationsHandler(IServiceProvider services)
    {
        return new GetAllRotations.Handler(
            services.GetRequiredService<IEmployeeDbContext>());
    }

    protected static UpdateRotationPosition.Handler CreateUpdateRotationHandler(IServiceProvider services)
    {
        return new UpdateRotationPosition.Handler(
            services.GetRequiredService<IEmployeeDbContext>(),
            services.GetRequiredService<IValidator<UpdateRotationCommand>>());
    }

    protected static DeleteRotation.Handler CreateDeleteRotationHandler(IServiceProvider services)
    {
        return new DeleteRotation.Handler(
            services.GetRequiredService<IEmployeeDbContext>());
    }

    protected static async Task<EmployeeGroup> SeedGroupAsync(
        IEmployeeDbContext db,
        string name = "Group A",
        string groupNumber = "GRP-001")
    {
        var group = EmployeeGroup.Create(groupNumber, name, false, new DateOnly(2026, 1, 1), "Test group");
        db.EmployeeGroups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }

    protected static async Task<EmployeeGroup> SeedGroupWithScheduleAsync(
        IEmployeeDbContext db,
        string name = "Group A",
        string groupNumber = "GRP-001")
    {
        var group = EmployeeGroup.Create(groupNumber, name, false, new DateOnly(2026, 1, 1), "Test group");
        group.AddWorkSchedule(new CreateWorkScheduleDto(
            group.Id,
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(16)),
            0,
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(13)),
            15,
            15));
        db.EmployeeGroups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }

    protected static async Task<(EmployeeGroup Group, WorkSchedule Schedule)> SeedGroupWithScheduleReturnScheduleAsync(
        IEmployeeDbContext db,
        string name = "Group A",
        string groupNumber = "GRP-001")
    {
        var group = EmployeeGroup.Create(groupNumber, name, false, new DateOnly(2026, 1, 1), "Test group");
        group.AddWorkSchedule(new CreateWorkScheduleDto(
            group.Id,
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(16)),
            0,
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(13)),
            15,
            15));
        db.EmployeeGroups.Add(group);
        await db.SaveChangesAsync();

        var schedule = group.WorkSchedules.First();
        return (group, schedule);
    }

    protected static async Task<EmployeeGroup> SeedGroupWithScheduleAndRotationAsync(
        IEmployeeDbContext db,
        string name = "Group A",
        string groupNumber = "GRP-001")
    {
        var group = EmployeeGroup.Create(groupNumber, name, false, new DateOnly(2026, 1, 1), "Test group");
        group.ReplaceSchedulesAndRotations(
            [
                new CreateWorkScheduleDto(
                    group.Id,
                    TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
                    TimeOnly.FromTimeSpan(TimeSpan.FromHours(16)),
                    0,
                    TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)),
                    TimeOnly.FromTimeSpan(TimeSpan.FromHours(13)),
                    15,
                    15)
            ],
            [(1, 0)]);
        db.EmployeeGroups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }

    protected static CreateWorkScheduleRequest ValidScheduleRequest() =>
        new(
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(16)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(13)),
            0,
            15,
            15);

    protected static CreateRotationEntryRequest ValidWorkRotationRequest(int position = 1, int scheduleIndex = 0) =>
        new(position, scheduleIndex);

    protected static CreateRotationEntryRequest ValidRestRotationRequest(int position = 1) =>
        new(position, null);
}
