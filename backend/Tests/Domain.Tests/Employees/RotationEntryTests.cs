using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.Domain.Common;

namespace Domain.Tests.Employees;

public sealed class RotationEntryTests
{
    private static readonly EmployeeGroupId EmployeeGroupId = EmployeeGroupId.New();

    #region Create

    [Fact]
    public void Create_SetsExpectedInitialState()
    {
        var entry = RotationEntry.Create(EmployeeGroupId, position: 1, workScheduleId: null);

        Assert.NotEqual(Guid.Empty, entry.Id.Value);
        Assert.Equal(EmployeeGroupId, entry.EmployeeGroupId);
        Assert.Equal(1, entry.Position);
        Assert.Null(entry.WorkScheduleId);
    }

    [Fact]
    public void Create_WithWorkScheduleId_SetsWorkScheduleId()
    {
        var workScheduleId = WorkScheduleId.New();

        var entry = RotationEntry.Create(EmployeeGroupId, position: 1, workScheduleId);

        Assert.Equal(workScheduleId, entry.WorkScheduleId);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var entry1 = RotationEntry.Create(EmployeeGroupId, 1, null);
        var entry2 = RotationEntry.Create(EmployeeGroupId, 2, null);

        Assert.NotEqual(entry1.Id, entry2.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WhenPositionLessThan1_ThrowsDomainException(int position)
    {
        var exception = Assert.Throws<DomainException>(() =>
            RotationEntry.Create(EmployeeGroupId, position, null));

        Assert.Equal(RotationEntryErrors.InvalidPosition.Code, exception.Error.Code);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    public void Create_WhenPositionValid_Succeeds(int position)
    {
        var entry = RotationEntry.Create(EmployeeGroupId, position, null);

        Assert.Equal(position, entry.Position);
    }

    #endregion

    #region Status

    [Fact]
    public void Status_WhenWorkScheduleIdIsNull_ReturnsRest()
    {
        var entry = RotationEntry.Create(EmployeeGroupId, 1, workScheduleId: null);

        Assert.Equal(RotationStatus.Rest, entry.Status);
    }

    [Fact]
    public void Status_WhenWorkScheduleIdNotNull_ReturnsWork()
    {
        var entry = RotationEntry.Create(EmployeeGroupId, 1, WorkScheduleId.New());

        Assert.Equal(RotationStatus.Work, entry.Status);
    }

    #endregion
}
