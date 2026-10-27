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
/// Catálogos del módulo de Personas.
/// - Tipos de persona: por organización (aislados por el filtro global); los administra quien tenga Personas.
/// - Tipos de documento: catálogo global de la plataforma; se consultan con Personas y solo el administrador
///   de plataforma los crea, modifica o inactiva.
/// Inactivar impide nuevos usos y conserva las referencias existentes; no hay borrado.
/// </summary>
[Route("api/personas")]
public sealed class CatalogosPersonaController(ConecsiaDbContext db, IOrganizationContext organizationContext, IModuloAccesoService accesos)
    : AdministracionControllerBase(db, organizationContext, accesos)
{
    // ---------- Tipos de persona ----------

    [HttpGet("tipos-persona")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<TipoPersonaDto>>> ListarTiposPersona(CancellationToken ct) =>
        Ok(await Db.TiposPersona.OrderByDescending(x => x.Activo).ThenBy(x => x.Nombre)
            .Select(x => new TipoPersonaDto { IdTipoPersona = x.IdTipoPersona, Nombre = x.Nombre, Descripcion = x.Descripcion, Activo = x.Activo }).ToListAsync(ct));

    [HttpPost("tipos-persona")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Anadir)]
    public async Task<ActionResult<TipoPersonaDto>> CrearTipoPersona([FromBody] TipoPersonaEntrada request, CancellationToken ct)
    {
        var nombre = request.Nombre.Trim();
        if (await NombreTipoUsadoAsync(nombre, null, ct)) return Conflicto("Ya existe un tipo de persona con ese nombre.");
        var tipo = new TipoPersona { IdOrganizacion = OrganizacionActiva, Nombre = nombre, Descripcion = Limpiar(request.Descripcion), Activo = true };
        Db.TiposPersona.Add(tipo);
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe un tipo de persona con ese nombre.");
        Auditar(OrganizacionActiva, "Crear", "TipoPersona", tipo.IdTipoPersona.ToString(), null, ADto(tipo));
        await Db.SaveChangesAsync(ct);
        return Created($"/api/personas/tipos-persona/{tipo.IdTipoPersona}", ADto(tipo));
    }

    [HttpPut("tipos-persona/{idTipoPersona:int}")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    public async Task<ActionResult<TipoPersonaDto>> EditarTipoPersona(int idTipoPersona, [FromBody] TipoPersonaEntrada request, CancellationToken ct)
    {
        var tipo = await Db.TiposPersona.SingleOrDefaultAsync(x => x.IdTipoPersona == idTipoPersona, ct);
        if (tipo is null) return NoEncontrado("El tipo de persona no existe en tu organización.");
        var nombre = request.Nombre.Trim();
        if (await NombreTipoUsadoAsync(nombre, idTipoPersona, ct)) return Conflicto("Ya existe un tipo de persona con ese nombre.");
        var anterior = ADto(tipo);
        tipo.Nombre = nombre;
        tipo.Descripcion = Limpiar(request.Descripcion);
        Auditar(OrganizacionActiva, "Modificar", "TipoPersona", idTipoPersona.ToString(), anterior, ADto(tipo));
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe un tipo de persona con ese nombre.");
        return Ok(ADto(tipo));
    }

    [HttpPatch("tipos-persona/{idTipoPersona:int}/estado")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    public async Task<ActionResult<TipoPersonaDto>> CambiarEstadoTipoPersona(int idTipoPersona, [FromBody] EstadoAdministracionEntrada request, CancellationToken ct)
    {
        var tipo = await Db.TiposPersona.SingleOrDefaultAsync(x => x.IdTipoPersona == idTipoPersona, ct);
        if (tipo is null) return NoEncontrado("El tipo de persona no existe en tu organización.");
        if (tipo.Activo != request.Activo!.Value)
        {
            tipo.Activo = request.Activo.Value;
            Auditar(OrganizacionActiva, tipo.Activo ? "Activar" : "Inactivar", "TipoPersona", idTipoPersona.ToString(), new { Activo = !tipo.Activo }, new { tipo.Activo });
            await Db.SaveChangesAsync(ct);
        }
        return Ok(ADto(tipo));
    }

    // ---------- Tipos de documento (globales) ----------

    [HttpGet("tipos-documento")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<TipoDocumentoAdminDto>>> ListarTiposDocumento(CancellationToken ct) =>
        Ok(await Db.TiposDocumento.OrderByDescending(x => x.Activo).ThenBy(x => x.IdTipoDocumento).Select(x => ADto(x)).ToListAsync(ct));

    [HttpPost("tipos-documento")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Anadir)]
    public async Task<ActionResult<TipoDocumentoAdminDto>> CrearTipoDocumento([FromBody] TipoDocumentoEntrada request, CancellationToken ct)
    {
        if (!EsPlataforma) return Prohibido("Los tipos de documento son globales: solo el administrador de plataforma los administra.");
        if (!ReglasPersona.PatronValido(request.PatronValidacion)) return Invalido("El patrón de validación no es una expresión regular válida.");
        var tipo = new TipoDocumento { Activo = true };
        AplicarDocumento(tipo, request);
        if (await Db.TiposDocumento.AnyAsync(x => x.Codigo == tipo.Codigo, ct)) return Conflicto("Ya existe un tipo de documento con ese código.");
        Db.TiposDocumento.Add(tipo);
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe un tipo de documento con ese código.");
        Auditar(null, "Crear", "TipoDocumento", tipo.IdTipoDocumento.ToString(), null, ADto(tipo));
        await Db.SaveChangesAsync(ct);
        return Created($"/api/personas/tipos-documento/{tipo.IdTipoDocumento}", ADto(tipo));
    }

    [HttpPut("tipos-documento/{idTipoDocumento:int}")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    public async Task<ActionResult<TipoDocumentoAdminDto>> EditarTipoDocumento(int idTipoDocumento, [FromBody] TipoDocumentoEntrada request, CancellationToken ct)
    {
        if (!EsPlataforma) return Prohibido("Los tipos de documento son globales: solo el administrador de plataforma los administra.");
        if (!ReglasPersona.PatronValido(request.PatronValidacion)) return Invalido("El patrón de validación no es una expresión regular válida.");
        var tipo = await Db.TiposDocumento.SingleOrDefaultAsync(x => x.IdTipoDocumento == idTipoDocumento, ct);
        if (tipo is null) return NoEncontrado("El tipo de documento no existe.");
        var anterior = ADto(tipo);
        AplicarDocumento(tipo, request);
        if (await Db.TiposDocumento.AnyAsync(x => x.IdTipoDocumento != idTipoDocumento && x.Codigo == tipo.Codigo, ct)) return Conflicto("Ya existe un tipo de documento con ese código.");
        Auditar(null, "Modificar", "TipoDocumento", idTipoDocumento.ToString(), anterior, ADto(tipo));
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe un tipo de documento con ese código.");
        return Ok(ADto(tipo));
    }

    [HttpPatch("tipos-documento/{idTipoDocumento:int}/estado")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    public async Task<ActionResult<TipoDocumentoAdminDto>> CambiarEstadoTipoDocumento(int idTipoDocumento, [FromBody] EstadoAdministracionEntrada request, CancellationToken ct)
    {
        if (!EsPlataforma) return Prohibido("Los tipos de documento son globales: solo el administrador de plataforma los administra.");
        var tipo = await Db.TiposDocumento.SingleOrDefaultAsync(x => x.IdTipoDocumento == idTipoDocumento, ct);
        if (tipo is null) return NoEncontrado("El tipo de documento no existe.");
        if (tipo.Activo != request.Activo!.Value)
        {
            tipo.Activo = request.Activo.Value;
            Auditar(null, tipo.Activo ? "Activar" : "Inactivar", "TipoDocumento", idTipoDocumento.ToString(), new { Activo = !tipo.Activo }, new { tipo.Activo });
            await Db.SaveChangesAsync(ct);
        }
        return Ok(ADto(tipo));
    }

    private Task<bool> NombreTipoUsadoAsync(string nombre, int? excluir, CancellationToken ct)
    {
        var normalizado = nombre.ToUpper();
        return Db.TiposPersona.AnyAsync(x => x.IdTipoPersona != excluir && x.Nombre.ToUpper() == normalizado, ct);
    }

    private static void AplicarDocumento(TipoDocumento tipo, TipoDocumentoEntrada request)
    {
        tipo.Codigo = Normalizar(request.Codigo);
        tipo.Nombre = request.Nombre.Trim();
        tipo.PaisEmisor = Limpiar(request.PaisEmisor);
        tipo.PatronValidacion = Limpiar(request.PatronValidacion);
    }

    private static TipoPersonaDto ADto(TipoPersona x) => new() { IdTipoPersona = x.IdTipoPersona, Nombre = x.Nombre, Descripcion = x.Descripcion, Activo = x.Activo };

    private static TipoDocumentoAdminDto ADto(TipoDocumento x) => new()
    {
        IdTipoDocumento = x.IdTipoDocumento, Codigo = x.Codigo, Nombre = x.Nombre, PaisEmisor = x.PaisEmisor, PatronValidacion = x.PatronValidacion, Activo = x.Activo,
    };
}
