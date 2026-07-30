namespace PublicApi.Infrastructure.Services.Notifications;

/// <summary>
/// Strongly-typed configuration options for the SMTP email service.
/// Populated from environment variables and <c>EMAIL_CONFIGURATIONS</c> config section.
/// </summary>
internal class EmailOptions
{
    /// <summary>SMTP server hostname (e.g. <c>"smtp.gmail.com"</c>).</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>SMTP server port (e.g. 587 for STARTTLS).</summary>
    public int Port { get; set; }

    /// <summary>Sender email address used for outgoing messages.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>SMTP authentication password.</summary>
    public string Password { get; set; } = string.Empty;
}
