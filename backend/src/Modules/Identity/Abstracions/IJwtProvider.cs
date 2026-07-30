using Modules.Identity.Domain.Users;

namespace Modules.Identity.Abstracions;

public interface IJwtProvider
{
    public string GenerateToken(User user);

    public string GenerateRefreshToken();

    public DateTimeOffset GetTokenExpiration();
}
