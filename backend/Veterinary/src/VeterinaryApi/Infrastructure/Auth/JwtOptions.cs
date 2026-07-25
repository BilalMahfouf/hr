namespace VeterinaryApi.Infrastructure.Auth;

/// <summary>
/// Strongly-typed configuration options for JWT token generation and validation.
/// Bound from the <c>Jwt</c> section of <c>appsettings.json</c> (or environment variables)
/// via the ASP.NET Core options pattern.
/// </summary>
/// <remarks>
/// These values are validated at runtime inside <c>JwtProvider.ValidateSettings()</c>:
/// the signing key must be at least 32 characters, and the lifetime must be positive.
///
/// <b>Security:</b> Never store the <see cref="SingingKey"/> in source control.
/// Use environment variables or a secrets manager (Azure Key Vault, AWS Secrets Manager, etc.).
///
/// <b>Typo:</b> <see cref="SingingKey"/> is a known spelling error; it should be
/// <c>SigningKey</c>. Correcting it requires coordinated changes in both this class and
/// <c>appsettings.json</c> / environment variable names.
/// </remarks>
public class JwtOptions
{
    /// <summary>Gets or sets the token issuer (<c>iss</c> claim).</summary>
    /// <value>Should match the issuer configured in the JWT bearer middleware validation parameters.</value>
    public string Issuer { get; set; } = null!;

    /// <summary>Gets or sets the intended audience (<c>aud</c> claim).</summary>
    /// <value>Should match the audience configured in the JWT bearer middleware validation parameters.</value>
    public string Audience { get; set; } = null!;

    /// <summary>Gets or sets the access token lifetime in <b>minutes</b>.</summary>
    /// <value>Must be greater than zero. Typical values: 15–60 minutes.</value>
    public byte LifeTime { get; set; }

    /// <summary>Gets or sets the HMAC-SHA256 signing key.</summary>
    /// <value>Must be at least 32 characters (256 bits) long to satisfy HMAC-SHA256 requirements.</value>
    /// <remarks>⚠️ Property name contains a typo (<c>Singing</c> instead of <c>Signing</c>).</remarks>
    public string SingingKey { get; set; } = null!;
}
