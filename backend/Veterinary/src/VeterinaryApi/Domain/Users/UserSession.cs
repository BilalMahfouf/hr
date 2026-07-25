using VeterinaryApi.Domain.Common;

namespace VeterinaryApi.Domain.Users
{
    /// <summary>
    /// Represents a persisted authentication session token associated with a <see cref="User"/>.
    /// Used for refresh-token rotation and password-reset verification flows.
    /// </summary>
    /// <remarks>
    /// Each row corresponds to one issued session token. On refresh-token rotation, the expired
    /// session is deleted and a new one is inserted. Password-reset tokens share the same table
    /// but use <see cref="UserSessionTokenType.ResetPassword"/> as the discriminator.
    ///
    /// <b>Note:</b> Because <see cref="UserSession"/> inherits <see cref="Entity"/> it participates
    /// in soft-delete and tenant isolation, though tokens are typically deleted (hard) after use.
    /// </remarks>
    public class UserSession : Entity
    {
        /// <summary>Gets or sets the foreign key referencing the owning <see cref="User"/>.</summary>
        public Guid UserId { get; set; }

        /// <summary>Gets or sets the opaque token string (Base64 random bytes or GUID-based code).</summary>
        public string Token { get; set; } = null!;

        /// <summary>Gets or sets the type of this session token, distinguishing refresh from password-reset tokens.</summary>
        public UserSessionTokenType TokenType { get; set; }

        /// <summary>Gets or sets the UTC timestamp after which this token is no longer considered valid.</summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>Navigation property to the <see cref="User"/> that owns this session.</summary>
        public User User { get; set; } = null!;
    }

    /// <summary>
    /// Discriminates between the two types of session tokens stored in the <see cref="UserSession"/> table.
    /// </summary>
    public enum UserSessionTokenType : byte
    {
        /// <summary>A refresh token used to obtain new JWT access tokens without re-authentication.</summary>
        Refresh = 1,

        /// <summary>A one-time token sent via email to allow a user to reset their password.</summary>
        ResetPassword = 2,
    }
}

