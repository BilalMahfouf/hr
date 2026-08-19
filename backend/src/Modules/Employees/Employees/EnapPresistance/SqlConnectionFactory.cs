using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Modules.Employees.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Employees.EnapPresistance;

internal class SqlConnectionFactory : ISqlConnectionFactory
{
    public SqlConnection CreateConnection()
    {
        var connectionString = Environment.GetEnvironmentVariable("ENAP_SQL_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("connection string is null or empty ");
        }
        return new SqlConnection(connectionString);
    }
}
