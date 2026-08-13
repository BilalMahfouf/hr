using Modules.Attendence.Domain.AttendenceRecords;

namespace Domain.Tests.Attendence;

public sealed class AttendanceRecordIdTests
{
    [Fact]
    public void New_ReturnsNonEmptyId()
    {
        var id = AttendanceRecordId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void Empty_ReturnsGuidEmpty()
    {
        Assert.Equal(Guid.Empty, AttendanceRecordId.Empty.Value);
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var id = AttendanceRecordId.New();

        Assert.Equal(id.Value.ToString(), id.ToString());
    }
}