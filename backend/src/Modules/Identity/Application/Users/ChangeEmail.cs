using FluentValidation;
using Modules.Identity.Abstracions;
using Modules.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Modules.Shared.Abstracions;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Identity.Application.Users;

public static class ChangeEmail
{
    public sealed record ChangeEmailCommand(string Email) : ICommand;

    public sealed class Validator : AbstractValidator<ChangeEmailCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();
        }
    }

    public sealed class ChangeEmailCommandHandler
        : ICommandHandler<ChangeEmailCommand>
    {
        private readonly IIdentityApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IValidator<ChangeEmailCommand> _validator;

        public ChangeEmailCommandHandler(
            IIdentityApplicationDbContext db,
            ICurrentUser currentUser,
            IValidator<ChangeEmailCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task<Result> Handle(
            ChangeEmailCommand command,
            CancellationToken cancellationToken)
        {
            _validator.ValidateAndThrow(command);

            var isEmailInUse = await _db.Users
                .AnyAsync(u => u.Email == command.Email);
            if (isEmailInUse)
            {
                return Result.Failure(UserErrors.EmailAlreadyInUse(command.Email));
            }

            var user = await _db.Users
                .FirstOrDefaultAsync(
                e => e.Id == _currentUser.UserId,
                cancellationToken);
            if (user is null)
            {
                return Result.Failure(UserErrors.UserNotFound(command.Email));
            }
            user.UpdateEmail(command.Email);
            _db.Users.Update(user);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPatch("/change-email", async (
                ChangeEmailCommand command,
                ICommandHandler<ChangeEmailCommand> handler,
                CancellationToken ct = default) =>
            {
                var result = await handler.Handle(command, ct);
                return result.IsSuccess ? Results.NoContent() : result.Problem();
            }).RequireAuthorization()
            .WithTags($"{nameof(User)}s")
            .WithSummary("Change email")
            .WithDescription("Updates the authenticated user's email address. Requires the new email to be unique.");
        }
    }
}
