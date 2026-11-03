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
/// Zonas empresariales (ADR-028): agrupación física de sedes, sin vínculo con organizaciones. Cada sede pertenece a cero o una
/// zona y la zona se resuelve siempre desde la sede; las áreas y puntos comunes de la zona los administra la organización
/// dueña de la sede. Crear, editar e inactivar zonas es un catálogo de plataforma: solo el administrador de plataforma.
/// Las demás organizaciones consultan las zonas en las que tienen sedes y solo ven sus propias sedes.
/// </summary>
[Route("api/zonas-empresariales")]
public sealed class ZonaEmpresarialController(ConecsiaDbContext db, IOrganizationContext organizationContext, IModuloAccesoService accesos)
    : AdministracionControllerBase(db, organizationContext, accesos)
{
    private IQueryable<Sede> TodasLasSedes => Db.Sedes.IgnoreQueryFilters();

    [HttpGet]
    [RequiereModulo(CodigosModulo.ZonasEmpresariales, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<ZonaEmpresarialAdministracionDto>>> Listar([FromQuery] string? buscar = null, [FromQuery] bool? activa = null, CancellationToken ct = default)
    {
        var query = Visibles();
        if (activa is bool valor) query = query.Where(x => x.Activa == valor);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = $"%{buscar.Trim()}%";
            query = query.Where(z => EF.Functions.ILike(z.Nombre, texto) || (z.Descripcion != null && EF.Functions.ILike(z.Descripcion, texto)));
        }
        return Ok(await MapearAsync(await query.OrderBy(x => x.Nombre).ToListAsync(ct), ct));
    }

    [HttpGet("{id:int}")]
    [RequiereModulo(CodigosModulo.ZonasEmpresariales, AccionModulo.Consultar)]
    public async Task<ActionResult<ZonaEmpresarialAdministracionDto>> Obtener(int id, CancellationToken ct)
    {
        var zona = await Visibles().SingleOrDefaultAsync(x => x.IdZonaEmpresarial == id, ct);
        return zona is null ? NoEncontrado("La zona empresarial no existe o no está en tu alcance.") : Ok((await MapearAsync([zona], ct))[0]);
    }

    [HttpPost]
    [RequiereModulo(CodigosModulo.ZonasEmpresariales, AccionModulo.Anadir)]
    public async Task<ActionResult<ZonaEmpresarialAdministracionDto>> Crear([FromBody] ZonaEmpresarialAdministracionEntrada request, CancellationToken ct)
    {
        if (!EsPlataforma) return Prohibido("Solo el administrador de plataforma puede crear zonas empresariales.");
        if (await NombreDuplicadoAsync(request.Nombre, null, ct)) return Conflicto("Ya existe una zona empresarial con ese nombre.");
        var zona = new ZonaEmpresarial { Nombre = request.Nombre.Trim(), Descripcion = Limpiar(request.Descripcion), Activa = request.Activa, CreadaEn = DateTime.UtcNow };
        Db.ZonasEmpresariales.Add(zona);
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe una zona empresarial con ese nombre.");
        Auditar(null, "Crear", "ZonaEmpresarial", zona.IdZonaEmpresarial.ToString(), null, new { zona.Nombre, zona.Descripcion, zona.Activa });
        await Db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = zona.IdZonaEmpresarial }, (await MapearAsync([zona], ct))[0]);
    }

    [HttpPut("{id:int}")]
    [RequiereModulo(CodigosModulo.ZonasEmpresariales, AccionModulo.Editar)]
    public async Task<ActionResult<ZonaEmpresarialAdministracionDto>> Editar(int id, [FromBody] ZonaEmpresarialAdministracionEntrada request, CancellationToken ct)
    {
        if (!EsPlataforma) return Prohibido("Solo el administrador de plataforma puede editar zonas empresariales.");
        var zona = await Db.ZonasEmpresariales.SingleOrDefaultAsync(x => x.IdZonaEmpresarial == id, ct);
        if (zona is null) return NoEncontrado("La zona empresarial no existe.");
        if (await NombreDuplicadoAsync(request.Nombre, id, ct)) return Conflicto("Ya existe una zona empresarial con ese nombre.");

        var anterior = new { zona.Nombre, zona.Descripcion, zona.Activa };
        zona.Nombre = request.Nombre.Trim(); zona.Descripcion = Limpiar(request.Descripcion); zona.Activa = request.Activa;
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe una zona empresarial con ese nombre.");
        Auditar(null, "Modificar", "ZonaEmpresarial", id.ToString(), anterior, new { zona.Nombre, zona.Descripcion, zona.Activa });
        await Db.SaveChangesAsync(ct);
        return Ok((await MapearAsync([zona], ct))[0]);
    }

    /// <summary>
    /// Activa o inactiva la zona. No toca sedes, áreas, puntos ni autorizaciones: una zona inactiva solo deja de ofrecerse
    /// para nuevas asignaciones de sede.
    /// </summary>
    [HttpPatch("{id:int}/estado")]
    [RequiereModulo(CodigosModulo.ZonasEmpresariales, AccionModulo.Editar)]
    public async Task<ActionResult<ZonaEmpresarialAdministracionDto>> CambiarEstado(int id, [FromBody] EstadoAdministracionEntrada request, CancellationToken ct)
    {
        if (!EsPlataforma) return Prohibido("Solo el administrador de plataforma puede activar o inactivar zonas empresariales.");
        var zona = await Db.ZonasEmpresariales.SingleOrDefaultAsync(x => x.IdZonaEmpresarial == id, ct);
        if (zona is null) return NoEncontrado("La zona empresarial no existe.");
        var anterior = zona.Activa;
        zona.Activa = request.Activo!.Value;
        Auditar(null, zona.Activa ? "Activar" : "Inactivar", "ZonaEmpresarial", id.ToString(), new { Activa = anterior }, new { zona.Activa });
        await Db.SaveChangesAsync(ct);
        return Ok((await MapearAsync([zona], ct))[0]);
    }

    // ---------- Auxiliares ----------

    /// <summary>Plataforma: todas; los demás: zonas en las que la organización activa tiene alguna sede.</summary>
    private IQueryable<ZonaEmpresarial> Visibles()
    {
        if (EsPlataforma) return Db.ZonasEmpresariales;
        var orgId = OrganizacionActiva;
        return Db.ZonasEmpresariales.Where(z => TodasLasSedes.Any(s => s.IdOrganizacion == orgId && s.IdZonaEmpresarial == z.IdZonaEmpresarial));
    }

    private Task<bool> NombreDuplicadoAsync(string nombre, int? excluir, CancellationToken ct)
    {
        var normalizado = nombre.Trim().ToUpper();
        return Db.ZonasEmpresariales.AnyAsync(x => x.IdZonaEmpresarial != excluir && x.Nombre.ToUpper() == normalizado, ct);
    }

    /// <summary>Sedes de cada zona: la plataforma ve todas; las demás organizaciones solo las propias (sin datos de otras empresas).</summary>
    private async Task<List<ZonaEmpresarialAdministracionDto>> MapearAsync(IReadOnlyList<ZonaEmpresarial> zonas, CancellationToken ct)
    {
        var ids = zonas.Select(x => x.IdZonaEmpresarial).ToArray();
        var sedes = TodasLasSedes.Where(s => s.IdZonaEmpresarial != null && ids.Contains(s.IdZonaEmpresarial.Value));
        if (!EsPlataforma)
        {
            var orgId = OrganizacionActiva;
            sedes = sedes.Where(s => s.IdOrganizacion == orgId);
        }
        var filas = await (from s in sedes
                           join o in Db.Organizaciones on s.IdOrganizacion equals o.IdOrganizacion
                           orderby o.Nombre, s.Nombre
                           select new { s.IdZonaEmpresarial, Dto = new SedeZonaDto { IdSede = s.IdSede, Sede = s.Nombre, Organizacion = o.Nombre, Activo = s.Activo } })
            .ToListAsync(ct);

        return zonas.Select(zona =>
        {
            var propias = filas.Where(x => x.IdZonaEmpresarial == zona.IdZonaEmpresarial).Select(x => x.Dto).ToList();
            return new ZonaEmpresarialAdministracionDto
            {
                IdZonaEmpresarial = zona.IdZonaEmpresarial, Nombre = zona.Nombre, Descripcion = zona.Descripcion, Activa = zona.Activa, CreadaEn = zona.CreadaEn,
                TotalSedes = propias.Count, Sedes = propias,
            };
        }).ToList();
    }
}
