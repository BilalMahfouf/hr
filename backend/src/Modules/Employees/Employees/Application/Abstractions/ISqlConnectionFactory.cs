using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;


namespace Modules.Employees.Application.Abstractions;

public interface ISqlConnectionFactory
{
    public SqlConnection CreateConnection();
}
