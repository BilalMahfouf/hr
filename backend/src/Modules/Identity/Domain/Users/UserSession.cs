using Modules.Shared.Domain.Common;

namespace Modules.Identity.Domain.Users
{
    public class UserSession : Entity
    {
        public Guid UserId { get; set; }

        public string Token { get; set; } = null!;

        public UserSessionTokenType TokenType { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public User User { get; set; } = null!;
    }

    public enum UserSessionTokenType : byte
    {
        Refresh = 1,

        ResetPassword = 2,
    }
}
