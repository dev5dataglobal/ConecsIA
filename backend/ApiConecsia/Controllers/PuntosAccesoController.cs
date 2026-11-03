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
/// Administración de puntos de acceso (RF-05, ADR-028): Organización → Sede → Punto de acceso. Las zonas de acceso se retiraron.
/// Organización: siempre la activa validada en servidor (filtro global); alcance por sede de la membresía (D-34).
/// Permisos: módulo «sedes» (estructura física) — consultar, añadir, editar; eliminar exige editar.
/// Un punto puede marcarse común de la zona empresarial solo si su sede pertenece a una zona; sigue siendo de la sede y su organización.
/// Unicidad: nombre por sede sin distinguir mayúsculas, garantizada por índice único.
/// Un punto con movimientos u operadores asignados no se elimina: se inactiva y conserva su historial.
/// </summary>
public sealed class PuntosAccesoController(
    ConecsiaDbContext db,
    IOrganizationContext organizationContext,
    IModuloAccesoService accesos) : AdministracionControllerBase(db, organizationContext, accesos)
{
    private static readonly string[] Modos = ["Entrada", "Salida", "Ambos"];

    [HttpGet("api/puntos-acceso")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<PuntoAccesoAdminDto>>> ListarPuntos(
        [FromQuery] int? idSede = null, [FromQuery] bool? activo = null, [FromQuery] string? buscar = null, CancellationToken ct = default)
    {
        var query = await PuntosEnAlcanceAsync(ct);
        if (idSede is int sede) query = query.Where(x => x.IdSede == sede);
        if (activo is bool a) query = query.Where(x => x.Activo == a);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = $"%{buscar.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Nombre, texto) || (x.Descripcion != null && EF.Functions.ILike(x.Descripcion, texto)));
        }
        return Ok(await ProyectarPuntos(query).OrderBy(x => x.Sede).ThenBy(x => x.Nombre).ToListAsync(ct));
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
        var (sede, error) = await ValidarPuntoAsync(request, ct);
        if (error is not null) return error;
        var nombre = request.Nombre.Trim();
        if (await PuntoDuplicadoAsync(sede!.IdSede, nombre, null, ct)) return Conflicto("Ya existe un punto de acceso con ese nombre en la sede.");

        var punto = new PuntoAcceso
        {
            IdOrganizacion = OrganizacionActiva, IdSede = sede.IdSede, Nombre = nombre,
            Descripcion = Limpiar(request.Descripcion), ModoAcceso = request.ModoAcceso, EsComunZonaEmpresarial = request.EsComunZonaEmpresarial,
            Activo = true, CreadoEn = DateTime.UtcNow, IdUsuarioCrea = User.IdUsuario(),
        };
        Db.PuntosAcceso.Add(punto);
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe un punto de acceso con ese nombre en la sede.");
        Auditar(OrganizacionActiva, "punto_acceso.crear", "PuntoAcceso", punto.IdPuntoAcceso.ToString(), null, Resumen(punto));
        await Db.SaveChangesAsync(ct);
        return Created($"/api/puntos-acceso/{punto.IdPuntoAcceso}", await PuntoDtoAsync(punto.IdPuntoAcceso, ct));
    }

    /// <summary>El punto no cambia de sede: sus movimientos y operadores asignados quedan ligados a ella.</summary>
    [HttpPut("api/puntos-acceso/{id:int}")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Editar)]
    public async Task<ActionResult<PuntoAccesoAdminDto>> EditarPunto(int id, [FromBody] PuntoAccesoEntrada request, CancellationToken ct)
    {
        var punto = await PuntoEnAlcanceAsync(id, ct);
        if (punto is null) return NoEncontrado("El punto de acceso no existe o no está en tu alcance.");
        if (request.IdSede != punto.IdSede) return Invalido("El punto de acceso no puede cambiar de sede.");
        var (_, error) = await ValidarPuntoAsync(request, ct, edicion: true);
        if (error is not null) return error;
        var nombre = request.Nombre.Trim();
        if (await PuntoDuplicadoAsync(punto.IdSede, nombre, id, ct)) return Conflicto("Ya existe un punto de acceso con ese nombre en la sede.");

        var anterior = Resumen(punto);
        punto.Nombre = nombre;
        punto.Descripcion = Limpiar(request.Descripcion);
        punto.ModoAcceso = request.ModoAcceso;
        punto.EsComunZonaEmpresarial = request.EsComunZonaEmpresarial;
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
        if (activo && !await Db.Sedes.AnyAsync(x => x.IdSede == punto.IdSede && x.Activo, ct)) return Conflicto("Activa primero la sede del punto de acceso.");
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
            // Una referencia creada en paralelo (FK restrict) impide borrar.
            Db.ChangeTracker.Clear();
            return Conflicto("El punto de acceso quedó asociado a otros registros: inactívalo para conservar su historial.");
        }
        return NoContent();
    }

    // ---------- Apoyo ----------

    /// <summary>
    /// Sede del punto: de la organización activa (filtro global), en alcance y activa (al editar se tolera la sede inactiva).
    /// La marca común exige que la sede pertenezca a una zona empresarial.
    /// </summary>
    private async Task<(Sede? Sede, ObjectResult? Error)> ValidarPuntoAsync(PuntoAccesoEntrada request, CancellationToken ct, bool edicion = false)
    {
        if (request.Nombre.Trim().Length == 0) return (null, Invalido("Indique el nombre del punto de acceso."));
        if (!Modos.Contains(request.ModoAcceso)) return (null, Invalido("El modo debe ser Entrada, Salida o Ambos."));
        var sede = await Db.Sedes.AsNoTracking().SingleOrDefaultAsync(x => x.IdSede == request.IdSede, ct);
        if (sede is null) return (null, Invalido("La sede no existe en tu organización."));
        if (!sede.Activo && !edicion) return (null, Invalido("La sede está inactiva: no admite nuevos puntos de acceso."));
        if (!EnAlcance(await SedesAlcanceAsync(ct), sede.IdSede)) return (null, Prohibido("La sede no está en tu alcance."));
        if (request.EsComunZonaEmpresarial && sede.IdZonaEmpresarial is null)
            return (null, Invalido("La sede no pertenece a una zona empresarial: el punto no puede ser común de la zona."));
        return (sede, null);
    }

    private Task<bool> PuntoDuplicadoAsync(int idSede, string nombre, int? excluir, CancellationToken ct)
    {
        var normalizado = Normalizar(nombre);
        return Db.PuntosAcceso.AnyAsync(x => x.IdSede == idSede && x.IdPuntoAcceso != excluir && EF.Property<string>(x, "NombreNormalizado") == normalizado, ct);
    }

    private async Task<bool> PuntoConHistorialAsync(int id, CancellationToken ct) =>
        await Db.MovimientosAcceso.AnyAsync(x => x.IdPuntoAcceso == id, ct)
        || await Db.MembresiasPuntosAcceso.AnyAsync(x => x.IdPuntoAcceso == id, ct);

    private async Task<IQueryable<PuntoAcceso>> PuntosEnAlcanceAsync(CancellationToken ct)
    {
        var alcance = await SedesAlcanceAsync(ct);
        return alcance is null ? Db.PuntosAcceso.AsNoTracking() : Db.PuntosAcceso.AsNoTracking().Where(x => alcance.Contains(x.IdSede));
    }

    private async Task<PuntoAcceso?> PuntoEnAlcanceAsync(int id, CancellationToken ct)
    {
        var punto = await Db.PuntosAcceso.SingleOrDefaultAsync(x => x.IdPuntoAcceso == id, ct);
        return punto is not null && EnAlcance(await SedesAlcanceAsync(ct), punto.IdSede) ? punto : null;
    }

    private void Tocar(PuntoAcceso x) { x.ActualizadoEn = DateTime.UtcNow; x.IdUsuarioActualiza = User.IdUsuario(); }

    private static object Resumen(PuntoAcceso x) => new { x.IdSede, x.Nombre, x.Descripcion, x.ModoAcceso, x.EsComunZonaEmpresarial, x.Activo };

    private IQueryable<PuntoAccesoAdminDto> ProyectarPuntos(IQueryable<PuntoAcceso> origen) =>
        from p in origen
        join s in Db.Sedes on p.IdSede equals s.IdSede
        select new PuntoAccesoAdminDto
        {
            IdPuntoAcceso = p.IdPuntoAcceso, IdSede = s.IdSede, Sede = s.Nombre, SedeActiva = s.Activo,
            ZonaEmpresarial = Db.ZonasEmpresariales.Where(z => z.IdZonaEmpresarial == s.IdZonaEmpresarial).Select(z => z.Nombre).FirstOrDefault(),
            EsComunZonaEmpresarial = p.EsComunZonaEmpresarial,
            Nombre = p.Nombre, Descripcion = p.Descripcion, ModoAcceso = p.ModoAcceso, Activo = p.Activo,
            TieneHistorial = Db.MovimientosAcceso.Any(m => m.IdPuntoAcceso == p.IdPuntoAcceso) || Db.MembresiasPuntosAcceso.Any(m => m.IdPuntoAcceso == p.IdPuntoAcceso),
            CreadoEn = p.CreadoEn, CreadoPor = Db.Usuarios.Where(u => u.IdUsuario == p.IdUsuarioCrea).Select(u => u.Correo).FirstOrDefault(),
            ActualizadoEn = p.ActualizadoEn, ActualizadoPor = Db.Usuarios.Where(u => u.IdUsuario == p.IdUsuarioActualiza).Select(u => u.Correo).FirstOrDefault(),
        };

    private async Task<PuntoAccesoAdminDto?> PuntoDtoAsync(int id, CancellationToken ct) =>
        await ProyectarPuntos((await PuntosEnAlcanceAsync(ct)).Where(x => x.IdPuntoAcceso == id)).SingleOrDefaultAsync(ct);
}
