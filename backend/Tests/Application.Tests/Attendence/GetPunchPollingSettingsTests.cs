using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.PunchPolling;
using Modules.Attendence.Domain.PunchPolling;
using Modules.Attendence.Infrastructure.Presistance;

namespace Application.Tests.Attendence;

public sealed class GetPunchPollingSettingsTests
{
    private static (GetPunchPollingSettings.QueryHandler handler, AttendanceDbContext db) Arrange()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AttendanceDbContext(options);

        return (new GetPunchPollingSettings.QueryHandler(db), db);
    }

    [Fact]
    public async Task Handle_WhenSettingsExist_ReturnsSettings()
    {
        var (handler, db) = Arrange();
        var settings = PunchPollingSettings.Create(
            PunchPollingSettingsId.New(), true, 30);
        db.PunchPollingSettings.Add(settings);
        await db.SaveChangesAsync();

        var result = await handler.Handle(
            new GetPunchPollingSettings.Query());

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsEnabled);
        Assert.Equal(30, result.Value.IntervalMinutes);
    }

    [Fact]
    public async Task Handle_WhenNoSettingsExist_ReturnsDefault()
    {
        var (handler, _) = Arrange();

        var result = await handler.Handle(
            new GetPunchPollingSettings.Query());

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsEnabled);
        Assert.Equal(30, result.Value.IntervalMinutes);
    }
}
