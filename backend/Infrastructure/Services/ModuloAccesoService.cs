using System.Collections.Concurrent;
using ConecsIA.Application.Interfaces;
using ConecsIA.Application.Interfaces.Repositories;
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

    public async Task<IReadOnlySet<string>> ObtenerCodigosPermitidosAsync(int idUsuario, int idOrganizacion, bool esAdministradorGeneral, CancellationToken cancellationToken = default)
    {
        var clave = $"modulos:{idOrganizacion}:{idUsuario}:{(esAdministradorGeneral ? "admin" : "rol")}";
        if (cache.TryGetValue(clave, out IReadOnlySet<string>? codigos) && codigos is not null) return codigos;

        var lista = esAdministradorGeneral
            // El administrador general accede a todos los módulos activos sin depender de asignaciones.
            ? await db.AppModulos.AsNoTracking().Where(x => x.Activo).Select(x => x.Codigo).ToListAsync(cancellationToken)
            : await modulosPermisos.ObtenerCodigosDeUsuarioAsync(idUsuario, idOrganizacion, cancellationToken);
        codigos = lista.ToHashSet(StringComparer.Ordinal);

        var opciones = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = Duracion }
            .AddExpirationToken(tokens.TokenOrganizacion(idOrganizacion))
            .AddExpirationToken(tokens.TokenGlobal());
        cache.Set(clave, codigos, opciones);
        return codigos;
    }

    public async Task<bool> TieneAccesoAsync(int idUsuario, int idOrganizacion, bool esAdministradorGeneral, string codigoModulo, CancellationToken cancellationToken = default) =>
        (await ObtenerCodigosPermitidosAsync(idUsuario, idOrganizacion, esAdministradorGeneral, cancellationToken)).Contains(codigoModulo);

    public void InvalidarOrganizacion(int idOrganizacion) => tokens.InvalidarOrganizacion(idOrganizacion);

    public void InvalidarTodo() => tokens.InvalidarTodo();
}
