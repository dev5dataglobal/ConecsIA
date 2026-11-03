using System.Security.Claims;
using ApiConecsia.Helpers;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Services;

/// <summary>
/// Valida la cabecera de organización seleccionada en cada petición autenticada.
/// - Solo el administrador de plataforma puede cambiar de organización; debe existir y estar activa.
/// - Cualquier otro usuario que envíe una organización distinta de la de su token recibe 403.
/// </summary>
public sealed class ContextoOrganizacionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ConecsiaDbContext db)
    {
        var valor = context.Request.Headers[OrganizationContext.Cabecera].ToString();
        if (string.IsNullOrWhiteSpace(valor) || context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        if (!int.TryParse(valor, out var solicitada) || solicitada <= 0)
        {
            await Rechazar(context, StatusCodes.Status400BadRequest, "La organización seleccionada no es válida.");
            return;
        }

        var propia = int.TryParse(context.User.FindFirstValue(UserAuthorization.Claims.OrganizationId), out var id) ? id : (int?)null;
        if (!context.User.EsAdministradorGeneral())
        {
            if (solicitada != propia)
            {
                await Rechazar(context, StatusCodes.Status403Forbidden, "No puedes cambiar a otra organización.");
                return;
            }
            await next(context);
            return;
        }

        if (!await db.Organizaciones.AnyAsync(x => x.IdOrganizacion == solicitada && x.Estado == "Activa", context.RequestAborted))
        {
            await Rechazar(context, StatusCodes.Status403Forbidden, "La organización seleccionada no existe o está inactiva.");
            return;
        }

        context.Items[OrganizationContext.ClaveSeleccion] = solicitada;
        await next(context);
    }

    private static Task Rechazar(HttpContext context, int status, string detalle)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = status == 403 ? "Acceso denegado" : "Solicitud no válida", Detail = detalle },
            options: null, contentType: "application/problem+json");
    }
}
