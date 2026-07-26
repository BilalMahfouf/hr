namespace VeterinaryApi.Domain.Common;

/// <summary>
/// Value object encapsulating a person's first and last name, with a computed full-name property.
/// Constructed exclusively via <see cref="Create"/>.
/// </summary>
public class Name
{
    /// <summary>The person's first (given) name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>The person's last (family) name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Computed full name in the format <c>"FirstName LastName"</c>.</summary>
    public string FullName => $"{FirstName} {LastName}";

    private Name()
    {
    }

    private Name(string firstName, string lastName)
    {
        this.FirstName = firstName;
        this.LastName = lastName;
    }

    /// <summary>Factory method that constructs a new <see cref="Name"/> value object.</summary>
    /// <param name="firstName">The first name.</param>
    /// <param name="lastName">The last name.</param>
    public static Name Create(string firstName, string lastName)
    {
        return new Name(firstName, lastName);
    }
}
