using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Modules.Shared.Endpoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence.Infrastructure.ZKTeco.Gateway;

public static  class Test
{
    public sealed record Request(string ip, int port);
    public class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("attendance/connect", async (
                [FromBody] Request request,
                ZKTecoGatwayMachineReader reader,
                CancellationToken ct) =>
            {
                await reader.ConnectAsync(request.ip, request.port, ct);
                return Results.Ok();

            }).WithTags("ZKTeco");
        }
    }
}
