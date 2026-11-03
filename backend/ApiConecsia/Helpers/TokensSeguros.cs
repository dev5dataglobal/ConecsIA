using System.Security.Cryptography;
using System.Text;

namespace ApiConecsia.Helpers;

/// <summary>Tokens opacos (256 bits, base64url) y su huella SHA-256: la base de datos nunca guarda el token en claro.</summary>
public static class TokensSeguros
{
    public static string Generar() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
