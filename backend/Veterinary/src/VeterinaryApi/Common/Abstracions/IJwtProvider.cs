using VeterinaryApi.Domain.Users;

namespace VeterinaryApi.Common.Abstracions;

/// <summary>
/// Defines the contract for generating and inspecting JSON Web Tokens (JWT)
/// used for authenticating API requests.
/// Implemented by <c>JwtProvider</c> in the Infrastructure.Auth layer.
/// Registered as a scoped service with settings loaded from environment variables.
/// </summary>
public interface IJwtProvider
{
    /// <summary>
    /// Generates a signed JWT access token for the specified user.
    /// The token contains claims for the user's ID, username, and a unique token ID (<c>jti</c>).
    /// The token lifetime is controlled by the <c>JWT_ACCESS_TOKEN_LIFETIME_MINUTES</c> environment variable.
    /// </summary>
    /// <param name="user">The authenticated user for whom to generate the token.</param>
    /// <returns>A compact JWT string ready to include in an <c>Authorization: Bearer</c> header.</returns>
    public string GenerateToken(User user);

    /// <summary>
    /// Generates a cryptographically random refresh token (32 bytes, base64-encoded).
    /// Refresh tokens are stored as <c>UserSession</c> entities and used to obtain new access tokens
    /// without re-authentication.
    /// </summary>
    /// <returns>A secure random base64 string for use as a refresh token.</returns>
    public string GenerateRefreshToken();

    /// <summary>
    /// Returns the absolute UTC expiration time for a newly generated access token,
    /// based on the currently configured lifetime.
    /// </summary>
    /// <returns>A <see cref="DateTimeOffset"/> representing when the next token will expire.</returns>
    public DateTimeOffset GetTokenExpiration();
}
