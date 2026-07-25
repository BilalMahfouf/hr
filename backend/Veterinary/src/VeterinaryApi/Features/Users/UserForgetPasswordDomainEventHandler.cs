using VeterinaryApi.Common.Abstracions.Emails;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Util;
using VeterinaryApi.Domain.Users;

namespace VeterinaryApi.Features.Users;

public sealed class UserForgetPasswordDomainEventHandler(
    IEmailService emailService,
    ILogger<UserForgetPasswordDomainEventHandler>logger)
    : IDomainEventHandler<UserForgetPasswordDomainEvent>
{

    public async Task Handle(
        UserForgetPasswordDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        try
        {

        logger.LogInformation(logger.IsEnabled(LogLevel.Information)
            ? "Handling UserForgetPasswordDomainEvent for user {UserId} with email {Email}"
            : "Handling UserForgetPasswordDomainEvent", domainEvent.UserId, domainEvent.Email);
        var link = Utility.GenerateResponseLink(
            domainEvent.Email, domainEvent.Token, domainEvent.ClientUri);
        var body = $@"
                            <p>Click here to reset your password:</p>
                            <a href=""{link}"">Reset Password</a>";
            logger.LogInformation("Generated password reset link for user {UserId}: {Link}", domainEvent.UserId, link);

        var message = new SendEmailRequest(domainEvent.Email, "Reset Password", body);
        var result = await emailService.SendEmailAsync(message, cancellationToken);
            logger.LogInformation($"result: {result.Error.ToString()}");

        logger.LogInformation(logger.IsEnabled(LogLevel.Information)
            ? "Sent password reset email to {Email} for user {UserId}"
            : "Sent password reset email", domainEvent.Email, domainEvent.UserId);


        }
        catch(Exception ex)
        {
            logger.LogError(ex, logger.IsEnabled(LogLevel.Error)
                ? "Error handling UserForgetPasswordDomainEvent for user {UserId} with email {Email}: {ErrorMessage}"
                : "Error handling UserForgetPasswordDomainEvent", domainEvent.UserId, domainEvent.Email, ex.Message);
        }
    }

}
