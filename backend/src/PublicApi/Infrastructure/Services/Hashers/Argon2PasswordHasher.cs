using Isopoh.Cryptography.Argon2;
using Modules.Shared.Abstracions;

namespace PublicApi.Infrastructure.Services.Hashers;

/// <summary>
/// Implements <see cref="IPasswordHasher"/> using the Argon2 algorithm (via <c>Isopoh.Cryptography.Argon2</c>).
/// Argon2 is a memory-hard password hashing function recommended for modern authentication systems.
/// </summary>
public class Argon2PasswordHasher : IPasswordHasher
{
    /// <summary>Hashes a plain-text password using Argon2.</summary>
    /// <param name="password">The plain-text password to hash.</param>
    /// <returns>The Argon2 hash string.</returns>
    public string Hash(string password)
    {
        return Argon2.Hash(password);
    }

    /// <summary>Verifies a plain-text password against a stored Argon2 hash.</summary>
    /// <param name="password">The plain-text password to verify.</param>
    /// <param name="hash">The stored Argon2 hash to compare against.</param>
    /// <returns><c>true</c> if the password matches the hash; otherwise <c>false</c>.</returns>
    public bool Verify(string password, string hash)
    {
        return Argon2.Verify(hash, password);
    }
}
