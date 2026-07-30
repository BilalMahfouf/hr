using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Modules.Shared.Abstracions.Emails;
using Modules.Shared.Errors;
using Modules.Shared.Results;

namespace PublicApi.Infrastructure.Services.Notifications;

/// <summary>
/// Implementation of <see cref="IEmailService"/> that sends transactional HTML emails
/// via SMTP using MailKit with STARTTLS encryption.
/// </summary>
internal class EmailService : IEmailService
{
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<EmailService> _logger;

    /// <summary>Initializes the service with the configured email options.</summary>
    public EmailService(
        IOptions<EmailOptions> options,
        ILogger<EmailService> logger)
    {
        _emailOptions = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Sends an HTML email using the configured SMTP server.
    /// Connects, authenticates, sends, and disconnects gracefully.
    /// Catches all exceptions and returns a failure result rather than throwing.
    /// </summary>
    public async Task<Result> SendEmailAsync(SendEmailRequest request
        , CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"email options:{_emailOptions.Email}," +
                $"{_emailOptions.Port}, {_emailOptions.Password}," +
                $"{_emailOptions.Host}");

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_emailOptions.Email));
            email.To.Add(MailboxAddress.Parse(request.To));
            email.Subject = request.Subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = request.Body
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_emailOptions.Host, _emailOptions.Port
                , SecureSocketOptions.StartTls, cancellationToken);
            await smtp.AuthenticateAsync(_emailOptions.Email
                , _emailOptions.Password, cancellationToken);
            await smtp.SendAsync(email, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError($"An Error occured in email service {ex.Message}");
            _logger.LogError($"Ex:{ex}");

            var error = Error.Failure(
                "Email.Exception", ex.Message);
            return Result.Failure(error);
        }
    }
}