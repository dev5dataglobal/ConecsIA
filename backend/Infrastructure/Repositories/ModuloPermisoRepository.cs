using ConecsIA.Application.Interfaces.Repositories;
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

    public Task<List<string>> ObtenerCodigosDeUsuarioAsync(int idUsuario, int idOrganizacion, CancellationToken cancellationToken = default) =>
        // Filtro de organización explícito: no depende del contexto HTTP.
        (from membresia in Context.MembresiasOrganizacion.IgnoreQueryFilters()
         join membresiaRol in Context.MembresiasRoles.IgnoreQueryFilters()
             on new { membresia.IdOrganizacion, membresia.IdMembresia } equals new { membresiaRol.IdOrganizacion, membresiaRol.IdMembresia }
         join rol in Context.Roles.IgnoreQueryFilters()
             on new { membresiaRol.IdOrganizacion, membresiaRol.IdRol } equals new { rol.IdOrganizacion, rol.IdRol }
         join asignacion in Context.ModulosPermisos.IgnoreQueryFilters()
             on new { rol.IdOrganizacion, rol.IdRol } equals new { asignacion.IdOrganizacion, asignacion.IdRol }
         join modulo in Context.AppModulos on asignacion.IdAppModulo equals modulo.IdAppModulo
         where membresia.IdUsuario == idUsuario
               && membresia.IdOrganizacion == idOrganizacion
               && (membresia.Estado == "Activa" || membresia.Estado == "Aceptada")
               && rol.Activo
               && modulo.Activo
         select modulo.Codigo).Distinct().ToListAsync(cancellationToken);
}
