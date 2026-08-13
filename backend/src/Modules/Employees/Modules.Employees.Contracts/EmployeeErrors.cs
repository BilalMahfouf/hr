using Modules.Shared.Errors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Employees.Contracts;


public static  class EmployeeErrors
{
    private  const string _entityName = "Employee";
    public static Error NotFound => Error.NotFound($"{_entityName}.{nameof(NotFound)}", "employee not found");
}
