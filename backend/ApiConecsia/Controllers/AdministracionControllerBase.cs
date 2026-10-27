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

    protected void Auditar(int? idOrganizacion, string accion, string entidad, string idRegistro, object? anterior = null, object? nuevo = null)
    {
        db.Auditorias.Add(new Auditoria
        {
            IdOrganizacion = idOrganizacion,
            TipoActor = "Usuario",
            IdUsuario = User.IdUsuario(),
            Accion = accion,
            Entidad = entidad,
            IdRegistro = idRegistro,
            Resultado = "Exitoso",
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

    protected static string Normalizar(string texto) => texto.Trim().ToUpperInvariant();

    protected static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
