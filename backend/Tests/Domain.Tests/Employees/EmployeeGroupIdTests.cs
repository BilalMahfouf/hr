using Modules.Employees.Domain.EmployeeGroups;

namespace Domain.Tests.Employees;

public sealed class EmployeeGroupIdTests
{
    [Fact]
    public void New_ReturnsNonEmptyId()
    {
        var id = EmployeeGroupId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void From_WhenEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => EmployeeGroupId.From(Guid.Empty));
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var id = EmployeeGroupId.New();

        Assert.Equal(id.Value.ToString(), id.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToGuid()
    {
        var id = EmployeeGroupId.New();

        Guid guid = id;

        Assert.Equal(id.Value, guid);
    }
}