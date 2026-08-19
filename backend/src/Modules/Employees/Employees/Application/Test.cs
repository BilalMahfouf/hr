using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modules.Shared.Endpoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Employees.Application;

public static class Test
{
    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/employees/test", () =>
            {
                return Results.Ok("hola");
            }).WithTags("Employees");
        }
    }
}
