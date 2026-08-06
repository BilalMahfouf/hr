using Microsoft.AspNetCore.Builder;
using Modules.Shared.Endpoints;
using Modules.Shared.Infrastructure.Outbox;
using Modules.Shared.Infrastructure.Persistence;

public static class Test
{
    public class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("test/outbox", (SharedDbContext dbContext) => { 
                  var outboxMessages = dbContext.Set<OutboxMessage>().ToList();
                  return outboxMessages;
                });
        }
    }
}
