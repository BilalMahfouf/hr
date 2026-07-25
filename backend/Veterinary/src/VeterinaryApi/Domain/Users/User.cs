using VeterinaryApi.Domain.Common;
using VeterinaryApi.Domain.Subscriptions;

namespace VeterinaryApi.Domain.Users;

/// <summary>
/// Represents an authenticated system user, typically a veterinary doctor.
/// A user is also the root tenant in the multi-tenant model — their <see cref="Entity.TenantId"/>
/// is set to their own <see cref="Entity.Id"/> at creation time.
/// </summary>
/// <remarks>
/// The <see cref="Create"/> factory method is used for admin-level user creation (inviting staff),
/// while <see cref="Register"/> is the self-registration path that defaults the role to
/// <see cref="UserRoles.Doctor"/>.
/// Passwords are stored as hashed values (Argon2) and are never stored in plain text.
/// </remarks>
public class User : Entity
{
    /// <summary>Gets the unique username chosen by the user for display and identification.</summary>
    public string UserName { get; private set; } = null!;

    /// <summary>Gets the user's first name.</summary>
    public string FirstName { get; private set; } = null!;

    /// <summary>Gets the user's last name.</summary>
    public string LastName { get; private set; } = null!;

    /// <summary>Gets the full display name computed from first and last name.</summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>Gets the user's email address, used for authentication and notifications.</summary>
    public string Email { get; private set; } = null!;

    /// <summary>
    /// Gets the Argon2-hashed password. Never expose this value in API responses.
    /// </summary>
    public string PasswordHash { get; private set; } = null!;

    /// <summary>Gets the role assigned to this user, controlling their feature access.</summary>
    public UserRoles Role { get; private set; }

    /// <summary>Gets a value indicating whether the user account is currently active.</summary>
    public bool IsActive { get; private set; }

    private readonly List<UserSession> _sessions = new List<UserSession>();

    public IReadOnlyCollection<Subscription> Subscriptions = new List<Subscription>();

    /// <summary>
    /// Gets a read-only collection of active and historical user sessions (refresh tokens).
    /// Each session represents a logged-in device/browser.
    /// </summary>
    public IReadOnlyCollection<UserSession> Sessions => _sessions.AsReadOnly();

    private User() { }

    /// <summary>
    /// Factory method for creating a user with an explicitly specified role.
    /// Used when an administrator creates a staff member account within a clinic.
    /// Sets <c>TenantId = Id</c> to establish the tenant boundary.
    /// </summary>
    /// <param name="firstName">The user's first name.</param>
    /// <param name="lastName">The user's last name.</param>
    /// <param name="email">The user's email address (must be unique in the system).</param>
    /// <param name="passwordHash">The pre-hashed Argon2 password string.</param>
    /// <param name="role">The role to assign to this user.</param>
    /// <returns>A new active <see cref="User"/> instance with <c>TenantId == Id</c>.</returns>
    public static User Create(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        UserRoles role
    )
    {
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = passwordHash,
            Email = email,
            Role = role,
            IsActive = true,
        };
        user.TenantId = user.Id;
        return user;
    }

    /// <summary>
    /// Factory method for self-registration. Creates a new user with the
    /// <see cref="UserRoles.Doctor"/> role and marks the account as active.
    /// </summary>
    /// <param name="userName">The chosen display username.</param>
    /// <param name="firstName">The user's first name.</param>
    /// <param name="lastName">The user's last name.</param>
    /// <param name="email">The user's email address (must be unique in the system).</param>
    /// <param name="passwordHash">The pre-hashed Argon2 password string.</param>
    /// <returns>A new active <see cref="User"/> instance with <c>Role = Doctor</c>.</returns>
    public static User Register(
        string userName,
        string firstName,
        string lastName,
        string email,
        string passwordHash
    )
    {
        var user = new User
        {
            UserName = userName,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = passwordHash,
            Email = email,
            Role = UserRoles.Doctor,
            IsActive = true,
        };
        user.TenantId = user.Id;
        return user;
    }

    /// <summary>
    /// Updates the user's password hash after verifying the minimum length of the raw password.
    /// </summary>
    /// <param name="password">The new raw (plain-text) password — used only for length validation.</param>
    /// <param name="newPasswordHash">The Argon2-hashed value of the new password to persist.</param>
    /// <exception cref="Domain.Common.DomainException">
    /// Thrown when the raw password is fewer than 6 characters.
    /// </exception>
    public void UpdatePassword(string password, string newPasswordHash)
    {
        if (password.Length < 6)
        {
            throw new DomainException(UserErrors.InvalidPasswordLength);
        }
        PasswordHash = newPasswordHash;
    }

    /// <summary>
    /// Updates the user's profile information (username and name fields).
    /// </summary>
    /// <param name="userName">The new username.</param>
    /// <param name="firstName">The new first name.</param>
    /// <param name="lastName">The new last name.</param>
    public void UpdateProfile(string userName, string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        UserName = userName;
    }

    public void UpdateEmail(string email)
    {
        Email = email;
    }

    public void ForgetPassword(string token, string clientUri)
    {
        var @event = new UserForgetPasswordDomainEvent(Id, Email, clientUri, token);
        @event.TenantId = TenantId;

        RaiseDomainEvent(@event);
    }
}
