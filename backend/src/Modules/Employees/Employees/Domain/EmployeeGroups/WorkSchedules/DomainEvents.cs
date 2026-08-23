using Modules.Shared.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Employees.Domain.EmployeeGroups.WorkSchedules;

public sealed record WorkSheduleActivatedDomainEvent(
    WorkScheduleId WorkScheduleId,
    EmployeeGroupId EmployeeGroupId,
    DateTime ActivatedAt
) : DomainEvent;
public sealed record WorkSheduleDeactivatedDomainEvent(
    WorkScheduleId WorkScheduleId,
    EmployeeGroupId EmployeeGroupId,
    DateTime DeactivatedAt
) : DomainEvent;

