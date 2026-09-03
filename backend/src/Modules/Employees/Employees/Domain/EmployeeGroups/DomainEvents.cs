using Modules.Shared.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Employees.Domain.EmployeeGroups;


public sealed record EmployeeGroupUpdatedDomainEvent(Guid GroupId, DateOnly OldRotationStartDate) : DomainEvent;

public sealed record EmployeeGroupRotationStartDateUpdatedDomainEvent(Guid GroupId, DateOnly OldRotationStartDate) : DomainEvent;
