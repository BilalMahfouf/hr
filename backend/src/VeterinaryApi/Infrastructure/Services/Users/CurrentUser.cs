using System.Security.Claims;
using Shared.Abstracions;

namespace VeterinaryApi.Infrastructure.Services.Users;

/// <summary>
/// ASP.NET Core implementation of <see cref="ICurrentTenant"/> that resolves the authenticated
/// user's identifier from the active HTTP request's claims principal.
/// </summary>
/// <remarks>
/// The service reads the <see cref="ClaimTypes.NameIdentifier"/> claim written by
/// <see cref="Infrastructure.Auth.JwtProvider"/> during token generation. It relies on
/// <see cref="IHttpContextAccessor"/> which is registered as a singleton by the framework.
///
/// Returns <c>null</c> when:
/// <list type="bullet">
///   <item>There is no active HTTP context (e.g., background jobs).</item>
///   <item>The request is unauthenticated (no JWT bearer token).</item>
///   <item>The <c>NameIdentifier</c> claim is absent or not a valid <see cref="Guid"/>.</item>
/// </list>
///
/// Callers (interceptors, handlers) must guard against the <c>null</c> case explicitly.
/// </remarks>
internal class CurrentUserService : ICurrentTenant
{
    private readonly IHttpContextAccessor _contextAccessor;

    /// <summary>
    /// Initializes the service with the current HTTP context accessor.
    /// </summary>
    /// <param name="contextAccessor">The accessor providing the ambient <see cref="HttpContext"/>.</param>
    public CurrentUserService(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    /// Gets the authenticated user's unique identifier, or <c>null</c> if unavailable.
    /// </summary>
    /// <value>
    /// A <see cref="Guid"/> parsed from the <c>ClaimTypes.NameIdentifier</c> claim,
    /// or <c>null</c> if the HTTP context, user, or claim is absent.
    /// </value>
    public Guid? UserId
    {
        get
        {
            var userId = _contextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userId, out Guid id) ? id : null;
        }
    }
}
