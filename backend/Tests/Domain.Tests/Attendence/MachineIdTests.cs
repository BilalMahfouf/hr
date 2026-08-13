namespace Domain.Tests.Attendence;

public sealed class MachineIdTests
{
    [Fact]
    public void New_ReturnsNonEmptyId()
    {
        var id = MachineId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void From_WithEmptyGuid_Throws()
    {
        Assert.Throws<ArgumentException>(() => MachineId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithValidGuid_ReturnsId()
    {
        var value = Guid.NewGuid();

        var id = MachineId.From(value);

        Assert.Equal(value, id.Value);
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var id = MachineId.From(Guid.NewGuid());

        Assert.Equal(id.Value.ToString(), id.ToString());
    }
}