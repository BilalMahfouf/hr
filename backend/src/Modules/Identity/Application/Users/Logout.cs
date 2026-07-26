using Identity.Abstracions;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;

namespace Identity.Application.Users;

public class Logout
{
    public sealed record LogoutCommand(string RefreshToken)
        : ICommand;

    public sealed class LogoutCommandHandler
        : ICommandHandler<LogoutCommand>
    {
        private readonly IIdentityApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LogoutCommandHandler(
            IIdentityApplicationDbContext db,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken = default)
        {
            var session = await _db.UserSessions
                .FirstOrDefaultAsync(e => e.Token == command.RefreshToken);
            if (session is null)
            {
                return Result.Failure(UserErrors.InvalidCredentials);
            }
            _db.UserSessions.Remove(session);
            await _db.SaveChangesAsync(cancellationToken);

            _httpContextAccessor.HttpContext!.Response.Cookies.Delete("refreshToken");
            return Result.Success;
        }
    }

    public class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/logout", async (
               HttpContext httpContext,
               [FromServices] ICommandHandler<LogoutCommand> handler,
                CancellationToken cancellationToken = default) =>
            {
                var refreshToken = httpContext.Request
                .Cookies["refreshToken"] ?? string.Empty;
                var command = new LogoutCommand(refreshToken);
                var result = await handler.Handle(command, cancellationToken);
                return result.IsSuccess ? Results.Ok() : result.Problem();
            })
            .WithTags("Authentication")
            .WithSummary("User logout")
            .WithDescription("Logs out the current user by invalidating the refresh token and clearing the refresh token cookie.");
        }
    }
}
