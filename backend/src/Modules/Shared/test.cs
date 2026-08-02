using Modules.Shared.Endpoints;
using Modules.Shared.Infrastructure.Persistence;

public static class Test
{
    public class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("test/outbox", (SharedDbContext) => { 
                  var outboxMessages = SharedDbContext.Set<Outbox>
                });
        }
    }
}
