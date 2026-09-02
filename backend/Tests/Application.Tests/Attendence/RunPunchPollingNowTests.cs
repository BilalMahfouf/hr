using Moq;
using Modules.Attendence.Application.Importer;
using Modules.Attendence.Application.PunchPolling;
using Modules.Shared.CQRS;
using Modules.Shared.Errors;
using Modules.Shared.Results;

namespace Application.Tests.Attendence;

public sealed class RunPunchPollingNowTests
{
    private static (RunPunchPollingNow.CommandHandler handler, Mock<ICommandHandler<ImportAttendanceLogs.Command, ImportAttendanceLogs.Response>> importMock) Arrange()
    {
        var importMock = new Mock<ICommandHandler<ImportAttendanceLogs.Command, ImportAttendanceLogs.Response>>();

        var handler = new RunPunchPollingNow.CommandHandler(importMock.Object);

        return (handler, importMock);
    }

    [Fact]
    public async Task Handle_CallsImportHandler_WithTodayDate()
    {
        var (handler, importMock) = Arrange();
        importMock
            .Setup(h => h.Handle(
                It.IsAny<ImportAttendanceLogs.Command>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImportAttendanceLogs.Response>.Success(
                new ImportAttendanceLogs.Response(2, 5)));

        var result = await handler.Handle(
            new RunPunchPollingNow.Command());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.MachineCount);
        Assert.Equal(5, result.Value.PunchCount);

        importMock.Verify(h => h.Handle(
            It.Is<ImportAttendanceLogs.Command>(c =>
                c.From == DateOnly.FromDateTime(DateTime.UtcNow) &&
                c.To == DateOnly.FromDateTime(DateTime.UtcNow)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenImportFails_ReturnsFailure()
    {
        var (handler, importMock) = Arrange();
        importMock
            .Setup(h => h.Handle(
                It.IsAny<ImportAttendanceLogs.Command>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImportAttendanceLogs.Response>.Failure(
                Error.Failure("Import.Failed", "Failed to read from machines")));

        var result = await handler.Handle(
            new RunPunchPollingNow.Command());

        Assert.False(result.IsSuccess);
        Assert.Equal("Import.Failed", result.Error.Code);
    }
}
