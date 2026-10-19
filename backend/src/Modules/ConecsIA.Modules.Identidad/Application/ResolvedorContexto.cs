using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Organizaciones;

namespace ConecsIA.Modules.Identidad.Application;

public enum EstadoResolucion
{
    Resuelto,

    /// <summary>Usuario inexistente o inactivo: 401.</summary>
    UsuarioNoValido,

    /// <summary>Sin membresía activa en una organización activa: 403.</summary>
    SinMembresia,

    /// <summary>Se seleccionó una organización donde no tiene membresía: 403 auditado.</summary>
    OrganizacionNoPermitida,

    /// <summary>Varias membresías y ninguna seleccionada. La política de multi-membresía depende de D-03.</summary>
    OrganizacionNoSeleccionada,
}

public sealed record ResultadoResolucion(EstadoResolucion Estado, Usuario? Usuario = null, Membresia? Membresia = null);

public interface IRepositorioIdentidad
{
    Task<Usuario?> ObtenerUsuarioAsync(Guid id, CancellationToken cancelacion);

    Task<IReadOnlyList<Membresia>> MembresiasActivasAsync(Guid usuarioId, CancellationToken cancelacion);

    Task<IReadOnlyList<Usuario>> UsuariosActivosAsync(CancellationToken cancelacion);

    /// <summary>Usuario con seguimiento de cambios, para registrar intentos y bloqueo.</summary>
    Task<Usuario?> ObtenerPorCorreoParaActualizarAsync(string correoNormalizado, CancellationToken cancelacion);

    void AgregarSesion(SesionUsuario sesion);

    Task<SesionUsuario?> ObtenerSesionAsync(Guid sesionId, CancellationToken cancelacion);

    Task<SesionUsuario?> ObtenerSesionParaActualizarAsync(Guid sesionId, CancellationToken cancelacion);

    Task<IReadOnlyList<SesionUsuario>> SesionesVigentesAsync(Guid usuarioId, DateTimeOffset ahora, CancellationToken cancelacion);
}

/// <summary>
/// HU-007 (ADR-005): deriva la organización de una membresía activa del usuario autenticado. Una
/// organización indicada por el cliente solo selecciona entre membresías verificadas.
/// </summary>
public sealed class ResolvedorContexto(IRepositorioIdentidad repositorio, IConsultaOrganizaciones organizaciones)
{
    public async Task<ResultadoResolucion> ResolverAsync(
        Guid usuarioId,
        Guid? organizacionSeleccionada,
        CancellationToken cancelacion)
    {
        var usuario = await repositorio.ObtenerUsuarioAsync(usuarioId, cancelacion);
        if (usuario is not { Activo: true })
        {
            return new ResultadoResolucion(EstadoResolucion.UsuarioNoValido);
        }

        var vigentes = (await MembresiasVigentesAsync(usuarioId, cancelacion)).Select(m => m.Membresia).ToList();

        if (organizacionSeleccionada is { } seleccionada)
        {
            var elegida = vigentes.SingleOrDefault(m => m.OrganizacionId == seleccionada);
            return elegida is null
                ? new ResultadoResolucion(EstadoResolucion.OrganizacionNoPermitida, usuario)
                : new ResultadoResolucion(EstadoResolucion.Resuelto, usuario, elegida);
        }

        return vigentes.Count switch
        {
            0 => new ResultadoResolucion(EstadoResolucion.SinMembresia, usuario),
            1 => new ResultadoResolucion(EstadoResolucion.Resuelto, usuario, vigentes[0]),
            _ => new ResultadoResolucion(EstadoResolucion.OrganizacionNoSeleccionada, usuario),
        };
    }

    /// <summary>Usuario activo (para endpoints <c>[SinOrganizacionResuelta]</c>).</summary>
    public async Task<bool> UsuarioActivoAsync(Guid usuarioId, CancellationToken cancelacion) =>
        await repositorio.ObtenerUsuarioAsync(usuarioId, cancelacion) is { Activo: true };

    /// <summary>Membresías activas del usuario en organizaciones activas, con el nombre de la organización.</summary>
    public async Task<IReadOnlyList<(Membresia Membresia, string NombreOrganizacion)>> MembresiasVigentesAsync(
        Guid usuarioId, CancellationToken cancelacion)
    {
        var vigentes = new List<(Membresia, string)>();
        foreach (var membresia in await repositorio.MembresiasActivasAsync(usuarioId, cancelacion))
        {
            var organizacion = await organizaciones.ObtenerAsync(membresia.OrganizacionId, cancelacion);
            // Organización inactiva: sin operaciones nuevas hasta que D-16 defina sus efectos.
            if (organizacion is { Activa: true }) vigentes.Add((membresia, organizacion.Nombre));
        }

        return vigentes;
    }
}
