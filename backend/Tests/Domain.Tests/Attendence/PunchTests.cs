using Modules.Attendence.Domain.Punches;

namespace Domain.Tests.Attendence;

public sealed class PunchTests
{
    private static readonly MachineId MachineId = MachineId.New();

    [Fact]
    public void Create_SetsExpectedStateAndRaisesDomainEvent()
    {
        var punchOccurredAt = new DateTime(2026, 8, 13, 9, 0, 0, DateTimeKind.Utc);
        var createdOnUtc = new DateTime(2026, 8, 13, 9, 0, 5, DateTimeKind.Utc);

        var punch = Punch.Create(MachineId, employeeBadge: 100, punchOccurredAt, createdOnUtc);

        Assert.NotEqual(Guid.Empty, punch.Id.Value);
        Assert.Equal(MachineId, punch.MachineId);
        Assert.Equal(100, punch.EmployeeBadge);
        Assert.Equal(punchOccurredAt, punch.PunchOccurredAt);
        Assert.Equal(createdOnUtc, punch.CreatedOnUtc);
        Assert.Single(punch.DomainEvents);
        Assert.IsType<PunchCreatedDomainEvent>(punch.DomainEvents.Single());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WhenBadgeInvalid_Throws(int badge)
    {
        var punchOccurredAt = new DateTime(2026, 8, 13, 9, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Punch.Create(MachineId, badge, punchOccurredAt, DateTime.UtcNow));
    }
}