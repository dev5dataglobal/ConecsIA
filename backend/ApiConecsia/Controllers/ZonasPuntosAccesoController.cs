using ApiConecsia.Helpers;
using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces;
using ConecsIA.Application.Seguridad;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Controllers;

/// <summary>
/// Administración de zonas y puntos de acceso (RF-05, ADR-027): Organización → Sede (ubicación) → Zona → Punto de acceso.
/// Organización: siempre la activa validada en servidor (filtro global); alcance por sede de la membresía (D-34).
/// Permisos: módulo «sedes» (estructura física) — consultar, añadir, editar; eliminar exige editar.
/// Unicidad: nombre por sede sin distinguir mayúsculas (zona y punto), garantizada por índices únicos.
/// Un registro con autorizaciones, movimientos, puntos u operadores asignados no se elimina: se inactiva y conserva su historial.
/// </summary>
public sealed class ZonasPuntosAccesoController(
    ConecsiaDbContext db,
    IOrganizationContext organizationContext,
    IModuloAccesoService accesos) : AdministracionControllerBase(db, organizationContext, accesos)
{
    private static readonly string[] Modos = ["Entrada", "Salida", "Ambos"];

    // ---------- Zonas ----------

    [HttpGet("api/zonas")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<ZonaAdminDto>>> ListarZonas(
        [FromQuery] int? idSede = null, [FromQuery] bool? activo = null, [FromQuery] string? buscar = null, CancellationToken ct = default)
    {
        var query = await ZonasEnAlcanceAsync(ct);
        if (idSede is int sede) query = query.Where(x => x.IdSede == sede);
        if (activo is bool a) query = query.Where(x => x.Activo == a);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = $"%{buscar.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Nombre, texto) || (x.Descripcion != null && EF.Functions.ILike(x.Descripcion, texto)));
        }
        return Ok(await ProyectarZonas(query).OrderBy(x => x.Sede).ThenBy(x => x.Nombre).ToListAsync(ct));
    }

    [HttpGet("api/zonas/{id:int}")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Consultar)]
    public async Task<ActionResult<ZonaAdminDto>> ObtenerZona(int id, CancellationToken ct)
    {
        var dto = await ZonaDtoAsync(id, ct);
        return dto is null ? NoEncontrado("La zona no existe o no está en tu alcance.") : Ok(dto);
    }

    [HttpPost("api/zonas")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Anadir)]
    public async Task<ActionResult<ZonaAdminDto>> CrearZona([FromBody] ZonaEntrada request, CancellationToken ct)
    {
        if (await ValidarSedeAsync(request.IdSede, ct) is { } error) return error;
        var nombre = request.Nombre.Trim();
        if (nombre.Length == 0) return Invalido("Indique el nombre de la zona.");
        if (await ZonaDuplicadaAsync(request.IdSede, nombre, null, ct)) return Conflicto("Ya existe una zona con ese nombre en la sede.");

        var zona = new Zona
        {
            IdOrganizacion = OrganizacionActiva, IdSede = request.IdSede, Nombre = nombre, Descripcion = Limpiar(request.Descripcion),
            Activo = true, CreadoEn = DateTime.UtcNow, IdUsuarioCrea = User.IdUsuario(),
        };
        Db.Zonas.Add(zona);
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe una zona con ese nombre en la sede.");
        Auditar(OrganizacionActiva, "zona.crear", "Zona", zona.IdZona.ToString(), null, Resumen(zona));
        await Db.SaveChangesAsync(ct);
        return Created($"/api/zonas/{zona.IdZona}", await ZonaDtoAsync(zona.IdZona, ct));
    }

    [HttpPut("api/zonas/{id:int}")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Editar)]
    public async Task<ActionResult<ZonaAdminDto>> EditarZona(int id, [FromBody] ZonaEntrada request, CancellationToken ct)
    {
        var zona = await ZonaEnAlcanceAsync(id, ct);
        if (zona is null) return NoEncontrado("La zona no existe o no está en tu alcance.");
        var nombre = request.Nombre.Trim();
        if (nombre.Length == 0) return Invalido("Indique el nombre de la zona.");
        if (request.IdSede != zona.IdSede)
        {
            // Cambiar de sede rompería la coherencia de sus puntos, autorizaciones y movimientos.
            if (await ZonaConHistorialAsync(id, ct)) return Conflicto("La zona ya tiene puntos de acceso, autorizaciones o movimientos: no puede cambiar de sede.");
            if (await ValidarSedeAsync(request.IdSede, ct) is { } error) return error;
        }
        if (await ZonaDuplicadaAsync(request.IdSede, nombre, id, ct)) return Conflicto("Ya existe una zona con ese nombre en la sede.");

        var anterior = Resumen(zona);
        zona.IdSede = request.IdSede;
        zona.Nombre = nombre;
        zona.Descripcion = Limpiar(request.Descripcion);
        Tocar(zona);
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe una zona con ese nombre en la sede.");
        Auditar(OrganizacionActiva, "zona.modificar", "Zona", id.ToString(), anterior, Resumen(zona));
        await Db.SaveChangesAsync(ct);
        return Ok(await ZonaDtoAsync(id, ct));
    }

    [HttpPost("api/zonas/{id:int}/activar")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Editar)]
    public Task<ActionResult<ZonaAdminDto>> ActivarZona(int id, CancellationToken ct) => CambiarEstadoZonaAsync(id, true, ct);

    /// <summary>Inactivar no toca sus puntos ni el historial; mientras esté inactiva, sus puntos no registran movimientos.</summary>
    [HttpPost("api/zonas/{id:int}/inactivar")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Editar)]
    public Task<ActionResult<ZonaAdminDto>> InactivarZona(int id, CancellationToken ct) => CambiarEstadoZonaAsync(id, false, ct);

    private async Task<ActionResult<ZonaAdminDto>> CambiarEstadoZonaAsync(int id, bool activo, CancellationToken ct)
    {
        var zona = await ZonaEnAlcanceAsync(id, ct);
        if (zona is null) return NoEncontrado("La zona no existe o no está en tu alcance.");
        if (zona.Activo == activo) return Conflicto(activo ? "La zona ya está activa." : "La zona ya está inactiva.");
        if (activo && !await Db.Sedes.AnyAsync(x => x.IdSede == zona.IdSede && x.Activo, ct)) return Conflicto("Activa primero la sede de la zona.");
        var anterior = Resumen(zona);
        zona.Activo = activo;
        Tocar(zona);
        Auditar(OrganizacionActiva, activo ? "zona.activar" : "zona.inactivar", "Zona", id.ToString(), anterior, Resumen(zona));
        await Db.SaveChangesAsync(ct);
        return Ok(await ZonaDtoAsync(id, ct));
    }

    [HttpDelete("api/zonas/{id:int}")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Editar)]
    public async Task<IActionResult> EliminarZona(int id, CancellationToken ct)
    {
        var zona = await ZonaEnAlcanceAsync(id, ct);
        if (zona is null) return NoEncontrado("La zona no existe o no está en tu alcance.");
        if (await ZonaConHistorialAsync(id, ct))
            return Conflicto("La zona tiene puntos de acceso, autorizaciones o movimientos asociados: inactívala para conservar su historial.");
        Db.Zonas.Remove(zona);
        Auditar(OrganizacionActiva, "zona.eliminar", "Zona", id.ToString(), Resumen(zona));
        try { await Db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            // Una referencia creada en paralelo (FK restrict) impide borrar.
            Db.ChangeTracker.Clear();
            return Conflicto("La zona quedó asociada a otros registros: inactívala para conservar su historial.");
        }
        return NoContent();
    }

    // ---------- Puntos de acceso ----------

    [HttpGet("api/puntos-acceso")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<PuntoAccesoAdminDto>>> ListarPuntos(
        [FromQuery] int? idSede = null, [FromQuery] int? idZona = null, [FromQuery] bool? activo = null, [FromQuery] string? buscar = null,
        CancellationToken ct = default)
    {
        var query = await PuntosEnAlcanceAsync(ct);
        if (idSede is int sede) query = query.Where(x => x.IdSede == sede);
        if (idZona is int zona) query = query.Where(x => x.IdZona == zona);
        if (activo is bool a) query = query.Where(x => x.Activo == a);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = $"%{buscar.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Nombre, texto) || (x.Descripcion != null && EF.Functions.ILike(x.Descripcion, texto)));
        }
        return Ok(await ProyectarPuntos(query).OrderBy(x => x.Sede).ThenBy(x => x.Zona).ThenBy(x => x.Nombre).ToListAsync(ct));
    }

    [HttpGet("api/puntos-acceso/{id:int}")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Consultar)]
    public async Task<ActionResult<PuntoAccesoAdminDto>> ObtenerPunto(int id, CancellationToken ct)
    {
        var dto = await PuntoDtoAsync(id, ct);
        return dto is null ? NoEncontrado("El punto de acceso no existe o no está en tu alcance.") : Ok(dto);
    }

    [HttpPost("api/puntos-acceso")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Anadir)]
    public async Task<ActionResult<PuntoAccesoAdminDto>> CrearPunto([FromBody] PuntoAccesoEntrada request, CancellationToken ct)
    {
        var (zona, error) = await ValidarPuntoAsync(request, ct);
        if (error is not null) return error;
        var nombre = request.Nombre.Trim();
        if (await PuntoDuplicadoAsync(zona!.IdSede, nombre, null, ct)) return Conflicto("Ya existe un punto de acceso con ese nombre en la sede.");

        var punto = new PuntoAcceso
        {
            IdOrganizacion = OrganizacionActiva, IdSede = zona.IdSede, IdZona = zona.IdZona, Nombre = nombre,
            Descripcion = Limpiar(request.Descripcion), ModoAcceso = request.ModoAcceso, Activo = true,
            CreadoEn = DateTime.UtcNow, IdUsuarioCrea = User.IdUsuario(),
        };
        Db.PuntosAcceso.Add(punto);
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe un punto de acceso con ese nombre en la sede.");
        Auditar(OrganizacionActiva, "punto_acceso.crear", "PuntoAcceso", punto.IdPuntoAcceso.ToString(), null, Resumen(punto));
        await Db.SaveChangesAsync(ct);
        return Created($"/api/puntos-acceso/{punto.IdPuntoAcceso}", await PuntoDtoAsync(punto.IdPuntoAcceso, ct));
    }

    /// <summary>El punto puede pasar a otra zona de la misma sede; los movimientos anteriores conservan la zona en que se registraron.</summary>
    [HttpPut("api/puntos-acceso/{id:int}")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Editar)]
    public async Task<ActionResult<PuntoAccesoAdminDto>> EditarPunto(int id, [FromBody] PuntoAccesoEntrada request, CancellationToken ct)
    {
        var punto = await PuntoEnAlcanceAsync(id, ct);
        if (punto is null) return NoEncontrado("El punto de acceso no existe o no está en tu alcance.");
        var (zona, error) = await ValidarPuntoAsync(request, ct, punto.IdZona);
        if (error is not null) return error;
        if (zona!.IdSede != punto.IdSede) return Invalido("El punto de acceso solo puede asociarse a una zona de su misma sede.");
        var nombre = request.Nombre.Trim();
        if (await PuntoDuplicadoAsync(punto.IdSede, nombre, id, ct)) return Conflicto("Ya existe un punto de acceso con ese nombre en la sede.");

        var anterior = Resumen(punto);
        punto.IdZona = zona.IdZona;
        punto.Nombre = nombre;
        punto.Descripcion = Limpiar(request.Descripcion);
        punto.ModoAcceso = request.ModoAcceso;
        Tocar(punto);
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe un punto de acceso con ese nombre en la sede.");
        Auditar(OrganizacionActiva, "punto_acceso.modificar", "PuntoAcceso", id.ToString(), anterior, Resumen(punto));
        await Db.SaveChangesAsync(ct);
        return Ok(await PuntoDtoAsync(id, ct));
    }

    [HttpPost("api/puntos-acceso/{id:int}/activar")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Editar)]
    public Task<ActionResult<PuntoAccesoAdminDto>> ActivarPunto(int id, CancellationToken ct) => CambiarEstadoPuntoAsync(id, true, ct);

    [HttpPost("api/puntos-acceso/{id:int}/inactivar")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Editar)]
    public Task<ActionResult<PuntoAccesoAdminDto>> InactivarPunto(int id, CancellationToken ct) => CambiarEstadoPuntoAsync(id, false, ct);

    private async Task<ActionResult<PuntoAccesoAdminDto>> CambiarEstadoPuntoAsync(int id, bool activo, CancellationToken ct)
    {
        var punto = await PuntoEnAlcanceAsync(id, ct);
        if (punto is null) return NoEncontrado("El punto de acceso no existe o no está en tu alcance.");
        if (punto.Activo == activo) return Conflicto(activo ? "El punto de acceso ya está activo." : "El punto de acceso ya está inactivo.");
        if (activo && !await Db.Zonas.AnyAsync(x => x.IdZona == punto.IdZona && x.Activo, ct)) return Conflicto("Activa primero la zona del punto de acceso.");
        var anterior = Resumen(punto);
        punto.Activo = activo;
        Tocar(punto);
        Auditar(OrganizacionActiva, activo ? "punto_acceso.activar" : "punto_acceso.inactivar", "PuntoAcceso", id.ToString(), anterior, Resumen(punto));
        await Db.SaveChangesAsync(ct);
        return Ok(await PuntoDtoAsync(id, ct));
    }

    [HttpDelete("api/puntos-acceso/{id:int}")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Editar)]
    public async Task<IActionResult> EliminarPunto(int id, CancellationToken ct)
    {
        var punto = await PuntoEnAlcanceAsync(id, ct);
        if (punto is null) return NoEncontrado("El punto de acceso no existe o no está en tu alcance.");
        if (await PuntoConHistorialAsync(id, ct))
            return Conflicto("El punto de acceso tiene movimientos u operadores asignados: inactívalo para conservar su historial.");
        Db.PuntosAcceso.Remove(punto);
        Auditar(OrganizacionActiva, "punto_acceso.eliminar", "PuntoAcceso", id.ToString(), Resumen(punto));
        try { await Db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            Db.ChangeTracker.Clear();
            return Conflicto("El punto de acceso quedó asociado a otros registros: inactívalo para conservar su historial.");
        }
        return NoContent();
    }

    // ---------- Apoyo ----------

    private async Task<ObjectResult?> ValidarSedeAsync(int idSede, CancellationToken ct)
    {
        if (!await Db.Sedes.AnyAsync(x => x.IdSede == idSede && x.Activo, ct)) return Invalido("La sede no existe en tu organización o está inactiva.");
        return EnAlcance(await SedesAlcanceAsync(ct), idSede) ? null : Prohibido("La sede no está en tu alcance.");
    }

    /// <summary>Zona destino del punto: de la organización activa, en alcance y activa (se tolera la zona actual inactiva al editar).</summary>
    private async Task<(Zona? Zona, ObjectResult? Error)> ValidarPuntoAsync(PuntoAccesoEntrada request, CancellationToken ct, int? zonaActual = null)
    {
        if (request.Nombre.Trim().Length == 0) return (null, Invalido("Indique el nombre del punto de acceso."));
        if (!Modos.Contains(request.ModoAcceso)) return (null, Invalido("El modo debe ser Entrada, Salida o Ambos."));
        var zona = await Db.Zonas.AsNoTracking().SingleOrDefaultAsync(x => x.IdZona == request.IdZona, ct);
        if (zona is null) return (null, Invalido("La zona no existe en tu organización."));
        if (!zona.Activo && zona.IdZona != zonaActual) return (null, Invalido("La zona está inactiva: no admite nuevos puntos de acceso."));
        if (!EnAlcance(await SedesAlcanceAsync(ct), zona.IdSede)) return (null, Prohibido("La sede de la zona no está en tu alcance."));
        return (zona, null);
    }

    private Task<bool> ZonaDuplicadaAsync(int idSede, string nombre, int? excluir, CancellationToken ct)
    {
        var normalizado = Normalizar(nombre);
        return Db.Zonas.AnyAsync(x => x.IdSede == idSede && x.IdZona != excluir && EF.Property<string>(x, "NombreNormalizado") == normalizado, ct);
    }

    private Task<bool> PuntoDuplicadoAsync(int idSede, string nombre, int? excluir, CancellationToken ct)
    {
        var normalizado = Normalizar(nombre);
        return Db.PuntosAcceso.AnyAsync(x => x.IdSede == idSede && x.IdPuntoAcceso != excluir && EF.Property<string>(x, "NombreNormalizado") == normalizado, ct);
    }

    private async Task<bool> ZonaConHistorialAsync(int id, CancellationToken ct) =>
        await Db.PuntosAcceso.AnyAsync(x => x.IdZona == id, ct)
        || await Db.AutorizacionesZonas.AnyAsync(x => x.IdZona == id, ct)
        || await Db.MovimientosAcceso.AnyAsync(x => x.IdZona == id, ct);

    private async Task<bool> PuntoConHistorialAsync(int id, CancellationToken ct) =>
        await Db.MovimientosAcceso.AnyAsync(x => x.IdPuntoAcceso == id, ct)
        || await Db.MembresiasPuntosAcceso.AnyAsync(x => x.IdPuntoAcceso == id, ct);

    private async Task<IQueryable<Zona>> ZonasEnAlcanceAsync(CancellationToken ct)
    {
        var alcance = await SedesAlcanceAsync(ct);
        return alcance is null ? Db.Zonas.AsNoTracking() : Db.Zonas.AsNoTracking().Where(x => alcance.Contains(x.IdSede));
    }

    private async Task<IQueryable<PuntoAcceso>> PuntosEnAlcanceAsync(CancellationToken ct)
    {
        var alcance = await SedesAlcanceAsync(ct);
        return alcance is null ? Db.PuntosAcceso.AsNoTracking() : Db.PuntosAcceso.AsNoTracking().Where(x => alcance.Contains(x.IdSede));
    }

    private async Task<Zona?> ZonaEnAlcanceAsync(int id, CancellationToken ct)
    {
        var zona = await Db.Zonas.SingleOrDefaultAsync(x => x.IdZona == id, ct);
        return zona is not null && EnAlcance(await SedesAlcanceAsync(ct), zona.IdSede) ? zona : null;
    }

    private async Task<PuntoAcceso?> PuntoEnAlcanceAsync(int id, CancellationToken ct)
    {
        var punto = await Db.PuntosAcceso.SingleOrDefaultAsync(x => x.IdPuntoAcceso == id, ct);
        return punto is not null && EnAlcance(await SedesAlcanceAsync(ct), punto.IdSede) ? punto : null;
    }

    private void Tocar(Zona x) { x.ActualizadoEn = DateTime.UtcNow; x.IdUsuarioActualiza = User.IdUsuario(); }
    private void Tocar(PuntoAcceso x) { x.ActualizadoEn = DateTime.UtcNow; x.IdUsuarioActualiza = User.IdUsuario(); }

    private static object Resumen(Zona x) => new { x.IdSede, x.Nombre, x.Descripcion, x.Activo };
    private static object Resumen(PuntoAcceso x) => new { x.IdSede, x.IdZona, x.Nombre, x.Descripcion, x.ModoAcceso, x.Activo };

    private IQueryable<ZonaAdminDto> ProyectarZonas(IQueryable<Zona> origen) =>
        from z in origen
        join s in Db.Sedes on z.IdSede equals s.IdSede
        select new ZonaAdminDto
        {
            IdZona = z.IdZona, IdSede = s.IdSede, Sede = s.Nombre, SedeActiva = s.Activo, Nombre = z.Nombre, Descripcion = z.Descripcion, Activo = z.Activo,
            PuntosAcceso = Db.PuntosAcceso.Count(p => p.IdZona == z.IdZona),
            TieneHistorial = Db.PuntosAcceso.Any(p => p.IdZona == z.IdZona) || Db.AutorizacionesZonas.Any(a => a.IdZona == z.IdZona)
                || Db.MovimientosAcceso.Any(m => m.IdZona == z.IdZona),
            CreadoEn = z.CreadoEn, CreadoPor = Db.Usuarios.Where(u => u.IdUsuario == z.IdUsuarioCrea).Select(u => u.Correo).FirstOrDefault(),
            ActualizadoEn = z.ActualizadoEn, ActualizadoPor = Db.Usuarios.Where(u => u.IdUsuario == z.IdUsuarioActualiza).Select(u => u.Correo).FirstOrDefault(),
        };

    private IQueryable<PuntoAccesoAdminDto> ProyectarPuntos(IQueryable<PuntoAcceso> origen) =>
        from p in origen
        join z in Db.Zonas on p.IdZona equals z.IdZona
        join s in Db.Sedes on p.IdSede equals s.IdSede
        select new PuntoAccesoAdminDto
        {
            IdPuntoAcceso = p.IdPuntoAcceso, IdSede = s.IdSede, Sede = s.Nombre, IdZona = z.IdZona, Zona = z.Nombre, ZonaActiva = z.Activo,
            Nombre = p.Nombre, Descripcion = p.Descripcion, ModoAcceso = p.ModoAcceso, Activo = p.Activo,
            TieneHistorial = Db.MovimientosAcceso.Any(m => m.IdPuntoAcceso == p.IdPuntoAcceso) || Db.MembresiasPuntosAcceso.Any(m => m.IdPuntoAcceso == p.IdPuntoAcceso),
            CreadoEn = p.CreadoEn, CreadoPor = Db.Usuarios.Where(u => u.IdUsuario == p.IdUsuarioCrea).Select(u => u.Correo).FirstOrDefault(),
            ActualizadoEn = p.ActualizadoEn, ActualizadoPor = Db.Usuarios.Where(u => u.IdUsuario == p.IdUsuarioActualiza).Select(u => u.Correo).FirstOrDefault(),
        };

    private async Task<ZonaAdminDto?> ZonaDtoAsync(int id, CancellationToken ct) =>
        await ProyectarZonas((await ZonasEnAlcanceAsync(ct)).Where(x => x.IdZona == id)).SingleOrDefaultAsync(ct);

    private async Task<PuntoAccesoAdminDto?> PuntoDtoAsync(int id, CancellationToken ct) =>
        await ProyectarPuntos((await PuntosEnAlcanceAsync(ct)).Where(x => x.IdPuntoAcceso == id)).SingleOrDefaultAsync(ct);
}
