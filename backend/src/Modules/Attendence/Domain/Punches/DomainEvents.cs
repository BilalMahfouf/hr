using Modules.Shared.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence.Domain.Punches;


public sealed record PunchCreatedDomainEvent(
    MachineId MachineId,
    int EmployeeBadge,
    DateTime PunchOccurredAt) : DomainEvent;
