using Modules.Attendence.Application.Abstractions;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Infrastructure.ZKTeco;
using Moq;

namespace Application.Tests.Attendence;

public sealed class AttendanceMachineReaderFactoryTests
{
    private static readonly ZKTecoAttendanceMachineReader SdkReader = new(Mock.Of<IZKemSessionFactory>());
    private static readonly ZKTecoGatwayMachineReader GatewayReader = new(new HttpClient());

    private static AttendanceMachineReaderFactory CreateFactory()
        => new(SdkReader, GatewayReader);

    [Fact]
    public void Create_WhenZKTecoSdkType_ReturnsSdkReader()
    {
        var factory = CreateFactory();
        var machine = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoSdk);

        var reader = factory.Create(machine);

        Assert.Same(SdkReader, reader);
    }

    [Fact]
    public void Create_WhenZKTecoGatewayType_ReturnsGatewayReader()
    {
        var factory = CreateFactory();
        var machine = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);

        var reader = factory.Create(machine);

        Assert.Same(GatewayReader, reader);
    }

    [Fact]
    public void Create_WhenUnsupportedType_ThrowsNotSupportedException()
    {
        var factory = CreateFactory();
        var machine = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, (MachineType)999);

        var ex = Assert.Throws<NotSupportedException>(() => factory.Create(machine));
        Assert.Contains("999", ex.Message);
    }
}
