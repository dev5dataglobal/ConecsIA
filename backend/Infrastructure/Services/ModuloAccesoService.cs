using System.Collections.Concurrent;
using ConecsIA.Application.Interfaces;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Application.Seguridad;
using Microsoft.EntityFrameworkCore;
using ConecsIA.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace ConecsIA.Infrastructure.Services;

/// <summary>Tokens de invalidación compartidos (singleton) para la caché de módulos permitidos.</summary>
public sealed class ModuloAccesoCacheTokens
{
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _porOrganizacion = new();
    private CancellationTokenSource _global = new();

    public IChangeToken TokenOrganizacion(int idOrganizacion) =>
        new CancellationChangeToken(_porOrganizacion.GetOrAdd(idOrganizacion, _ => new CancellationTokenSource()).Token);

    public IChangeToken TokenGlobal() => new CancellationChangeToken(_global.Token);

    public void InvalidarOrganizacion(int idOrganizacion)
    {
        if (_porOrganizacion.TryRemove(idOrganizacion, out var fuente)) fuente.Cancel();
    }

    public void InvalidarTodo()
    {
        var anterior = Interlocked.Exchange(ref _global, new CancellationTokenSource());
        anterior.Cancel();
    }
}

public sealed class ModuloAccesoService(
    IModuloPermisoRepository modulosPermisos,
    ConecsiaDbContext db,
    IMemoryCache cache,
    ModuloAccesoCacheTokens tokens) : IModuloAccesoService
{
    private static readonly TimeSpan Duracion = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlySet<string>> ObtenerCodigosPermitidosAsync(int idUsuario, int idOrganizacion, bool esAdministradorGeneral, CancellationToken cancellationToken = default) =>
        (await ObtenerAccesosAsync(idUsuario, idOrganizacion, esAdministradorGeneral, cancellationToken)).Keys.ToHashSet(StringComparer.Ordinal);

    public async Task<IReadOnlyDictionary<string, AccionesModulo>> ObtenerAccesosAsync(int idUsuario, int idOrganizacion, bool esAdministradorGeneral, CancellationToken cancellationToken = default)
    {
        var clave = $"modulos:{idOrganizacion}:{idUsuario}:{(esAdministradorGeneral ? "admin" : "rol")}";
        if (cache.TryGetValue(clave, out IReadOnlyDictionary<string, AccionesModulo>? accesos) && accesos is not null) return accesos;

        accesos = esAdministradorGeneral
            // El administrador general accede a todos los módulos activos, con todas las acciones, sin depender de asignaciones.
            ? (await db.AppModulos.AsNoTracking().Where(x => x.Activo).Select(x => x.Codigo).ToListAsync(cancellationToken))
                .ToDictionary(x => x, _ => AccionesModulo.Todas, StringComparer.Ordinal)
            : await modulosPermisos.ObtenerAccesosDeUsuarioAsync(idUsuario, idOrganizacion, cancellationToken);

        var opciones = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = Duracion }
            .AddExpirationToken(tokens.TokenOrganizacion(idOrganizacion))
            .AddExpirationToken(tokens.TokenGlobal());
        cache.Set(clave, accesos, opciones);
        return accesos;
    }

    public async Task<bool> PuedeAsync(int idUsuario, int idOrganizacion, bool esAdministradorGeneral, string codigoModulo, AccionModulo accion, CancellationToken cancellationToken = default) =>
        (await ObtenerAccesosAsync(idUsuario, idOrganizacion, esAdministradorGeneral, cancellationToken)).TryGetValue(codigoModulo, out var acciones)
        && acciones.Permite(accion);

    public async Task<bool> TieneAccesoAsync(int idUsuario, int idOrganizacion, bool esAdministradorGeneral, string codigoModulo, CancellationToken cancellationToken = default) =>
        (await ObtenerAccesosAsync(idUsuario, idOrganizacion, esAdministradorGeneral, cancellationToken)).ContainsKey(codigoModulo);

    public void InvalidarOrganizacion(int idOrganizacion) => tokens.InvalidarOrganizacion(idOrganizacion);

    public void InvalidarTodo() => tokens.InvalidarTodo();
}
