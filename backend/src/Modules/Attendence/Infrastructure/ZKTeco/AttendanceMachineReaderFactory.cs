using Modules.Attendence.Application.Abstractions;
using Modules.Attendence.Domain.Machines;

namespace Modules.Attendence.Infrastructure.ZKTeco;

public sealed class AttendanceMachineReaderFactory(
    ZKTecoAttendanceMachineReader sdkReader,
    ZKTecoGatwayMachineReader gatewayReader) : IAttendanceMachineReaderFactory
{
    public IAttendanceMachineReader Create(AttendenceMachine machine) => machine.Type switch
    {
        MachineType.ZKTecoSdk => sdkReader,
        MachineType.ZKTecoGateway => gatewayReader,
        _ => throw new NotSupportedException(
            $"Machine type '{machine.Type}' is not supported.")
    };
}
