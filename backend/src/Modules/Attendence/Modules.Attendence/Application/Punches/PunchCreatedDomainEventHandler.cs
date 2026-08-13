using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.AttendenceRerords;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Attendence.Domain.Punches;
using Modules.Employees.Contracts;
using Modules.Shared.CQRS;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence.Application.Punches;

internal class PunchCreatedDomainEventHandler(
    ICommandHandler<CreateAttendenceRecord.Command> commandHandler) : IDomainEventHandler<PunchCreatedDomainEvent>
{

    public async Task Handle(PunchCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command  = new CreateAttendenceRecord.Command(
            domainEvent.EmployeeBadge,
            domainEvent.MachineId,
            domainEvent.PunchOccurredAt
        );

        await commandHandler.Handle(command, cancellationToken);
    }
}
