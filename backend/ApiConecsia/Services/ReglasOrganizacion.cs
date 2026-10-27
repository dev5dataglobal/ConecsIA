using ConecsIA.Application.DTOs;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Services;

/// <summary>
/// Reglas compartidas por organizaciones y zonas empresariales.
/// Zona empresarial = agrupación administrada por una organización superior (vínculo es_administradora) a la que
/// pertenecen empresas (vínculos no administradores). Reglas aplicadas (también con índices únicos parciales):
/// una organización superior activa por zona, una zona activa por empresa y sin anidamiento
/// (una organización superior no puede ser empresa de otra zona ni de la suya).
/// Los vínculos de otras organizaciones solo los escribe el administrador de plataforma.
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

    /// <summary>Zona activa a la que pertenece la empresa (vínculo no administrador), si existe.</summary>
    public static Task<ZonaEmpresarialOrganizacion?> ZonaDeEmpresaAsync(ConecsiaDbContext db, int idOrganizacion, CancellationToken ct) =>
        db.ZonasEmpresarialesOrganizaciones.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.IdOrganizacion == idOrganizacion && !x.EsAdministradora && x.Activa, ct);

    public static Task<bool> EsSuperiorAsync(ConecsiaDbContext db, int idOrganizacion, CancellationToken ct) =>
        db.ZonasEmpresarialesOrganizaciones.IgnoreQueryFilters().AnyAsync(x => x.IdOrganizacion == idOrganizacion && x.EsAdministradora && x.Activa, ct);

    /// <summary>
    /// Deja a la empresa en la zona indicada (o en ninguna). No borra vínculos: los desactiva para conservar la historia.
    /// Requiere un ámbito de escritura que incluya a la empresa (<see cref="ConecsiaDbContext.PermitirEscrituraEn"/>).
    /// </summary>
    public static async Task<string?> AsignarZonaAsync(ConecsiaDbContext db, int idEmpresa, int? idZona, CancellationToken ct)
    {
        var actual = await ZonaDeEmpresaAsync(db, idEmpresa, ct);
        if (actual?.IdZonaEmpresarial == idZona) return null;
        if (idZona is int zona)
        {
            if (!await db.ZonasEmpresariales.AnyAsync(x => x.IdZonaEmpresarial == zona && x.Activa, ct))
                return "La zona empresarial no existe o está inactiva.";
            if (await EsSuperiorAsync(db, idEmpresa, ct))
                return "Una organización superior no puede pertenecer a una zona empresarial.";
        }
        if (actual is not null) actual.Activa = false;
        if (idZona is int nueva)
        {
            var existente = await db.ZonasEmpresarialesOrganizaciones.IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.IdOrganizacion == idEmpresa && x.IdZonaEmpresarial == nueva, ct);
            if (existente is null)
                db.ZonasEmpresarialesOrganizaciones.Add(new ZonaEmpresarialOrganizacion { IdOrganizacion = idEmpresa, IdZonaEmpresarial = nueva, EsAdministradora = false, Activa = true });
            else
                existente.Activa = true;
        }
        return null;
    }

    /// <summary>Proyección con tipo, plan, zona y organización superior (sin filtros de organización).</summary>
    public static IQueryable<OrganizacionAdministracionDto> Proyectar(ConecsiaDbContext db, IQueryable<Organizacion> origen)
    {
        var links = db.ZonasEmpresarialesOrganizaciones.IgnoreQueryFilters();
        return from org in origen
               let pertenencia = links.Where(l => l.IdOrganizacion == org.IdOrganizacion && !l.EsAdministradora && l.Activa).Select(l => l.IdZonaEmpresarial).FirstOrDefault()
               select new OrganizacionAdministracionDto
               {
                   IdOrganizacion = org.IdOrganizacion, IdTipoOrganizacion = org.IdTipoOrganizacion,
                   TipoOrganizacion = db.TiposOrganizacion.Where(t => t.IdTipoOrganizacion == org.IdTipoOrganizacion).Select(t => t.Nombre).FirstOrDefault(),
                   IdPlan = org.IdPlan, Plan = db.Planes.Where(p => p.IdPlan == org.IdPlan).Select(p => p.Nombre).FirstOrDefault(),
                   Nombre = org.Nombre, Identificacion = org.Identificacion, Direccion = org.Direccion, Telefono = org.Telefono, Correo = org.Correo,
                   Estado = org.Estado, CreadoEn = org.CreadoEn, TieneLogo = org.IdArchivoLogo != null,
                   IdZonaEmpresarial = pertenencia == 0 ? null : pertenencia,
                   ZonaEmpresarial = db.ZonasEmpresariales.Where(z => z.IdZonaEmpresarial == pertenencia).Select(z => z.Nombre).FirstOrDefault(),
                   OrganizacionSuperior = (from l in links
                                           join sup in db.Organizaciones on l.IdOrganizacion equals sup.IdOrganizacion
                                           where l.IdZonaEmpresarial == pertenencia && l.EsAdministradora && l.Activa
                                           select sup.Nombre).FirstOrDefault(),
                   EsOrganizacionSuperior = links.Any(l => l.IdOrganizacion == org.IdOrganizacion && l.EsAdministradora && l.Activa),
               };
    }
}
