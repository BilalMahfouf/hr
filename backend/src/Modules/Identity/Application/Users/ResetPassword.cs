using Identity.Abstracions;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;

namespace Identity.Application.Users;

public static class ResetPassword
{
    public sealed record ResetPasswordCommand(
        string Password,
        string ConfirmPassword,
        string Token,
        string Email) : ICommand;

    public sealed class ResetPasswordCommandHandler
        : ICommandHandler<ResetPasswordCommand>
    {
        private readonly IIdentityApplicationDbContext _db;
        private readonly IPasswordHasher _passwordHasher;

        public ResetPasswordCommandHandler(
            IIdentityApplicationDbContext db,
            IPasswordHasher passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        public async Task<Result> Handle(
            ResetPasswordCommand command,
            CancellationToken cancellationToken = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(
                u => u.Email == command.Email,
                cancellationToken);
            if (user is null)
            {
                return Result.Failure(UserErrors.UserNotFound(command.Email));
            }
            var isValidToken = await _db.UserSessions
                .AnyAsync(u => u.Token == command.Token &&
            u.ExpiresAt > DateTime.UtcNow
            && u.TokenType == UserSessionTokenType.ResetPassword);
            if (!isValidToken)
            {
                return Result.Failure(UserErrors.InvalidCredentials);
            }
            var newPasswordHash = _passwordHasher.Hash(command.Password);
            user.UpdatePassword(command.Password, newPasswordHash);
            _db.Users.Update(user);

            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("/auth/reset-passowrd", async (
                ResetPasswordCommand command,
                ICommandHandler<ResetPasswordCommand> handler,
                CancellationToken cancellationToken = default) =>
            {
                var result = await handler.Handle(command, cancellationToken);
                return result.IsSuccess ? Results.Ok()
                                 : result.Problem();

            })
            .WithTags("Authentication")
            .WithSummary("Reset password")
            .WithDescription("Resets the user's password using a valid reset token. Requires matching password and confirm password fields.");
        }
    }
}
