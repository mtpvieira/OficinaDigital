using System.Security.Cryptography;
using OficinaDigital.Application.Seguranca;

namespace OficinaDigital.Infrastructure.Seguranca;

public class PasswordHasher : IPasswordHasher
{
    private const int TamanhoDoSalt = 16;
    private const int TamanhoDoHash = 32;
    private const int Iteracoes = 210_000; // Recomendação OWASP para PBKDF2-SHA256.

    public (string Hash, string Salt) Gerar(string senha)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(senha);

        var salt = RandomNumberGenerator.GetBytes(TamanhoDoSalt);
        var hash = Derivar(senha, salt);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verificar(string senha, string hash, string salt)
    {
        if (string.IsNullOrWhiteSpace(senha) || string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(salt))
            return false;

        try
        {
            var esperado = Convert.FromBase64String(hash);
            var calculado = Derivar(senha, Convert.FromBase64String(salt));

            return CryptographicOperations.FixedTimeEquals(esperado, calculado);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Derivar(string senha, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, TamanhoDoHash);
}
