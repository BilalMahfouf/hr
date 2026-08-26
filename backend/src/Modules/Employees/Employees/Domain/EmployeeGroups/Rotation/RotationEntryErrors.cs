using Modules.Shared.Errors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Employees.Domain.EmployeeGroups.Rotation;

public static  class RotationEntryErrors
{
    public static Error InvalidPosition=>
        Error.Conflict(
            code: "InvalidPosition",
            description: "Position must be greater than 0.");
}
