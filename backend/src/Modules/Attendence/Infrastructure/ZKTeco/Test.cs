using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modules.Shared.Endpoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence.Infrastructure.ZKTeco;

public static class Test
{
    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/attendence/test", (IZKemSessionFactory factory) =>
            {
                var session = factory.Create();
                if (!session.ConnectNet("192.168.3.205", 4370))
                {
                    return Results.NotFound("no connection is working ");
                }
                return Results.Ok("hola i'm working ");

            }).WithTags("Attendence");
        }
    }
}
