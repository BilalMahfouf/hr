using Modules.Attendence.Domain.Punches;
using Modules.Shared.CQRS;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence.Application.Punches;

internal class PunchCreatedDomainEventHandler : IDomainEventHandler<PunchCreatedDomainEvent>
{

    public Task Handle(PunchCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
