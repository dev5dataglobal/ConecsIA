using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Tiempo;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Api.Semillas;

/// <summary>
/// HU-017: copia las plantillas de rol en las organizaciones sintéticas y asigna el de
/// Administrador de organización a sus administradores y el de Operador de acceso al operador.
/// Idempotente: una organización que ya tiene roles no se toca.
/// </summary>
public sealed class SemillasRoles(ConecsIADbContext contexto, IClock reloj) : IConjuntoSemillas
{
    public async Task SembrarAsync(CancellationToken cancelacion)
    {
        (Guid Organizacion, Guid[] Administradores, Guid[] Operadores)[] organizaciones =
        [
            (SemillasPrimeraVertical.OrgPinos, [SemillasPrimeraVertical.AdminPinos], [SemillasPrimeraVertical.OperadorPinos]),
            (SemillasPrimeraVertical.OrgAndes, [SemillasPrimeraVertical.AdminAndes], []),
        ];

        foreach (var (organizacion, administradores, operadores) in organizaciones)
        {
            if (await contexto.Set<Rol>().AnyAsync(r => r.OrganizacionId == organizacion, cancelacion)) continue;

            var roles = PlantillasRol.Todas.ToDictionary(
                p => p.Clave,
                p => Rol.Crear(organizacion, p.Nombre, p.Descripcion, p.Permisos, p.Clave, reloj.UtcNow));
            contexto.AddRange(roles.Values);

            foreach (var membresia in await contexto.Set<Membresia>()
                         .Where(m => m.OrganizacionId == organizacion).ToListAsync(cancelacion))
            {
                if (administradores.Contains(membresia.UsuarioId)) membresia.AsignarRoles([roles["administrador-organizacion"].Id]);
                if (operadores.Contains(membresia.UsuarioId)) membresia.AsignarRoles([roles["operador-acceso"].Id]);
            }
        }

        await contexto.SaveChangesAsync(cancelacion);
    }
}
