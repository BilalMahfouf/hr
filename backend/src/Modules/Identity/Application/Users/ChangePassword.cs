using Modules.Identity.Abstracions;
using Modules.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Modules.Shared.Abstracions;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Errors;
using Modules.Shared.Results;

namespace Modules.Identity.Application.Users;

public static class ChangePassword
{
    public sealed record ChangePasswordCommand(
        string CurrentPassword,
        string NewPassword,
        string ConfirmNewPassword) : ICommand;

    public sealed class ChangePasswordCommandHandler
        : ICommandHandler<ChangePasswordCommand>
    {
        private readonly IIdentityApplicationDbContext _db;
        private readonly ICurrentTenant _currentTenant;
        private readonly IPasswordHasher _passwordHasher;

        public ChangePasswordCommandHandler(
            IIdentityApplicationDbContext db,
            ICurrentTenant currentTenant,
            IPasswordHasher passwordHasher)
        {
            _db = db;
            _currentTenant = currentTenant;
            _passwordHasher = passwordHasher;
        }

        public async Task<Result> Handle(
            ChangePasswordCommand command,
            CancellationToken cancellationToken = default)
        {
            var userId = _currentTenant.UserId;
            var user = await _db.Users
                .FirstOrDefaultAsync(e => e.Id == userId, cancellationToken);
            if (user is null)
            {
                return Result.Failure(UserErrors.NotFound);
            }
            if (!_passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
            {
                return Result.Failure(UserErrors.InvalidPassword);
            }
            var newPasswordHash = _passwordHasher.Hash(command.NewPassword);
            user.UpdatePassword(command.NewPassword, newPasswordHash);
            _db.Users.Update(user);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/change-password", async (
                ChangePasswordCommand command,
                ICommandHandler<ChangePasswordCommand> handler,
                CancellationToken ct = default) =>
            {
                var result = await handler.Handle(command, ct);
                return result.IsSuccess ? Results.Ok() : result.Problem();
            }).RequireAuthorization()
            .WithTags($"{nameof(User)}s")
            .WithSummary("Change password")
            .WithDescription("Updates the authenticated user's password. Requires current password verification.");
        }
    }
}
