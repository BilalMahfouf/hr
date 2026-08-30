using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;
using Modules.Attendence.Application.PunchPolling;
using Modules.Attendence.Domain.PunchPolling;
using Modules.Attendence.Infrastructure.Presistance;

namespace Application.Tests.Attendence;

public sealed class UpdatePunchPollingSettingsTests
{
    private static (UpdatePunchPollingSettings.CommandHandler handler, AttendanceDbContext db, Mock<IPunchPollingScheduler> schedulerMock) Arrange()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AttendanceDbContext(options);
        var schedulerMock = new Mock<IPunchPollingScheduler>();

        var handler = new UpdatePunchPollingSettings.CommandHandler(
            db,
            new UpdatePunchPollingSettings.Validator(),
            schedulerMock.Object);

        return (handler, db, schedulerMock);
    }

    [Fact]
    public async Task Handle_WithValidSettings_UpdatesAndSucceeds()
    {
        var (handler, db, schedulerMock) = Arrange();

        var result = await handler.Handle(
            new UpdatePunchPollingSettings.Command(true, 20));

        Assert.True(result.IsSuccess);

        var settings = await db.PunchPollingSettings.SingleAsync();
        Assert.True(settings.IsEnabled);
        Assert.Equal(20, settings.IntervalMinutes);

        schedulerMock.Verify(s => s.ScheduleAsync(true, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DisablingPolling_SetsIsEnabledFalse()
    {
        var (handler, db, _) = Arrange();

        // First enable
        await handler.Handle(new UpdatePunchPollingSettings.Command(true, 30));

        // Then disable
        var result = await handler.Handle(new UpdatePunchPollingSettings.Command(false, 30));

        Assert.True(result.IsSuccess);
        var settings = await db.PunchPollingSettings.SingleAsync();
        Assert.False(settings.IsEnabled);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(61)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Handle_WithInvalidInterval_ThrowsValidationException(int interval)
    {
        var (handler, db, _) = Arrange();

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new UpdatePunchPollingSettings.Command(true, interval)));

        Assert.Empty(db.PunchPollingSettings);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(60)]
    public async Task Handle_WithValidInterval_Succeeds(int interval)
    {
        var (handler, db, _) = Arrange();

        var result = await handler.Handle(
            new UpdatePunchPollingSettings.Command(true, interval));

        Assert.True(result.IsSuccess);
        var settings = await db.PunchPollingSettings.SingleAsync();
        Assert.Equal(interval, settings.IntervalMinutes);
    }

    [Fact]
    public async Task Handle_WhenSettingsExist_UpdatesExistingRecord()
    {
        var (handler, db, _) = Arrange();

        // Create initial settings
        await handler.Handle(new UpdatePunchPollingSettings.Command(true, 20));

        // Update
        await handler.Handle(new UpdatePunchPollingSettings.Command(true, 45));

        var settings = await db.PunchPollingSettings.SingleAsync();
        Assert.Equal(45, settings.IntervalMinutes);

        // Should still only be one record
        Assert.Single(await db.PunchPollingSettings.ToListAsync());
    }

    [Fact]
    public async Task Handle_CallsSchedulerWithCorrectParameters()
    {
        var (handler, _, schedulerMock) = Arrange();

        await handler.Handle(new UpdatePunchPollingSettings.Command(false, 15));

        schedulerMock.Verify(s => s.ScheduleAsync(false, 15, It.IsAny<CancellationToken>()), Times.Once);
    }
}
