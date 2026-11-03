using System.Text.Json;
using ApiConecsia.Helpers;
using ConecsIA.Application.Interfaces;
using ConecsIA.Application.Seguridad;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ApiConecsia.Controllers;

/// <summary>
/// Base de los endpoints de administración. Todo se resuelve contra la organización activa validada en servidor
/// (<see cref="IOrganizationContext"/>); el cliente nunca elige la organización por ruta, filtro ni cuerpo.
/// La acción por módulo la exige <see cref="RequiereModuloAttribute"/> en cada endpoint.
/// </summary>
public abstract class AdministracionControllerBase(ConecsiaDbContext db, IOrganizationContext organizationContext, IModuloAccesoService accesos)
    : BaseApiController
{
    protected ConecsiaDbContext Db => db;
    protected IModuloAccesoService Accesos => accesos;

    /// <summary>Organización activa (siempre presente: <see cref="RequiereModuloAttribute"/> deniega sin ella).</summary>
    protected int OrganizacionActiva => organizationContext.OrganizationId
        ?? throw new UnauthorizedAccessException("No hay organización activa.");

    protected bool HayOrganizacionActiva => organizationContext.OrganizationId is not null;

    protected bool EsPlataforma => User.EsAdministradorGeneral();

    protected Task<IReadOnlyDictionary<string, AccionesModulo>> AccesosPropiosAsync(CancellationToken ct) =>
        accesos.ObtenerAccesosAsync(User.IdUsuario()!.Value, OrganizacionActiva, EsPlataforma, ct);

    protected ObjectResult Prohibido(string detalle) =>
        Problem(statusCode: StatusCodes.Status403Forbidden, title: "Acceso denegado", detail: detalle);

    protected ObjectResult Conflicto(string detalle) =>
        Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflicto", detail: detalle);

    protected ObjectResult Invalido(string detalle) =>
        Problem(statusCode: StatusCodes.Status400BadRequest, title: "Solicitud no válida", detail: detalle);

    protected ObjectResult NoEncontrado(string detalle) =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "No encontrado", detail: detalle);

    protected void Auditar(int? idOrganizacion, string accion, string entidad, string idRegistro, object? anterior = null, object? nuevo = null, string resultado = "Exitoso")
    {
        db.Auditorias.Add(new Auditoria
        {
            IdOrganizacion = idOrganizacion,
            TipoActor = "Usuario",
            IdUsuario = User.IdUsuario(),
            Accion = accion,
            Entidad = entidad,
            IdRegistro = idRegistro,
            Resultado = resultado,
            ValorAnterior = anterior is null ? null : JsonSerializer.Serialize(anterior),
            ValorNuevo = nuevo is null ? null : JsonSerializer.Serialize(nuevo),
            DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            IdCorrelacion = HttpContext.TraceIdentifier,
            OcurridoEn = DateTime.UtcNow,
        });
    }

    /// <summary>Guarda; devuelve false ante una violación de unicidad (doble envío o carrera con otro usuario).</summary>
    protected async Task<bool> GuardarAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.ChangeTracker.Clear();
            return false;
        }
    }

    // ---------- Alcance por sede (D-34, Propuesta) ----------

    protected async Task<int?> MembresiaPropiaAsync(CancellationToken ct)
    {
        var idUsuario = User.IdUsuario()!.Value;
        return await Db.MembresiasOrganizacion.AsNoTracking()
            .Where(x => x.IdUsuario == idUsuario && (x.Estado == "Activa" || x.Estado == "Aceptada"))
            .Select(x => (int?)x.IdMembresia).FirstOrDefaultAsync(ct);
    }

    /// <summary>Sedes asignadas a la membresía; null = todas las de la organización (sin asignaciones o plataforma).</summary>
    protected async Task<List<int>?> SedesAlcanceAsync(CancellationToken ct)
    {
        var membresia = await MembresiaPropiaAsync(ct);
        if (membresia is null) return EsPlataforma ? null : [];
        var sedes = await Db.MembresiasSedes.AsNoTracking().Where(x => x.IdMembresia == membresia).Select(x => x.IdSede).ToListAsync(ct);
        return sedes.Count == 0 ? null : sedes;
    }

    protected static bool EnAlcance(List<int>? alcance, int idSede) => alcance is null || alcance.Contains(idSede);

    /// <summary>Membresía activa de la organización; si el destino tiene responsables designados, debe ser uno de ellos.</summary>
    protected async Task<string?> ValidarResponsableAsync(int idArea, int idMembresia, CancellationToken ct)
    {
        var activa = await Db.MembresiasOrganizacion.AnyAsync(x => x.IdMembresia == idMembresia && (x.Estado == "Activa" || x.Estado == "Aceptada")
            && Db.Usuarios.Any(u => u.IdUsuario == x.IdUsuario && u.Activo), ct);
        if (!activa) return "El responsable no es un usuario activo de tu organización.";
        var designados = await Db.AreasResponsables.Where(x => x.IdArea == idArea && x.Activo).Select(x => x.IdMembresia).ToListAsync(ct);
        return designados.Count > 0 && !designados.Contains(idMembresia) ? "El responsable no está designado para este destino." : null;
    }

    protected static string Normalizar(string texto) => texto.Trim().ToUpperInvariant();

    protected static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
