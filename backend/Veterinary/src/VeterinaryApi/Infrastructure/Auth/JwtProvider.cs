using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Users;

namespace VeterinaryApi.Infrastructure.Auth;

/// <summary>
/// Concrete implementation of <see cref="IJwtProvider"/> that issues and validates
/// JWT access tokens and opaque refresh tokens for authenticated users.
/// </summary>
/// <remarks>
/// <b>Access token</b> characteristics:
/// <list type="bullet">
///   <item>Algorithm: HMAC-SHA256 (symmetric)</item>
///   <item>Claims: <c>NameIdentifier</c>, <c>Name</c>, <c>sub</c>, <c>jti</c>, <c>iat</c></item>
///   <item>Expiry: configurable via <see cref="JwtOptions.LifeTime"/> (minutes)</item>
/// </list>
///
/// <b>Refresh token</b>: 32 cryptographically-random bytes encoded as Base64.
/// It is stored server-side as a <c>UserSession</c> entity and validated on rotation.
///
/// All configuration is loaded from <see cref="JwtOptions"/> via the options pattern.
/// Settings are validated before each token generation call so that misconfiguration
/// surfaces at runtime rather than silently producing invalid tokens.
/// </remarks>
internal class JwtProvider : IJwtProvider
{
    private readonly JwtOptions _jwtOptions;

    /// <summary>
    /// Initializes the provider with bound JWT configuration options.
    /// </summary>
    /// <param name="jwtOptions">Bound options containing issuer, audience, signing key, and lifetime.</param>
    public JwtProvider(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    /// <summary>
    /// Generates a signed JWT access token for the specified user.
    /// </summary>
    /// <param name="user">The authenticated user whose identity will be embedded in the token claims.</param>
    /// <returns>A compact serialized JWT string (header.payload.signature).</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the JWT configuration is invalid — e.g., missing or too-short signing key,
    /// missing issuer/audience, or non-positive lifetime.
    /// </exception>
    public string GenerateToken(User user)
    {
        ValidateSettings();
        var claims = GetUserClaims(user);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SingingKey));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
            null,
            DateTime.UtcNow.AddMinutes(_jwtOptions.LifeTime),
            signingCredentials);

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);
        return tokenValue;
    }

    /// <summary>
    /// Validates that all required JWT settings are present and meet minimum security requirements.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the signing key is absent or shorter than 32 characters,
    /// the issuer or audience is absent, or the token lifetime is non-positive.
    /// </exception>
    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_jwtOptions.SingingKey))
            throw new InvalidOperationException("JWT Secret is not configured");

        if (_jwtOptions.SingingKey.Length < 32)
            throw new InvalidOperationException("JWT Secret must be at least 32 characters long");

        if (string.IsNullOrWhiteSpace(_jwtOptions.Issuer))
            throw new InvalidOperationException("JWT Issuer is not configured");

        if (string.IsNullOrWhiteSpace(_jwtOptions.Audience))
            throw new InvalidOperationException("JWT Audience is not configured");

        if (_jwtOptions.LifeTime <= 0)
            throw new InvalidOperationException("JWT Lifetime must be greater than 0");
    }

    /// <summary>
    /// Returns the absolute UTC expiration time of an access token issued right now,
    /// based on the configured <see cref="JwtOptions.LifeTime"/>.
    /// </summary>
    /// <returns>A <see cref="DateTimeOffset"/> representing the token expiry instant.</returns>
    public DateTimeOffset GetTokenExpiration()
    {
        return DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.LifeTime);
    }

    /// <summary>
    /// Builds the standard claims payload for the given user.
    /// </summary>
    /// <param name="user">The user whose identity is encoded.</param>
    /// <returns>
    /// A list containing <c>NameIdentifier</c>, <c>Name</c>, <c>sub</c>, <c>jti</c>, and <c>iat</c> claims.
    /// </returns>
    private List<Claim> GetUserClaims(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };
        return claims;
    }

    /// <summary>
    /// Generates a cryptographically secure opaque refresh token.
    /// </summary>
    /// <returns>A Base64-encoded string of 32 random bytes (256 bits of entropy).</returns>
    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
