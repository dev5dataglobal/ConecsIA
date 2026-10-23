using ConecsIA.Application.Interfaces;
using ConecsIA.Domain.Entities;

namespace ConecsIA.Application.Interfaces.Repositories;

public interface IModuloPermisoRepository : IGenericRepository<ModuloPermiso>
{
    /// <summary>Asignaciones de un rol (respeta el filtro de organización del contexto).</summary>
    Task<List<ModuloPermiso>> ObtenerPorRolAsync(int idRol, CancellationToken cancellationToken = default);
    /// <summary>Módulos asignados a un rol con su catálogo, ordenados.</summary>
    Task<List<AppModulo>> ObtenerModulosDeRolAsync(int idRol, CancellationToken cancellationToken = default);
    /// <summary>Códigos de módulos activos permitidos a un usuario en una organización (membresía y roles activos).</summary>
    Task<List<string>> ObtenerCodigosDeUsuarioAsync(int idUsuario, int idOrganizacion, CancellationToken cancellationToken = default);
}
