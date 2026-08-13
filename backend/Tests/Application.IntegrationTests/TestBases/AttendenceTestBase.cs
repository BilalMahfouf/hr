using Application.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
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

    protected TestEmployeeApi EmployeeApi { get; } = new();

    protected MachineId MachineId { get; } = MachineId.New();

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await using var scope = RootProvider.CreateAsyncScope();
        var attendanceDb = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        await attendanceDb.Database.MigrateAsync();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<AttendanceDbContext>((sp, options) =>
        {
            options.UseNpgsql(ConnectionString)
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptors>());
        });
        services.AddScoped<IAttendanceDbContext>(sp => sp.GetRequiredService<AttendanceDbContext>());
        services.AddSingleton<IEmployeeApi>(EmployeeApi);
    }

    protected CreateAttendenceRecord.CommandHandler CreateCreateAttendenceRecordHandler(
        IServiceProvider services)
    {
        return new CreateAttendenceRecord.CommandHandler(
            services.GetRequiredService<IEmployeeApi>(),
            services.GetRequiredService<IAttendanceDbContext>());
    }
}
