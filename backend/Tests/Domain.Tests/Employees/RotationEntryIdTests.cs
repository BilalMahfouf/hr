using Modules.Employees.Domain.EmployeeGroups.Rotation;

namespace Domain.Tests.Employees;

public sealed class RotationEntryIdTests
{
    [Fact]
    public void New_ReturnsNonEmptyId()
    {
        var id = RotationEntryId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void From_WhenEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => RotationEntryId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithValidGuid_ReturnsId()
    {
        var guid = Guid.NewGuid();

        var id = RotationEntryId.From(guid);

        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var id = RotationEntryId.New();

        Assert.Equal(id.Value.ToString(), id.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToGuid()
    {
        var id = RotationEntryId.New();

        Guid guid = id;

        Assert.Equal(id.Value, guid);
    }

    [Fact]
    public void ExplicitConversion_FromGuid()
    {
        var guid = Guid.NewGuid();

        var id = (RotationEntryId)guid;

        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void TwoIds_WithSameValue_AreEqual()
    {
        var guid = Guid.NewGuid();
        var id1 = RotationEntryId.From(guid);
        var id2 = RotationEntryId.From(guid);

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void TwoIds_WithDifferentValues_AreNotEqual()
    {
        var id1 = RotationEntryId.New();
        var id2 = RotationEntryId.New();

        Assert.NotEqual(id1, id2);
    }
}
