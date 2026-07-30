using Resend;
using Modules.Shared.Abstracions.Emails;
using Modules.Shared.Errors;
using Modules.Shared.Results;

namespace PublicApi.Infrastructure.Services.Notifications;

public sealed class ResendEmailService(
    IResend resend,
    ILogger<ResendEmailService>logger) : IEmailService
{
    public async Task<Result> SendEmailAsync(
        SendEmailRequest request,
        CancellationToken cancellationToken)
    {
        var fromEmail = Environment.GetEnvironmentVariable("EMAIL_CONFIGURATIONS_EMAIL");
        if(fromEmail is null)
        {
            logger.LogError("Email configuration error: From email is" +
                " not configured in environment variables");
            return Result.Failure(
                Error
                .Failure(
                    "Email configuration error",
                    "From email is not configured in environment variables"));
        }

        try
        {
            logger.LogInformation("sending email");
            var message = new EmailMessage();
            message.From = fromEmail;
            message.Subject = request.Subject;
            message.To = request.To;
            message.HtmlBody = request.Body;

            await resend.EmailSendAsync(message, cancellationToken);
            logger.LogInformation("email send succsessfuly");
            return Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError($"Exception in the class {nameof(ResendEmailService)}" +
                $" in the function {nameof(SendEmailAsync)}.\n" +
                $"Ex: {ex}");
            return Result.Failure(Error.Failure("", ""));
        }
    }
}
