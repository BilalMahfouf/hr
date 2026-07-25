using VeterinaryApi.Common.Results;

namespace VeterinaryApi.Common.Abstracions.Emails;

/// <summary>Contract for sending transactional emails (e.g., password-reset links).</summary>
public interface IEmailService
{
    /// <summary>Sends an email asynchronously using the supplied request data.</summary>
    /// <param name="request">The email recipient, subject, and HTML body.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A successful result, or a failure with an error message if sending fails.</returns>
    Task<Result> SendEmailAsync(SendEmailRequest request
        , CancellationToken cancellationToken);
}

/// <summary>DTO carrying the data required to send a single email.</summary>
/// <param name="To">Recipient email address.</param>
/// <param name="Subject">Email subject line.</param>
/// <param name="Body">HTML-formatted email body.</param>
public sealed record SendEmailRequest(string To, string Subject, string Body);

