using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Identidad.Application;
using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Api.Semillas;

/// <summary>
/// HU-006: contraseña local para los usuarios sintéticos, para probar el inicio de sesión real en
/// Development. El valor llega por configuración (<c>Semillas__ContrasenaUsuarios</c> en .env) y
/// nunca se versiona. Sin valor no hace nada; solo toca usuarios sintéticos sin contraseña.
/// </summary>
public sealed partial class SemillasContrasenas(
    ConecsIADbContext contexto,
    IHasherContrasenas hasher,
    IConfiguration configuracion,
    ILogger<SemillasContrasenas> logger) : IConjuntoSemillas
{
    public const string Clave = "Semillas:ContrasenaUsuarios";

    private static readonly Guid[] Sinteticos =
    [
        SemillasPrimeraVertical.AdminPinos,
        SemillasPrimeraVertical.OperadorPinos,
        SemillasPrimeraVertical.AdminAndes,
        SemillasPlanoSaas.Superadministrador,
    ];

    public async Task SembrarAsync(CancellationToken cancelacion)
    {
        var contrasena = configuracion[Clave];
        if (string.IsNullOrEmpty(contrasena)) return;
        if (contrasena.Length < PoliticaAutenticacion.LongitudMinimaContrasena)
        {
            RegistrarContrasenaCorta(logger, PoliticaAutenticacion.LongitudMinimaContrasena);
            return;
        }

        var usuarios = await contexto.Set<Usuario>()
            .Where(u => Sinteticos.Contains(u.Id) && u.HashContrasena == null)
            .ToListAsync(cancelacion);
        foreach (var usuario in usuarios) usuario.EstablecerContrasena(hasher.Hashear(contrasena));
        await contexto.SaveChangesAsync(cancelacion);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Semillas: la contraseña de prueba debe tener al menos {Minimo} caracteres; no se asignó")]
    private static partial void RegistrarContrasenaCorta(ILogger logger, int minimo);
}
