namespace VeterinaryApi.Domain.Users;

/// <summary>
/// Defines the authorization roles available to a <see cref="User"/> in the system.
/// </summary>
/// <remarks>
/// Role values are stored as bytes in the database.
/// The role is embedded in the JWT bearer token so that authorization policies can be applied
/// per endpoint without an additional database query.
/// </remarks>
public enum UserRoles : byte
{
    /// <summary>Full administrative access — can manage users, clinics, and all tenant data.</summary>
    Admin = 1,

    /// <summary>Veterinary professional with access to patient records, visits, and prescriptions.</summary>
    Doctor = 2,
}
