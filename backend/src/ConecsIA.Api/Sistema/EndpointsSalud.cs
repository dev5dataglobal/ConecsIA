using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ConecsIA.Api.Sistema;

public static class EndpointsSalud
{
    public const string RutaVida = "/api/v1/health";
    public const string RutaPreparado = "/api/v1/health/ready";

    /// <summary>
    /// Vida: el proceso responde (sin dependencias). Preparado: incluye PostgreSQL.
    /// El cuerpo solo expone nombres y estados, nunca cadenas de conexión ni excepciones.
    /// </summary>
    public static WebApplication MapearSalud(this WebApplication app)
    {
        app.MapHealthChecks(RutaVida, new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = EscribirRespuesta,
        }).AllowAnonymous();
        app.MapHealthChecks(RutaPreparado, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = EscribirRespuesta,
        }).AllowAnonymous();
        return app;
    }

    private static Task EscribirRespuesta(HttpContext contexto, HealthReport reporte)
    {
        contexto.Response.ContentType = "application/json";
        var cuerpo = new
        {
            status = reporte.Status.ToString(),
            checks = reporte.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() }),
        };
        return contexto.Response.WriteAsync(JsonSerializer.Serialize(cuerpo));
    }
}
