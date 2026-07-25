using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Users;

namespace Application.IntegrationTests.Infrastructure;

public sealed class TestJwtProvider : IJwtProvider
{
    public string GenerateToken(User user)
    {
        return $"token-{user.Id:N}-{Guid.NewGuid():N}";
    }

    public string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString("N");
    }

    public DateTimeOffset GetTokenExpiration()
    {
        return DateTimeOffset.UtcNow.AddMinutes(15);
    }
}
