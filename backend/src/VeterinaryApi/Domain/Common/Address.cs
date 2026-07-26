namespace VeterinaryApi.Domain.Common;

/// <summary>
/// Value object representing a physical address.
/// Constructed exclusively via <see cref="Create"/>; use a private constructor to enforce invariants.
/// </summary>
public sealed class Address
{
    /// <summary>Street name and number.</summary>
    public string Street { get; private set; } = null!;
    /// <summary>City or municipality.</summary>
    public string City { get; private set; } = null!;
    /// <summary>State or province.</summary>
    public string State { get; private set; } = null!;
    /// <summary>Postal or ZIP code.</summary>
    public string ZipCode { get; private set; } = null!;

    private Address()
    {
    }

    private Address(string street,
        string city,
        string state,
        string zipCode)
    {
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
    }

    /// <summary>Factory method that constructs a new <see cref="Address"/> value object.</summary>
    public static Address Create(
        string street,
        string city,
        string state,
        string zipCode)
    {
        // Add any necessary validation here
        return new Address(street, city, state, zipCode);
    }
}