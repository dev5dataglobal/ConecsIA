using ApiConecsia.Extensions;
using ConecsIA.Application.DTOs;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ApiConecsia.Controllers;

/// <summary>
/// Recepción de solicitudes de información desde la página pública de presentación. Anónimo y con límite por IP;
/// solo inserta la solicitud (no lee datos de organizaciones) y no devuelve lo almacenado.
/// </summary>
[ApiController]
[Route("api/publico/contactos")]
public sealed class ContactoController(ConecsiaDbContext db, ILogger<ContactoController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting(RateLimitingExtensions.PoliticaContacto)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Crear([FromBody] SolicitudContactoEntrada entrada, CancellationToken cancellationToken)
    {
        // Campo trampa con valor: misma respuesta que un envío válido para no orientar al bot, pero no se guarda.
        if (!string.IsNullOrWhiteSpace(entrada.SitioWeb)) return Accepted();

        var tipo = SolicitudContactoEntrada.TiposOrganizacion.FirstOrDefault(t => t == entrada.TipoOrganizacion.Trim());
        if (tipo is null)
        {
            ModelState.AddModelError(nameof(entrada.TipoOrganizacion), "El tipo de organización no es válido.");
            return ValidationProblem(ModelState);
        }

        var solicitud = new SolicitudContacto
        {
            Organizacion = entrada.Organizacion.Trim(),
            NombreContacto = entrada.Nombre.Trim(),
            Correo = entrada.Correo.Trim().ToLowerInvariant(),
            Telefono = string.IsNullOrWhiteSpace(entrada.Telefono) ? null : entrada.Telefono.Trim(),
            TipoOrganizacion = tipo,
            NumeroSedes = entrada.NumeroSedes,
            Mensaje = entrada.Mensaje.Trim(),
            FechaCreacion = DateTime.UtcNow,
        };
        db.SolicitudesContacto.Add(solicitud);
        await db.SaveChangesAsync(cancellationToken);

        // Sin datos personales en el log (ADR-018).
        logger.LogInformation("Solicitud de contacto {IdSolicitud} registrada", solicitud.IdSolicitudContacto);
        return Accepted();
    }
}
