namespace Modules.Identity.Infrastructure.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = null!;

    public string Audience { get; set; } = null!;

    public byte LifeTime { get; set; }

    public string SingingKey { get; set; } = null!;
}
