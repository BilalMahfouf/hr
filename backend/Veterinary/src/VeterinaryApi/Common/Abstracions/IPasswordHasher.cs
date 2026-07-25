namespace VeterinaryApi.Common.Abstracions;

/// <summary>
/// Defines the contract for password hashing and verification.
/// The current implementation uses the Argon2 algorithm (via <c>Isopoh.Cryptography.Argon2</c>),
/// which is the OWASP-recommended algorithm for secure password storage as of 2024.
/// Registered as a singleton because the hashing algorithm is completely stateless.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes the given plain-text password using the Argon2 algorithm.
    /// The resulting hash includes the salt and algorithm parameters, making it self-contained.
    /// </summary>
    /// <param name="password">The plain-text password to hash. Should not be stored or logged.</param>
    /// <returns>An Argon2 hash string that encodes the salt, parameters, and hash value.</returns>
    string Hash(string password);

    /// <summary>
    /// Verifies that the provided plain-text password matches the stored hash.
    /// This operation is intentionally slow (constant-time comparison) to resist timing attacks.
    /// </summary>
    /// <param name="password">The plain-text password to verify.</param>
    /// <param name="hash">The stored Argon2 hash to compare against.</param>
    /// <returns><c>true</c> if the password matches the hash; otherwise, <c>false</c>.</returns>
    bool Verify(string password, string hash);
}
