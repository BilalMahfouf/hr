using Modules.Identity.Abstracions;
using Modules.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Modules.Shared.Abstracions;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Identity.Application.Users;

public static class UpdateUserProfile
{
    public sealed record UpdateUserProfileCommand(
        string UserName,
        string FirstName,
        string LastName) : ICommand;

    public sealed class UpdateUserProfileCommandHandler
        : ICommandHandler<UpdateUserProfileCommand>
    {
        private readonly IIdentityApplicationDbContext _db;
        private readonly ICurrentTenant _currentTenant;

        public UpdateUserProfileCommandHandler(
            IIdentityApplicationDbContext db,
            ICurrentTenant currentTenant)
        {
            _db = db;
            _currentTenant = currentTenant;
        }

        public async Task<Result> Handle(
            UpdateUserProfileCommand command,
            CancellationToken cancellationToken = default)
        {
            var userId = _currentTenant.UserId;
            var user = await _db.Users
                .FirstOrDefaultAsync(e => e.Id == userId, cancellationToken);
            if (user is null)
            {
                return Result.Failure(UserErrors.NotFound);
            }
            user.UpdateProfile(command.UserName, command.FirstName, command.LastName);
            _db.Users.Update(user);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("/update-profile", async (
                UpdateUserProfileCommand command,
                ICommandHandler<UpdateUserProfileCommand> handler) =>
            {
                var result = await handler.Handle(command);
                return result.IsSuccess ? Results.NoContent() : result.Problem();
            }).RequireAuthorization()
            .WithTags($"{nameof(User)}s")
            .WithSummary("Update user profile")
            .WithDescription("Updates the authenticated user's profile information including username and name.");
        }
    }
}
