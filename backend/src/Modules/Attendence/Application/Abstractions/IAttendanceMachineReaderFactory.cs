using Modules.Attendence.Domain.Machines;

namespace Modules.Attendence.Application.Abstractions;

public interface IAttendanceMachineReaderFactory
{
    IAttendanceMachineReader Create(AttendenceMachine machine);
}
