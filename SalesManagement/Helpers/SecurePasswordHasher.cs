using System.Security.Cryptography;

namespace SalesManagement.Helpers;

public static class SecurePasswordHasher
{
    private const int Iterations = 100_000;     // OWASP recommendation
    private const int SaltSize = 16;            // 128 bits
    private const int HashSize = 32;            // 256 bits

    /// <summary>
    /// Gera hash PBKDF2 no formato: iterations.base64(salt).base64(hash)
    /// </summary>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Senha não pode ser vazia.", nameof(password));

        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        var saltB64 = Convert.ToBase64String(salt);
        var hashB64 = Convert.ToBase64String(hash);

        return $"{Iterations}.{saltB64}.{hashB64}";
    }

    /// <summary>
    /// Verifica se a senha corresponde ao hash PBKDF2.
    /// Também aceita hashes SHA256 antigos para migração gradual.
    /// </summary>
    public static bool VerifyPassword(string password, string? hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        // 🔧 Compatibilidade com hash antigo SHA256 (migração gradual)
        if (!hash.Contains('.'))
        {
            var oldHash = Sha256Legacy(password);
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(oldHash),
                Convert.FromHexString(hash));
        }

        var parts = hash.Split('.', 3);
        if (parts.Length != 3 ||
            !int.TryParse(parts[0], out var iterations) ||
            iterations < 1)
            return false;

        var salt = Convert.FromBase64String(parts[1]);
        var expectedHash = Convert.FromBase64String(parts[2]);

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static string Sha256Legacy(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}