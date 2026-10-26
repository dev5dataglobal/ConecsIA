using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Application.Seguridad;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Infrastructure.Repositories;

public sealed class ModuloPermisoRepository(ConecsiaDbContext db) : GenericRepository<ModuloPermiso>(db), IModuloPermisoRepository
{
    public Task<List<ModuloPermiso>> ObtenerPorRolAsync(int idRol, CancellationToken cancellationToken = default) =>
        Context.ModulosPermisos.Where(x => x.IdRol == idRol).ToListAsync(cancellationToken);

    public Task<List<AppModulo>> ObtenerModulosDeRolAsync(int idRol, CancellationToken cancellationToken = default) =>
        (from asignacion in Context.ModulosPermisos
         join modulo in Context.AppModulos on asignacion.IdAppModulo equals modulo.IdAppModulo
         where asignacion.IdRol == idRol
         orderby modulo.Orden, modulo.Nombre
         select modulo).AsNoTracking().ToListAsync(cancellationToken);

    public async Task<List<string>> ObtenerCodigosDeUsuarioAsync(int idUsuario, int idOrganizacion, CancellationToken cancellationToken = default) =>
        (await ObtenerAccesosDeUsuarioAsync(idUsuario, idOrganizacion, cancellationToken)).Keys.ToList();

    public async Task<Dictionary<string, AccionesModulo>> ObtenerAccesosDeUsuarioAsync(int idUsuario, int idOrganizacion, CancellationToken cancellationToken = default)
    {
        // Filtro de organización explícito: no depende del contexto HTTP.
        var filas = await (from membresia in Context.MembresiasOrganizacion.IgnoreQueryFilters()
                           join membresiaRol in Context.MembresiasRoles.IgnoreQueryFilters()
                               on new { membresia.IdOrganizacion, membresia.IdMembresia } equals new { membresiaRol.IdOrganizacion, membresiaRol.IdMembresia }
                           join rol in Context.Roles.IgnoreQueryFilters()
                               on new { membresiaRol.IdOrganizacion, membresiaRol.IdRol } equals new { rol.IdOrganizacion, rol.IdRol }
                           join asignacion in Context.ModulosPermisos.IgnoreQueryFilters()
                               on new { rol.IdOrganizacion, rol.IdRol } equals new { asignacion.IdOrganizacion, asignacion.IdRol }
                           join modulo in Context.AppModulos on asignacion.IdAppModulo equals modulo.IdAppModulo
                           join organizacion in Context.Organizaciones on membresia.IdOrganizacion equals organizacion.IdOrganizacion
                           where membresia.IdUsuario == idUsuario
                                 && membresia.IdOrganizacion == idOrganizacion
                                 && (membresia.Estado == "Activa" || membresia.Estado == "Aceptada")
                                 && organizacion.Estado == "Activa"
                                 && rol.Activo
                                 && modulo.Activo
                           select new { modulo.Codigo, asignacion.PuedeConsultar, asignacion.PuedeAnadir, asignacion.PuedeEditar })
            .ToListAsync(cancellationToken);

        // Varios roles: unión por acción. Una asignación sin acciones equivale a no tener el módulo.
        var accesos = new Dictionary<string, AccionesModulo>(StringComparer.Ordinal);
        foreach (var fila in filas)
        {
            var acciones = new AccionesModulo(fila.PuedeConsultar, fila.PuedeAnadir, fila.PuedeEditar);
            accesos[fila.Codigo] = accesos.TryGetValue(fila.Codigo, out var previas) ? previas.Unir(acciones) : acciones;
        }
        foreach (var codigo in accesos.Where(x => !x.Value.Alguna).Select(x => x.Key).ToList()) accesos.Remove(codigo);
        return accesos;
    }
}
