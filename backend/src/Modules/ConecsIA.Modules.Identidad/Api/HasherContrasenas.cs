using ConecsIA.Modules.Identidad.Application;
using ConecsIA.Modules.Identidad.Domain;
using Microsoft.AspNetCore.Identity;

namespace ConecsIA.Modules.Identidad.Api;

/// <summary>
/// Hash de contraseñas con el <see cref="PasswordHasher{TUser}"/> de ASP.NET Core Identity (ADR-006).
/// Vive en la capa Api porque depende de ASP.NET Core (ADR-001).
/// </summary>
internal sealed class HasherContrasenas : IHasherContrasenas
{
    private static readonly PasswordHasher<Usuario> Hasher = new();

    public string Hashear(string contrasena) => Hasher.HashPassword(null!, contrasena);

    public (bool Valida, bool RequiereRehash) Verificar(string hash, string contrasena) =>
        Hasher.VerifyHashedPassword(null!, hash, contrasena) switch
        {
            PasswordVerificationResult.Success => (true, false),
            PasswordVerificationResult.SuccessRehashNeeded => (true, true),
            _ => (false, false),
        };
}
