using ConecsIA.Application.DTOs;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Services;

/// <summary>
/// Reglas compartidas de organizaciones. Desde ADR-028 la organización no se vincula con zonas empresariales:
/// la zona se resuelve desde cada sede, así que una organización puede tener sedes en varias zonas.
/// </summary>
public static class ReglasOrganizacion
{
    public static async Task<string?> ValidarDatosAsync(ConecsiaDbContext db, OrganizacionAdministracionEntrada request, CancellationToken ct)
    {
        if (!await db.TiposOrganizacion.AnyAsync(x => x.IdTipoOrganizacion == request.IdTipoOrganizacion && x.Activo, ct))
            return "El tipo de organización no existe o está inactivo.";
        if (!await db.Planes.AnyAsync(x => x.IdPlan == request.IdPlan && x.Activo, ct))
            return "El plan no existe o está inactivo.";
        return null;
    }

    public static async Task<string?> DuplicadoAsync(ConecsiaDbContext db, OrganizacionAdministracionEntrada request, int? idExcluir, CancellationToken ct)
    {
        var nombre = request.Nombre.Trim().ToUpper();
        if (await db.Organizaciones.AnyAsync(x => x.IdOrganizacion != idExcluir && x.Nombre.ToUpper() == nombre, ct))
            return "Ya existe una organización con ese nombre.";
        var identificacion = request.Identificacion.Trim().ToUpper();
        if (await db.Organizaciones.AnyAsync(x => x.IdOrganizacion != idExcluir && x.Identificacion != null && x.Identificacion.ToUpper() == identificacion, ct))
            return "Ya existe una organización con esa identificación.";
        return null;
    }

    public static void Aplicar(Organizacion entity, OrganizacionAdministracionEntrada request)
    {
        entity.Nombre = request.Nombre.Trim();
        entity.IdTipoOrganizacion = request.IdTipoOrganizacion;
        entity.IdPlan = request.IdPlan;
        entity.Identificacion = request.Identificacion.Trim();
        entity.Direccion = string.IsNullOrWhiteSpace(request.Direccion) ? null : request.Direccion.Trim();
        entity.Telefono = request.Telefono.Trim();
        entity.Correo = request.Correo.Trim();
        entity.Estado = request.Estado;
    }

    /// <summary>Proyección con tipo, plan y zonas empresariales de sus sedes (sin filtros de organización).</summary>
    public static IQueryable<OrganizacionAdministracionDto> Proyectar(ConecsiaDbContext db, IQueryable<Organizacion> origen)
    {
        var sedes = db.Sedes.IgnoreQueryFilters();
        return from org in origen
               select new OrganizacionAdministracionDto
               {
                   IdOrganizacion = org.IdOrganizacion, IdTipoOrganizacion = org.IdTipoOrganizacion,
                   TipoOrganizacion = db.TiposOrganizacion.Where(t => t.IdTipoOrganizacion == org.IdTipoOrganizacion).Select(t => t.Nombre).FirstOrDefault(),
                   IdPlan = org.IdPlan, Plan = db.Planes.Where(p => p.IdPlan == org.IdPlan).Select(p => p.Nombre).FirstOrDefault(),
                   Nombre = org.Nombre, Identificacion = org.Identificacion, Direccion = org.Direccion, Telefono = org.Telefono, Correo = org.Correo,
                   Estado = org.Estado, CreadoEn = org.CreadoEn, TieneLogo = org.IdArchivoLogo != null,
                   ZonasEmpresariales = db.ZonasEmpresariales
                       .Where(z => sedes.Any(s => s.IdOrganizacion == org.IdOrganizacion && s.IdZonaEmpresarial == z.IdZonaEmpresarial))
                       .OrderBy(z => z.Nombre).Select(z => z.Nombre).ToList(),
               };
    }
}
