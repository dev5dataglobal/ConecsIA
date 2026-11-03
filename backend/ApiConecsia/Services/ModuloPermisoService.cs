using System.Security.Claims;
using System.Text.Json;
using ApiConecsia.Helpers;
using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Application.Seguridad;
using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ApiConecsia.Services;

public enum ResultadoGestion { Ok, NoEncontrado, Prohibido, Conflicto, Invalido }

public sealed record Resultado<T>(ResultadoGestion Estado, T? Valor = default, string? Detalle = null)
{
    public static Resultado<T> Exito(T valor) => new(ResultadoGestion.Ok, valor);
    public static Resultado<T> Falla(ResultadoGestion estado, string detalle) => new(estado, default, detalle);
}

/// <summary>
/// Catálogo global de módulos y asignación de módulos a roles.
/// Reglas: el catálogo solo lo modifica el administrador general; quien tenga el módulo «perfiles» con
/// Consultar ve las asignaciones y con Editar las cambia, solo en roles de su organización activa
/// y solo para las acciones que él mismo tiene sobre cada módulo (evita escalamiento).
/// </summary>
public sealed class ModuloPermisoService(
    IUnitOfWork unitOfWork,
    IModuloAccesoService accesos,
    IOrganizationContext organizationContext,
    IHttpContextAccessor httpContextAccessor)
{
    // ---------- Autorización de la gestión ----------

    public Task<bool> PuedeConsultarAsync(ClaimsPrincipal user, CancellationToken ct = default) =>
        PuedeAsync(user, CodigosModulo.Perfiles, AccionModulo.Consultar, ct);

    public Task<bool> PuedeAdministrarRolesAsync(ClaimsPrincipal user, CancellationToken ct = default) =>
        PuedeAsync(user, CodigosModulo.Perfiles, AccionModulo.Editar, ct);

    private async Task<bool> PuedeAsync(ClaimsPrincipal user, string codigo, AccionModulo accion, CancellationToken ct)
    {
        var idUsuario = user.IdUsuario();
        var idOrganizacion = organizationContext.OrganizationId;
        return idUsuario is not null && idOrganizacion is not null
            && await accesos.PuedeAsync(idUsuario.Value, idOrganizacion.Value, user.EsAdministradorGeneral(), codigo, accion, ct);
    }

    // ---------- Catálogo ----------

    public async Task<List<AppModuloDto>> ListarCatalogoAsync(CancellationToken ct = default) =>
        (await unitOfWork.AppModuloRepository.ListarAsync(ct)).Select(Mapear).ToList();

    public async Task<List<AppModuloDto>> ListarPermitidosAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        var idUsuario = user.IdUsuario();
        var idOrganizacion = organizationContext.OrganizationId;
        if (idUsuario is null || idOrganizacion is null) return [];
        var propios = await accesos.ObtenerAccesosAsync(idUsuario.Value, idOrganizacion.Value, user.EsAdministradorGeneral(), ct);
        return (await unitOfWork.AppModuloRepository.ListarAsync(ct))
            .Where(x => x.Activo && propios.ContainsKey(x.Codigo))
            .Select(x => Mapear(x, propios[x.Codigo])).ToList();
    }

    public async Task<Resultado<AppModuloDto>> CrearModuloAsync(ClaimsPrincipal user, AppModuloGuardarDto dto, CancellationToken ct = default)
    {
        if (await unitOfWork.AppModuloRepository.ExisteCodigoAsync(dto.Codigo, null, ct))
            return Resultado<AppModuloDto>.Falla(ResultadoGestion.Conflicto, $"Ya existe un módulo con el código '{dto.Codigo}'.");

        var modulo = new AppModulo
        {
            Codigo = dto.Codigo, Nombre = dto.Nombre.Trim(), Descripcion = dto.Descripcion, Ruta = dto.Ruta,
            Icono = dto.Icono, Orden = dto.Orden, Activo = true, CreadoEn = DateTime.UtcNow,
        };
        unitOfWork.AppModuloRepository.Add(modulo);
        Auditar(user, null, "CrearModulo", "app_modulos", dto.Codigo, null, Mapear(modulo));
        if (!await GuardarAsync(ct))
            return Resultado<AppModuloDto>.Falla(ResultadoGestion.Conflicto, $"Ya existe un módulo con el código '{dto.Codigo}'.");
        accesos.InvalidarTodo();
        return Resultado<AppModuloDto>.Exito(Mapear(modulo));
    }

    public async Task<Resultado<AppModuloDto>> ActualizarModuloAsync(ClaimsPrincipal user, int id, AppModuloGuardarDto dto, CancellationToken ct = default)
    {
        var modulo = await unitOfWork.AppModuloRepository.ObtenerPorIdAsync(id, ct);
        if (modulo is null) return Resultado<AppModuloDto>.Falla(ResultadoGestion.NoEncontrado, "El módulo no existe.");
        // El código es el identificador estable de autorización: no se cambia una vez creado.
        if (!string.Equals(modulo.Codigo, dto.Codigo, StringComparison.Ordinal))
            return Resultado<AppModuloDto>.Falla(ResultadoGestion.Invalido, "El código de un módulo no puede modificarse.");

        var anterior = Mapear(modulo);
        modulo.Nombre = dto.Nombre.Trim();
        modulo.Descripcion = dto.Descripcion;
        modulo.Ruta = dto.Ruta;
        modulo.Icono = dto.Icono;
        modulo.Orden = dto.Orden;
        Auditar(user, null, "ActualizarModulo", "app_modulos", modulo.Codigo, anterior, Mapear(modulo));
        await GuardarAsync(ct);
        return Resultado<AppModuloDto>.Exito(Mapear(modulo));
    }

    public async Task<Resultado<AppModuloDto>> CambiarEstadoModuloAsync(ClaimsPrincipal user, int id, bool activo, CancellationToken ct = default)
    {
        var modulo = await unitOfWork.AppModuloRepository.ObtenerPorIdAsync(id, ct);
        if (modulo is null) return Resultado<AppModuloDto>.Falla(ResultadoGestion.NoEncontrado, "El módulo no existe.");
        var anterior = Mapear(modulo);
        modulo.Activo = activo;
        // Inactivar no borra asignaciones: al reactivar se recupera el acceso configurado.
        Auditar(user, null, activo ? "ActivarModulo" : "InactivarModulo", "app_modulos", modulo.Codigo, anterior, Mapear(modulo));
        await GuardarAsync(ct);
        accesos.InvalidarTodo();
        return Resultado<AppModuloDto>.Exito(Mapear(modulo));
    }

    // ---------- Asignaciones por rol ----------

    public async Task<Resultado<List<AppModuloDto>>> ObtenerModulosDeRolAsync(int idRol, CancellationToken ct = default)
    {
        if (await BuscarRolAsync(idRol, ct) is null)
            return Resultado<List<AppModuloDto>>.Falla(ResultadoGestion.NoEncontrado, "El rol no existe en tu organización.");
        var asignaciones = (await unitOfWork.ModuloPermisoRepository.ObtenerPorRolAsync(idRol, ct)).ToDictionary(x => x.IdAppModulo);
        return Resultado<List<AppModuloDto>>.Exito((await unitOfWork.ModuloPermisoRepository.ObtenerModulosDeRolAsync(idRol, ct))
            .Select(x => Mapear(x, Acciones(asignaciones[x.IdAppModulo]))).ToList());
    }

    /// <summary>Asigna el módulo con las tres acciones.</summary>
    public async Task<Resultado<bool>> AsignarAsync(ClaimsPrincipal user, int idRol, int idModulo, CancellationToken ct = default)
    {
        var rol = await BuscarRolAsync(idRol, ct);
        if (rol is null) return Resultado<bool>.Falla(ResultadoGestion.NoEncontrado, "El rol no existe en tu organización.");
        var modulo = (await unitOfWork.AppModuloRepository.ObtenerActivosPorIdsAsync([idModulo], ct)).SingleOrDefault();
        if (modulo is null) return Resultado<bool>.Falla(ResultadoGestion.NoEncontrado, "El módulo no existe o está inactivo.");
        if (!await PuedeOtorgarAsync(user, [(modulo.Codigo, AccionesModulo.Todas)], ct))
            return Resultado<bool>.Falla(ResultadoGestion.Prohibido, "No puedes otorgar un módulo o acción que no tienes.");
        if ((await unitOfWork.ModuloPermisoRepository.ObtenerPorRolAsync(idRol, ct)).Any(x => x.IdAppModulo == idModulo))
            return Resultado<bool>.Falla(ResultadoGestion.Conflicto, "El módulo ya está asignado a este rol.");

        unitOfWork.ModuloPermisoRepository.Add(NuevaAsignacion(user, rol, idModulo, AccionesModulo.Todas));
        Auditar(user, rol.IdOrganizacion, "AsignarModulo", "modulos_permisos", idRol.ToString(), null, new { idRol, modulo = modulo.Codigo });
        if (!await GuardarAsync(ct)) return Resultado<bool>.Falla(ResultadoGestion.Conflicto, "El módulo ya está asignado a este rol.");
        accesos.InvalidarOrganizacion(rol.IdOrganizacion);
        return Resultado<bool>.Exito(true);
    }

    public async Task<Resultado<bool>> RetirarAsync(ClaimsPrincipal user, int idRol, int idModulo, CancellationToken ct = default)
    {
        var rol = await BuscarRolAsync(idRol, ct);
        if (rol is null) return Resultado<bool>.Falla(ResultadoGestion.NoEncontrado, "El rol no existe en tu organización.");
        var asignacion = (await unitOfWork.ModuloPermisoRepository.ObtenerPorRolAsync(idRol, ct)).SingleOrDefault(x => x.IdAppModulo == idModulo);
        if (asignacion is null) return Resultado<bool>.Falla(ResultadoGestion.NoEncontrado, "El módulo no está asignado a este rol.");
        var codigo = (await unitOfWork.AppModuloRepository.ObtenerPorIdAsync(idModulo, ct))?.Codigo ?? idModulo.ToString();
        if (!await PuedeOtorgarAsync(user, [(codigo, Acciones(asignacion))], ct))
            return Resultado<bool>.Falla(ResultadoGestion.Prohibido, "No puedes retirar un módulo o acción que no tienes.");

        unitOfWork.ModuloPermisoRepository.Remove(asignacion);
        Auditar(user, rol.IdOrganizacion, "RetirarModulo", "modulos_permisos", idRol.ToString(), new { idRol, modulo = codigo }, null);
        await GuardarAsync(ct);
        accesos.InvalidarOrganizacion(rol.IdOrganizacion);
        return Resultado<bool>.Exito(true);
    }

    /// <summary>
    /// Reemplaza la matriz de acciones del rol en una sola transacción (todo o nada).
    /// Un módulo sin acciones marcadas se retira; solo se pueden cambiar acciones que el usuario tiene.
    /// </summary>
    public async Task<Resultado<List<AppModuloDto>>> ReemplazarAsync(ClaimsPrincipal user, int idRol, IReadOnlyCollection<AsignacionModuloDto> solicitud, CancellationToken ct = default)
    {
        var rol = await BuscarRolAsync(idRol, ct);
        if (rol is null) return Resultado<List<AppModuloDto>>.Falla(ResultadoGestion.NoEncontrado, "El rol no existe en tu organización.");

        // Duplicados del mismo módulo: se unen sus acciones.
        var deseadas = new Dictionary<int, AccionesModulo>();
        foreach (var item in solicitud)
        {
            var acciones = new AccionesModulo(item.Consultar, item.Anadir, item.Editar);
            deseadas[item.IdAppModulo] = deseadas.TryGetValue(item.IdAppModulo, out var previas) ? previas.Unir(acciones) : acciones;
        }
        foreach (var id in deseadas.Where(x => !x.Value.Alguna).Select(x => x.Key).ToList()) deseadas.Remove(id);

        var modulos = await unitOfWork.AppModuloRepository.ObtenerActivosPorIdsAsync(deseadas.Keys.ToList(), ct);
        if (modulos.Count != deseadas.Count)
            return Resultado<List<AppModuloDto>>.Falla(ResultadoGestion.Invalido, "Uno o más módulos no existen o están inactivos.");

        var actuales = (await unitOfWork.ModuloPermisoRepository.ObtenerPorRolAsync(idRol, ct)).ToDictionary(x => x.IdAppModulo);
        var catalogo = (await unitOfWork.AppModuloRepository.ListarAsync(ct)).ToDictionary(x => x.IdAppModulo, x => x.Codigo);

        // Acciones que cambian por módulo (añadidas o retiradas): deben estar entre las propias.
        var cambios = new List<(string Codigo, AccionesModulo Acciones)>();
        foreach (var id in deseadas.Keys.Union(actuales.Keys))
        {
            var antes = actuales.TryGetValue(id, out var fila) ? Acciones(fila) : default;
            var despues = deseadas.GetValueOrDefault(id);
            var diferencia = new AccionesModulo(antes.Consultar != despues.Consultar, antes.Anadir != despues.Anadir, antes.Editar != despues.Editar);
            if (diferencia.Alguna) cambios.Add((catalogo.GetValueOrDefault(id, id.ToString()), diferencia));
        }
        if (!await PuedeOtorgarAsync(user, cambios, ct))
            return Resultado<List<AppModuloDto>>.Falla(ResultadoGestion.Prohibido, "Solo puedes otorgar o retirar acciones de módulos que tú mismo tienes.");

        var anterior = actuales.Values.Select(x => new { modulo = catalogo.GetValueOrDefault(x.IdAppModulo), acciones = Acciones(x) }).OrderBy(x => x.modulo).ToList();
        foreach (var (id, fila) in actuales)
        {
            if (!deseadas.TryGetValue(id, out var acciones)) { unitOfWork.ModuloPermisoRepository.Remove(fila); continue; }
            if (Acciones(fila) == acciones) continue;
            fila.PuedeConsultar = acciones.Consultar; fila.PuedeAnadir = acciones.Anadir; fila.PuedeEditar = acciones.Editar;
            fila.AsignadoEn = DateTime.UtcNow; fila.IdUsuarioAsigna = user.IdUsuario();
            unitOfWork.ModuloPermisoRepository.Update(fila);
        }
        unitOfWork.ModuloPermisoRepository.AddRange(deseadas.Where(x => !actuales.ContainsKey(x.Key)).Select(x => NuevaAsignacion(user, rol, x.Key, x.Value)));
        Auditar(user, rol.IdOrganizacion, "ReemplazarModulos", "modulos_permisos", idRol.ToString(),
            new { idRol, modulos = anterior },
            new { idRol, modulos = modulos.Select(x => new { modulo = x.Codigo, acciones = deseadas[x.IdAppModulo] }).OrderBy(x => x.modulo) });
        if (!await GuardarAsync(ct))
            return Resultado<List<AppModuloDto>>.Falla(ResultadoGestion.Conflicto, "Las asignaciones cambiaron mientras se guardaban. Inténtalo de nuevo.");
        accesos.InvalidarOrganizacion(rol.IdOrganizacion);
        return Resultado<List<AppModuloDto>>.Exito(modulos.OrderBy(x => x.Orden).Select(x => Mapear(x, deseadas[x.IdAppModulo])).ToList());
    }

    // ---------- Auxiliares ----------

    /// <summary>Evita escalamiento: fuera del administrador general, solo se otorgan o retiran acciones propias.</summary>
    private async Task<bool> PuedeOtorgarAsync(ClaimsPrincipal user, IReadOnlyCollection<(string Codigo, AccionesModulo Acciones)> cambios, CancellationToken ct)
    {
        if (user.EsAdministradorGeneral() || cambios.Count == 0) return true;
        var idUsuario = user.IdUsuario();
        var idOrganizacion = organizationContext.OrganizationId;
        if (idUsuario is null || idOrganizacion is null) return false;
        var propios = await accesos.ObtenerAccesosAsync(idUsuario.Value, idOrganizacion.Value, false, ct);
        return cambios.All(c => propios.TryGetValue(c.Codigo, out var mias) && !c.Acciones.ExcedeA(mias));
    }

    // El filtro global de organización hace que un rol de otra organización no se encuentre.
    private Task<Rol?> BuscarRolAsync(int idRol, CancellationToken ct) =>
        Task.FromResult(organizationContext.OrganizationId is null ? null
            : unitOfWork.RolRepository.Find(x => x.IdRol == idRol).SingleOrDefault());

    private static AccionesModulo Acciones(ModuloPermiso x) => new(x.PuedeConsultar, x.PuedeAnadir, x.PuedeEditar);

    private static ModuloPermiso NuevaAsignacion(ClaimsPrincipal user, Rol rol, int idModulo, AccionesModulo acciones) => new()
    {
        IdOrganizacion = rol.IdOrganizacion, IdRol = rol.IdRol, IdAppModulo = idModulo,
        AsignadoEn = DateTime.UtcNow, IdUsuarioAsigna = user.IdUsuario(),
        PuedeConsultar = acciones.Consultar, PuedeAnadir = acciones.Anadir, PuedeEditar = acciones.Editar,
    };

    private void Auditar(ClaimsPrincipal user, int? idOrganizacion, string accion, string entidad, string idRegistro, object? anterior, object? nuevo)
    {
        var http = httpContextAccessor.HttpContext;
        unitOfWork.AuditoriaRepository.Add(new Auditoria
        {
            IdOrganizacion = idOrganizacion ?? organizationContext.OrganizationId,
            TipoActor = "Usuario",
            IdUsuario = user.IdUsuario(),
            Accion = accion,
            Entidad = entidad,
            IdRegistro = idRegistro,
            Resultado = "Exitoso",
            ValorAnterior = anterior is null ? null : JsonSerializer.Serialize(anterior),
            ValorNuevo = nuevo is null ? null : JsonSerializer.Serialize(nuevo),
            DireccionIp = http?.Connection.RemoteIpAddress?.ToString(),
            IdCorrelacion = http?.TraceIdentifier,
            OcurridoEn = DateTime.UtcNow,
        });
    }

    /// <summary>Guarda; devuelve false ante una violación de unicidad concurrente (p. ej. doble envío).</summary>
    private async Task<bool> GuardarAsync(CancellationToken ct)
    {
        try
        {
            await unitOfWork.SaveAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return false;
        }
    }

    private static AppModuloDto Mapear(AppModulo x) => Mapear(x, default);

    private static AppModuloDto Mapear(AppModulo x, AccionesModulo acciones) => new()
    {
        IdAppModulo = x.IdAppModulo, Codigo = x.Codigo, Nombre = x.Nombre, Descripcion = x.Descripcion,
        Ruta = x.Ruta, Icono = x.Icono, Orden = x.Orden, Activo = x.Activo,
        Consultar = acciones.Consultar, Anadir = acciones.Anadir, Editar = acciones.Editar,
    };
}
