using Modules.Shared.Domain.Common;

namespace Modules.Identity.Domain.Users;

public class User : Entity
{
    public string UserName { get; private set; } = null!;

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public string FullName => $"{FirstName} {LastName}";

    public string Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public UserRoles Role { get; private set; }

    public bool IsActive { get; private set; }

    private readonly List<UserSession> _sessions = new List<UserSession>();

    public IReadOnlyCollection<UserSession> Sessions => _sessions.AsReadOnly();

    private User() { }

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
        return user;
    }

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
        return user;
    }

    public void UpdatePassword(string password, string newPasswordHash)
    {
        if (password.Length < 6)
        {
            throw new DomainException(UserErrors.InvalidPasswordLength);
        }
        PasswordHash = newPasswordHash;
    }

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

        RaiseDomainEvent(@event);
    }
}
