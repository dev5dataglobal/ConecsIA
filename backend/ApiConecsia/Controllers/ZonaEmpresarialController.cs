using ConecsIA.Application.Seguridad;
using ApiConecsia.Helpers;
using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces;
using ConecsIA.Infrastructure.Data;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Controllers;

[Route("api/zonas-empresariales")]
[RequiereModulo(CodigosModulo.Sedes)]
public sealed class ZonaEmpresarialController(ConecsiaDbContext db, IOrganizationContext organizationContext) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ZonaEmpresarialDto>>> Get(CancellationToken cancellationToken)
    {
        if (!CanRead()) return Forbid();
        if (organizationContext.OrganizationId is not int orgId) return Forbid();
        var query = from zone in db.ZonasEmpresariales
                    join link in db.ZonasEmpresarialesOrganizaciones on zone.IdZonaEmpresarial equals link.IdZonaEmpresarial into links
                    where links.Any(x => x.IdOrganizacion == orgId && x.Activa)
                    select new ZonaEmpresarialDto { IdZonaEmpresarial = zone.IdZonaEmpresarial, Nombre = zone.Nombre, Descripcion = zone.Descripcion, Activa = zone.Activa, CreadaEn = zone.CreadaEn, TotalSedes = db.Sedes.Count(x => x.IdZonaEmpresarial == zone.IdZonaEmpresarial) };
        return Ok(await query.OrderBy(x => x.Nombre).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ZonaEmpresarialDto>> Get(int id, CancellationToken cancellationToken)
    {
        if (!CanRead()) return Forbid();
        if (organizationContext.OrganizationId is not int orgId) return Forbid();
        var result = await db.ZonasEmpresariales.Where(x => x.IdZonaEmpresarial == id && db.ZonasEmpresarialesOrganizaciones.Any(l => l.IdZonaEmpresarial == x.IdZonaEmpresarial && l.IdOrganizacion == orgId && l.Activa)).Select(x => new ZonaEmpresarialDto { IdZonaEmpresarial = x.IdZonaEmpresarial, Nombre = x.Nombre, Descripcion = x.Descripcion, Activa = x.Activa, CreadaEn = x.CreadaEn, TotalSedes = db.Sedes.Count(s => s.IdZonaEmpresarial == x.IdZonaEmpresarial) }).SingleOrDefaultAsync(cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ZonaEmpresarialDto>> Post([FromBody] ZonaEmpresarialEntrada request, CancellationToken cancellationToken)
    {
        if (!CanAdminister()) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Nombre)) return BadRequest("El nombre es obligatorio.");
        if (await db.ZonasEmpresariales.AnyAsync(x => x.Nombre.ToUpper() == request.Nombre.Trim().ToUpper(), cancellationToken)) return Conflict("Ya existe una zona empresarial con ese nombre.");
        if (organizationContext.OrganizationId is not int orgId) return Forbid();
        if (request.IdSedeInicial is not int sedeId || !await db.Sedes.AnyAsync(x => x.IdSede == sedeId && x.IdOrganizacion == orgId, cancellationToken)) return BadRequest("Debe indicar una sede de la organización autenticada para crear la zona.");
        var entity = new ZonaEmpresarial { Nombre = request.Nombre.Trim(), Descripcion = request.Descripcion?.Trim(), Activa = request.Activa, CreadaEn = DateTime.UtcNow };
        db.ZonasEmpresariales.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        db.ZonasEmpresarialesOrganizaciones.Add(new ZonaEmpresarialOrganizacion { IdOrganizacion = orgId, IdZonaEmpresarial = entity.IdZonaEmpresarial, EsAdministradora = true });
        var sede = await db.Sedes.SingleAsync(x => x.IdSede == sedeId, cancellationToken); sede.IdZonaEmpresarial = entity.IdZonaEmpresarial;
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = entity.IdZonaEmpresarial }, new ZonaEmpresarialDto { IdZonaEmpresarial = entity.IdZonaEmpresarial, Nombre = entity.Nombre, Descripcion = entity.Descripcion, Activa = entity.Activa, CreadaEn = entity.CreadaEn, TotalSedes = 1 });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, [FromBody] ZonaEmpresarialEntrada request, CancellationToken cancellationToken)
    {
        if (!CanAdminister()) return Forbid();
        var entity = await db.ZonasEmpresariales.SingleOrDefaultAsync(x => x.IdZonaEmpresarial == id, cancellationToken);
        if (entity is null) return NotFound();
        if (!await IsZoneAdministrator(id, cancellationToken)) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Nombre)) return BadRequest("El nombre es obligatorio.");
        if (await db.ZonasEmpresariales.AnyAsync(x => x.IdZonaEmpresarial != id && x.Nombre.ToUpper() == request.Nombre.Trim().ToUpper(), cancellationToken)) return Conflict("Ya existe una zona empresarial con ese nombre.");
        entity.Nombre = request.Nombre.Trim(); entity.Descripcion = request.Descripcion?.Trim(); entity.Activa = request.Activa;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!CanAdminister() || !await IsZoneAdministrator(id, cancellationToken)) return Forbid();
        var entity = await db.ZonasEmpresariales.SingleOrDefaultAsync(x => x.IdZonaEmpresarial == id, cancellationToken);
        if (entity is null) return NotFound();
        if (await db.Sedes.AnyAsync(x => x.IdZonaEmpresarial == id, cancellationToken)) return Conflict("No se puede eliminar una zona empresarial con sedes asociadas; desactívela.");
        entity.Activa = false;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private bool CanRead() => User.HasClaim("permission", AdministracionAuthorization.ZonasEmpresarialesConsultar) || CanAdminister();
    private bool CanAdminister() => User.HasClaim("permission", AdministracionAuthorization.ZonasEmpresarialesAdministrar);
    private Task<bool> IsZoneAdministrator(int id, CancellationToken ct) => organizationContext.OrganizationId is int orgId
        ? db.ZonasEmpresarialesOrganizaciones.AnyAsync(x => x.IdOrganizacion == orgId && x.IdZonaEmpresarial == id && x.EsAdministradora && x.Activa, ct)
        : Task.FromResult(false);
}
