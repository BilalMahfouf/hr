using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Attendence.Application.AttendenceRerords;
using Modules.Attendence.Domain.Punches;
using Modules.Shared.CQRS;
using Modules.Shared.Domain.Common;

namespace Modules.Attendence.Application.Punches;

public class PunchCreatedDomainEventHandler(
    ICommandHandler<CreateAttendenceRecord.Command> commandHandler,
    ILogger<PunchCreatedDomainEventHandler> logger) : IDomainEventHandler<PunchCreatedDomainEvent>
{
    public async Task Handle(PunchCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command = new CreateAttendenceRecord.Command(
            domainEvent.EmployeeBadge,
            domainEvent.MachineId,
            domainEvent.PunchOccurredAt
        );

        try
        {
            await commandHandler.Handle(command, cancellationToken);
        }
        catch (DomainException e)
        {
            logger.LogError("Domain Exception occurred while handling PunchCreatedDomainEvent: {Error}", e.Error);
        }
        catch (DbUpdateException e)
        {
            logger.LogError("Db Exception occurred while handling PunchCreatedDomainEvent: {Error}", e.Message);
        }
    }
}