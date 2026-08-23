using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;

namespace Domain.Tests.Employees;

public sealed class WorkScheduleIdTests
{
    [Fact]
    public void New_ReturnsNonEmptyId()
    {
        var id = WorkScheduleId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void From_WhenEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => WorkScheduleId.From(Guid.Empty));
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var id = WorkScheduleId.New();

        Assert.Equal(id.Value.ToString(), id.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToGuid()
    {
        var id = WorkScheduleId.New();

        Guid guid = id;

        Assert.Equal(id.Value, guid);
    }
}