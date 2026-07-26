using VeterinaryApi.Common.Results;

namespace VeterinaryApi.Common.Abstracions.Emails;

public interface IEmailService
{
    Task<Result> SendEmailAsync(SendEmailRequest request
        , CancellationToken cancellationToken);
}

public sealed record SendEmailRequest(string To, string Subject, string Body);
