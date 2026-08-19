using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using Modules.Attendence.Domain.Punches;
using Modules.Attendence.Infrastructure.Presistance;
using Modules.Shared.Infrastructure.Outbox;
using PublicApi.Infrastructure.Persistence;

namespace Application.IntegrationTests.Attendence;

public sealed class PunchOutboxTests : AttendenceTestBase
{
    private const int EmployeeBadge = 100;

    public PunchOutboxTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task SavePunch_PersistsPunchAndWritesPunchCreatedEventToOutbox()
    {
        using var scope = CreateScope();
        var attendanceDb = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        var punch = Punch.Create(MachineId.New(), EmployeeBadge, DateTime.UtcNow, DateTime.UtcNow);

        attendanceDb.Punches.Add(punch);
        await attendanceDb.SaveChangesAsync();

        var saved = await attendanceDb.Punches.SingleOrDefaultAsync(p => p.Id == punch.Id);
        Assert.NotNull(saved);
        Assert.Equal(EmployeeBadge, saved!.EmployeeBadge);

        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var message = await appDb.Set<OutboxMessage>()
            .OrderByDescending(m => m.CreatedOnUtc)
            .FirstOrDefaultAsync(m => m.Name.Contains(nameof(PunchCreatedDomainEvent)));

        Assert.NotNull(message);
        Assert.Equal(
            typeof(PunchCreatedDomainEvent).AssemblyQualifiedName,
            message!.Name);
        Assert.Null(message.ProcessedOnUtc);
    }

    [Fact]
    public async Task SaveTwoPunches_WritesTwoOutboxMessages()
    {
        using var scope = CreateScope();
        var attendanceDb = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        attendanceDb.Punches.Add(Punch.Create(MachineId.New(), EmployeeBadge, DateTime.UtcNow, DateTime.UtcNow));
        attendanceDb.Punches.Add(Punch.Create(MachineId.New(), EmployeeBadge, DateTime.UtcNow.AddMinutes(1), DateTime.UtcNow));
        await attendanceDb.SaveChangesAsync();

        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var messages = await appDb.Set<OutboxMessage>()
            .Where(m => m.Name.Contains(nameof(PunchCreatedDomainEvent)))
            .ToListAsync();

        Assert.Equal(2, messages.Count);
    }

    [Fact]
    public async Task SavePunch_WhenBadgeInvalid_DoesNotWriteToOutbox()
    {
        using var scope = CreateScope();
        var attendanceDb = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Punch.Create(MachineId.New(), 0, DateTime.UtcNow, DateTime.UtcNow));

        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var messages = await appDb.Set<OutboxMessage>()
            .Where(m => m.Name.Contains(nameof(PunchCreatedDomainEvent)))
            .ToListAsync();

        Assert.Empty(messages);
    }
}
