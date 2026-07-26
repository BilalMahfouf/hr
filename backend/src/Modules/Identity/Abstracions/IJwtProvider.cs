using Identity.Domain.Users;

namespace Identity.Abstracions;

public interface IJwtProvider
{
    public string GenerateToken(User user);

    public string GenerateRefreshToken();

    public DateTimeOffset GetTokenExpiration();
}
