namespace VeterinaryApi.Domain.Common;

/// <summary>
/// Value object encapsulating an email address string.
/// Constructed exclusively via <see cref="Create"/>.
/// TODO: add email format validation in <see cref="Create"/>.
/// </summary>
public class Email
{
    /// <summary>The raw email address string.</summary>
    public string Value { get; set; } = string.Empty;

    private Email()
    {
    }

    private Email(string value)
    {
        this.Value = value;
    }

    /// <summary>Factory method that constructs a new <see cref="Email"/> value object.</summary>
    /// <param name="address">The raw email address string.</param>
    public static Email Create(string address)
    {
        // to do add email validation here
        return new Email(address);
    }
}
