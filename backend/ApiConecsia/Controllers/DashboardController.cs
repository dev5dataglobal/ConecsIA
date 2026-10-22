using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Controllers;

[Route("api/dashboard")]
public sealed class DashboardController(ConecsiaDbContext db, IOrganizationContext organizationContext) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> Get([FromQuery] string periodo = "7d", [FromQuery] int? sedeId = null, CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is null)
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Organización no disponible", detail: "El usuario autenticado no tiene una organización válida asociada.");
        if (periodo is not ("hoy" or "7d")) return BadRequest("El periodo debe ser hoy o 7d.");
        if (sedeId.HasValue && !await db.Sedes.AnyAsync(x => x.IdSede == sedeId.Value, cancellationToken))
            return BadRequest("La sede no pertenece a la organización autenticada.");

        var now = DateTime.UtcNow;
        var fromDate = periodo == "hoy" ? now.Date : now.Date.AddDays(-6);
        var org = await db.Organizaciones.Where(x => x.IdOrganizacion == organizationContext.OrganizationId.Value)
            .Select(x => new DashboardOrganizationDto { IdOrganizacion = x.IdOrganizacion, Nombre = x.Nombre }).SingleOrDefaultAsync(cancellationToken);
        if (org is null) return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Organización no disponible", detail: "La organización del usuario no está activa o no existe.");

        var sedes = await db.Sedes.Where(x => !sedeId.HasValue || x.IdSede == sedeId.Value).OrderBy(x => x.Nombre)
            .Select(x => new DashboardSedeDto { IdSede = x.IdSede, Nombre = x.Nombre }).ToListAsync(cancellationToken);
        var permanencias = db.Permanencias.Where(x => x.Estado == "Abierta" && (!sedeId.HasValue || x.IdSede == sedeId.Value));
        var visitas = db.Visitas.Where(x => x.CreadoEn >= fromDate && x.CreadoEn <= now && (!sedeId.HasValue || x.IdSede == sedeId.Value));
        var autorizaciones = db.Autorizaciones.Where(x => x.Estado == "Activa" && (!sedeId.HasValue || x.IdSede == sedeId.Value));
        var alertas = db.Alertas.Where(x => x.Estado != "Resuelta" && x.Estado != "Cerrada" && (!sedeId.HasValue || x.IdSede == sedeId.Value));
        var resumen = new DashboardResumenDto { PersonasDentro = await permanencias.CountAsync(cancellationToken), Visitas = await visitas.CountAsync(cancellationToken), AutorizacionesActivas = await autorizaciones.CountAsync(cancellationToken), AlertasPendientes = await alertas.CountAsync(cancellationToken) };
        var visitasPorDia = (await visitas.Select(x => x.CreadoEn).ToListAsync(cancellationToken)).GroupBy(DateOnly.FromDateTime).Select(x => new DashboardSerieDto { Fecha = x.Key, Total = x.Count() }).OrderBy(x => x.Fecha).ToList();
        var distribucion = await (from movimiento in db.MovimientosAcceso
                                  join persona in db.Personas on movimiento.IdPersona equals persona.IdPersona
                                  join tipo in db.TiposPersona on persona.IdTipoPersona equals tipo.IdTipoPersona
                                  where movimiento.TipoMovimiento == "Entrada" && movimiento.Resultado == "Confirmado" && movimiento.OcurridoEn >= fromDate && movimiento.OcurridoEn <= now && (!sedeId.HasValue || movimiento.IdSede == sedeId.Value)
                                  group tipo by tipo.Nombre into grupo
                                  select new DashboardDistribucionDto { Tipo = grupo.Key, Total = grupo.Count() }).OrderByDescending(x => x.Total).ToListAsync(cancellationToken);
        var personasDentro = await (from permanencia in permanencias
                                    join persona in db.Personas on permanencia.IdPersona equals persona.IdPersona
                                    join tipo in db.TiposPersona on persona.IdTipoPersona equals tipo.IdTipoPersona into tipos
                                    from tipo in tipos.DefaultIfEmpty()
                                    orderby permanencia.AbiertaEn descending
                                    select new DashboardPersonaDentroDto { IdPersona = persona.IdPersona, Persona = persona.Nombres + " " + persona.Apellidos, Tipo = tipo == null ? "Sin tipo" : tipo.Nombre, EntradaEn = permanencia.AbiertaEn, Estado = permanencia.Estado }).Take(10).ToListAsync(cancellationToken);
        var actividadesMovimiento = await (from movimiento in db.MovimientosAcceso
                                           join persona in db.Personas on movimiento.IdPersona equals persona.IdPersona
                                           where movimiento.OcurridoEn >= fromDate && movimiento.OcurridoEn <= now && (!sedeId.HasValue || movimiento.IdSede == sedeId.Value)
                                           select new DashboardActividadDto { Tipo = movimiento.TipoMovimiento == "Entrada" ? "entrada" : "salida", Titulo = movimiento.TipoMovimiento == "Entrada" ? "Entrada registrada" : "Salida registrada", Detalle = persona.Nombres + " " + persona.Apellidos, OcurrioEn = movimiento.OcurridoEn }).ToListAsync(cancellationToken);
        var actividadesAlertas = await alertas.Select(x => new DashboardActividadDto { Tipo = "alerta", Titulo = "Alerta pendiente", Detalle = x.Descripcion, OcurrioEn = x.GeneradaEn }).ToListAsync(cancellationToken);
        var actividad = actividadesMovimiento.Concat(actividadesAlertas).OrderByDescending(x => x.OcurrioEn).Take(8).ToList();
        return Ok(new DashboardDto { Organizacion = org, Sedes = sedes, Resumen = resumen, VisitasPorDia = visitasPorDia, DistribucionPorTipo = distribucion, PersonasDentro = personasDentro, ActividadReciente = actividad });
    }
}
