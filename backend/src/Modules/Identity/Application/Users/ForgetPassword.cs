using Identity.Abstracions;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.Abstracions.Emails;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Common.Util;

namespace Identity.Application.Users;

public static class ForgetPassword
{
    public record ForgetPasswordCommand(string Email, string ClientUri)
        : ICommand;

    public record Response();

    public class ForgetPasswordCommandHandler
        : ICommandHandler<ForgetPasswordCommand>
    {
        private readonly IIdentityApplicationDbContext _db;
        private readonly IJwtProvider _jwtProvider;
        private readonly IEmailService _emailService;

        public ForgetPasswordCommandHandler(
            IIdentityApplicationDbContext db,
            IJwtProvider jwtProvider,
            IEmailService emailService)
        {
            _db = db;
            _jwtProvider = jwtProvider;
            _emailService = emailService;
        }

        public async Task<Result> Handle(
            ForgetPasswordCommand command,
            CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
               .FirstOrDefaultAsync(e => e.Email == command.Email,
               cancellationToken);
            if (user is null)
            {
                return Result<Response>
                    .Failure(UserErrors.UserNotFound(command.Email));
            }
            var token = _jwtProvider.GenerateToken(user);
            var userSession = new UserSession()
            {
                UserId = user.Id,
                Token = token,
                TokenType = UserSessionTokenType.ResetPassword,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };
            user.ForgetPassword(token, command.ClientUri);
            _db.UserSessions.Add(userSession);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
    }

    public class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/forget-password", async (
                ForgetPasswordCommand command,
                ICommandHandler<ForgetPasswordCommand> handler,
                CancellationToken cancellationToken = default) =>
            {
                var result = await handler.Handle(command, cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.Problem();
            })
            .WithTags("Authentication")
            .WithSummary("Request password reset")
            .WithDescription("Sends a password reset email to the specified email address. The email contains a link with a reset token valid for 15 minutes.");
        }
    }
}
