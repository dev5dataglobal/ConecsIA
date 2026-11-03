using ApiConecsia.Helpers;
using ApiConecsia.Services;
using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces;
using ConecsIA.Application.Seguridad;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Controllers;

/// <summary>
/// Dispositivos (equipos externos) de las personas de la organización activa y su catálogo de tipos (RF-17).
/// Un dispositivo pertenece a una sola persona responsable (1:N). La organización y la persona salen del contexto
/// y de la ruta, nunca del cuerpo; las FK compuestas impiden mezclar organizaciones. No hay borrado: se inactiva y
/// se conservan sus autorizaciones y movimientos.
/// </summary>
public sealed class DispositivosController(
    ConecsiaDbContext db,
    IOrganizationContext organizationContext,
    IModuloAccesoService accesos) : AdministracionControllerBase(db, organizationContext, accesos)
{
    private const string Entidad = "Dispositivo";

    // ---------- Tipos de dispositivo ----------

    [HttpGet("api/dispositivos/tipos")]
    [RequiereModulo(CodigosModulo.Dispositivos, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<TipoDispositivoDto>>> ListarTipos(CancellationToken ct) =>
        Ok(await Db.TiposDispositivo.AsNoTracking().OrderByDescending(x => x.Activo).ThenBy(x => x.Nombre)
            .Select(x => new TipoDispositivoDto { IdTipoDispositivo = x.IdTipoDispositivo, Nombre = x.Nombre, Descripcion = x.Descripcion, Activo = x.Activo })
            .ToListAsync(ct));

    [HttpPost("api/dispositivos/tipos")]
    [RequiereModulo(CodigosModulo.Dispositivos, AccionModulo.Anadir)]
    public async Task<ActionResult<TipoDispositivoDto>> CrearTipo([FromBody] TipoDispositivoEntrada request, CancellationToken ct)
    {
        var nombre = request.Nombre.Trim();
        if (nombre.Length == 0) return Invalido("El nombre es obligatorio.");
        var tipo = new TipoDispositivo { IdOrganizacion = OrganizacionActiva, Nombre = nombre, Descripcion = Limpiar(request.Descripcion), Activo = true };
        Db.TiposDispositivo.Add(tipo);
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe un tipo de dispositivo con ese nombre.");
        Auditar(OrganizacionActiva, "CrearTipo", "TipoDispositivo", tipo.IdTipoDispositivo.ToString(), null, new { tipo.Nombre, tipo.Descripcion });
        await Db.SaveChangesAsync(ct);
        return Created($"/api/dispositivos/tipos/{tipo.IdTipoDispositivo}", ATipoDto(tipo));
    }

    [HttpPut("api/dispositivos/tipos/{idTipo:int}")]
    [RequiereModulo(CodigosModulo.Dispositivos, AccionModulo.Editar)]
    public async Task<ActionResult<TipoDispositivoDto>> EditarTipo(int idTipo, [FromBody] TipoDispositivoEntrada request, CancellationToken ct)
    {
        var tipo = await Db.TiposDispositivo.SingleOrDefaultAsync(x => x.IdTipoDispositivo == idTipo, ct);
        if (tipo is null) return NoEncontrado("El tipo de dispositivo no existe en tu organización.");
        var nombre = request.Nombre.Trim();
        if (nombre.Length == 0) return Invalido("El nombre es obligatorio.");
        var anterior = new { tipo.Nombre, tipo.Descripcion };
        tipo.Nombre = nombre;
        tipo.Descripcion = Limpiar(request.Descripcion);
        Auditar(OrganizacionActiva, "ModificarTipo", "TipoDispositivo", idTipo.ToString(), anterior, new { tipo.Nombre, tipo.Descripcion });
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe un tipo de dispositivo con ese nombre.");
        return Ok(ATipoDto(tipo));
    }

    /// <summary>Inactivar un tipo impide usarlo en dispositivos nuevos; los existentes lo conservan.</summary>
    [HttpPatch("api/dispositivos/tipos/{idTipo:int}/estado")]
    [RequiereModulo(CodigosModulo.Dispositivos, AccionModulo.Editar)]
    public async Task<ActionResult<TipoDispositivoDto>> CambiarEstadoTipo(int idTipo, [FromBody] EstadoAdministracionEntrada request, CancellationToken ct)
    {
        var tipo = await Db.TiposDispositivo.SingleOrDefaultAsync(x => x.IdTipoDispositivo == idTipo, ct);
        if (tipo is null) return NoEncontrado("El tipo de dispositivo no existe en tu organización.");
        if (tipo.Activo != request.Activo!.Value)
        {
            tipo.Activo = request.Activo.Value;
            Auditar(OrganizacionActiva, tipo.Activo ? "ActivarTipo" : "InactivarTipo", "TipoDispositivo", idTipo.ToString(), new { Activo = !tipo.Activo }, new { tipo.Activo });
            await Db.SaveChangesAsync(ct);
        }
        return Ok(ATipoDto(tipo));
    }

    // ---------- Dispositivos de una persona ----------

    [HttpGet("api/personas/{idPersona:int}/dispositivos")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Consultar)]
    [RequiereModulo(CodigosModulo.Dispositivos, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<DispositivoDto>>> Listar(int idPersona, CancellationToken ct)
    {
        if (!await Db.Personas.AnyAsync(x => x.IdPersona == idPersona, ct)) return NoEncontrado("La persona no existe o no está en tu alcance.");
        return Ok(await Proyectar(Db.Dispositivos.Where(x => x.IdPersonaResponsable == idPersona))
            .OrderByDescending(x => x.Activo).ThenBy(x => x.TipoDispositivo).ThenBy(x => x.CodigoIdentificador).ToListAsync(ct));
    }

    [HttpPost("api/personas/{idPersona:int}/dispositivos")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    [RequiereModulo(CodigosModulo.Dispositivos, AccionModulo.Anadir)]
    public async Task<ActionResult<DispositivoDto>> Crear(int idPersona, [FromBody] DispositivoEntrada request, CancellationToken ct)
    {
        if (!await Db.Personas.AnyAsync(x => x.IdPersona == idPersona, ct)) return NoEncontrado("La persona no existe o no está en tu alcance.");
        if (await ValidarAsync(request, null, ct) is { } error) return error;

        var dispositivo = new Dispositivo
        {
            IdOrganizacion = OrganizacionActiva, IdPersonaResponsable = idPersona, Activo = true,
            CreadoEn = DateTime.UtcNow, IdUsuarioCrea = User.IdUsuario()!.Value,
        };
        Aplicar(dispositivo, request);
        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        Db.Dispositivos.Add(dispositivo);
        // Doble envío o alta concurrente: el índice único (organización, identificador) rechaza la segunda.
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe un dispositivo con ese identificador en la organización.");
        Auditar(OrganizacionActiva, "Crear", Entidad, dispositivo.IdDispositivo.ToString(), null, Resumen(dispositivo));
        await Db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return Created($"/api/personas/{idPersona}/dispositivos/{dispositivo.IdDispositivo}", await DispositivoAsync(dispositivo.IdDispositivo, ct));
    }

    [HttpPut("api/personas/{idPersona:int}/dispositivos/{idDispositivo:int}")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    [RequiereModulo(CodigosModulo.Dispositivos, AccionModulo.Editar)]
    public async Task<ActionResult<DispositivoDto>> Editar(int idPersona, int idDispositivo, [FromBody] DispositivoEntrada request, CancellationToken ct)
    {
        var dispositivo = await Db.Dispositivos.SingleOrDefaultAsync(x => x.IdDispositivo == idDispositivo && x.IdPersonaResponsable == idPersona, ct);
        if (dispositivo is null) return NoEncontrado("El dispositivo no existe para esta persona.");
        if (await ValidarAsync(request, dispositivo, ct) is { } error) return error;

        var anterior = Resumen(dispositivo);
        Aplicar(dispositivo, request);
        Auditar(OrganizacionActiva, "Modificar", Entidad, idDispositivo.ToString(), anterior, Resumen(dispositivo));
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe un dispositivo con ese identificador en la organización.");
        return Ok(await DispositivoAsync(idDispositivo, ct));
    }

    /// <summary>Inactivar no borra ni revoca: autorizaciones y movimientos del dispositivo se conservan.</summary>
    [HttpPatch("api/personas/{idPersona:int}/dispositivos/{idDispositivo:int}/estado")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    [RequiereModulo(CodigosModulo.Dispositivos, AccionModulo.Editar)]
    public async Task<ActionResult<DispositivoDto>> CambiarEstado(int idPersona, int idDispositivo, [FromBody] EstadoAdministracionEntrada request, CancellationToken ct)
    {
        var dispositivo = await Db.Dispositivos.SingleOrDefaultAsync(x => x.IdDispositivo == idDispositivo && x.IdPersonaResponsable == idPersona, ct);
        if (dispositivo is null) return NoEncontrado("El dispositivo no existe para esta persona.");
        if (dispositivo.Activo != request.Activo!.Value)
        {
            dispositivo.Activo = request.Activo.Value;
            Auditar(OrganizacionActiva, dispositivo.Activo ? "Activar" : "Inactivar", Entidad, idDispositivo.ToString(), new { Activo = !dispositivo.Activo }, new { dispositivo.Activo });
            await Db.SaveChangesAsync(ct);
        }
        return Ok(await DispositivoAsync(idDispositivo, ct));
    }

    // ---------- Apoyo ----------

    private async Task<ActionResult?> ValidarAsync(DispositivoEntrada request, Dispositivo? actual, CancellationToken ct)
    {
        // Un tipo inactivo solo se admite si el dispositivo ya lo tenía (conserva la referencia histórica).
        var tipoActual = actual?.IdTipoDispositivo ?? 0;
        if (!await Db.TiposDispositivo.AnyAsync(x => x.IdTipoDispositivo == request.IdTipoDispositivo && (x.Activo || x.IdTipoDispositivo == tipoActual), ct))
            return Invalido("El tipo de dispositivo no existe en tu organización o está inactivo.");
        var codigo = ReglasDispositivo.NormalizarIdentificador(request.CodigoIdentificador);
        if (codigo.Length == 0) return Invalido("El identificador es obligatorio.");
        var idActual = actual?.IdDispositivo ?? 0;
        if (await Db.Dispositivos.AnyAsync(x => x.IdDispositivo != idActual && x.CodigoIdentificador == codigo, ct))
            return Conflicto("Ya existe un dispositivo con ese identificador en la organización.");
        return null;
    }

    private static void Aplicar(Dispositivo dispositivo, DispositivoEntrada request)
    {
        dispositivo.IdTipoDispositivo = request.IdTipoDispositivo;
        dispositivo.CodigoIdentificador = ReglasDispositivo.NormalizarIdentificador(request.CodigoIdentificador);
        dispositivo.Serial = Limpiar(request.Serial);
        dispositivo.SerialNormalizado = dispositivo.Serial is null ? null : ReglasDispositivo.NormalizarSerial(dispositivo.Serial);
        dispositivo.Marca = Limpiar(request.Marca);
        dispositivo.Modelo = Limpiar(request.Modelo);
        dispositivo.Color = Limpiar(request.Color);
        dispositivo.Descripcion = Limpiar(request.Descripcion);
    }

    private static object Resumen(Dispositivo x) => new { x.IdPersonaResponsable, x.IdTipoDispositivo, x.CodigoIdentificador, x.Serial, x.Marca, x.Modelo, x.Color, x.Activo };

    private static TipoDispositivoDto ATipoDto(TipoDispositivo x) => new() { IdTipoDispositivo = x.IdTipoDispositivo, Nombre = x.Nombre, Descripcion = x.Descripcion, Activo = x.Activo };

    private IQueryable<DispositivoDto> Proyectar(IQueryable<Dispositivo> origen) =>
        from d in origen.AsNoTracking()
        select new DispositivoDto
        {
            IdDispositivo = d.IdDispositivo, IdPersona = d.IdPersonaResponsable, IdTipoDispositivo = d.IdTipoDispositivo,
            TipoDispositivo = Db.TiposDispositivo.Where(t => t.IdTipoDispositivo == d.IdTipoDispositivo).Select(t => t.Nombre).FirstOrDefault(),
            CodigoIdentificador = d.CodigoIdentificador, Serial = d.Serial, Marca = d.Marca, Modelo = d.Modelo, Color = d.Color,
            Descripcion = d.Descripcion, Activo = d.Activo, CreadoEn = d.CreadoEn,
        };

    private Task<DispositivoDto> DispositivoAsync(int idDispositivo, CancellationToken ct) =>
        Proyectar(Db.Dispositivos.Where(x => x.IdDispositivo == idDispositivo)).SingleAsync(ct);
}
