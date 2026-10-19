using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ConecsIA.Api.Observabilidad;

public static class ExtensionesObservabilidad
{
    public const string NombreServicio = "conecsia-api";

    /// <summary>
    /// ADR-018: logs JSON redactados en consola y métricas/trazas OpenTelemetry. El exportador
    /// OTLP solo se activa si se configura OTEL_EXPORTER_OTLP_ENDPOINT (destino pendiente, D-13).
    /// </summary>
    public static WebApplicationBuilder AgregarObservabilidad(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(o => o.FormatterName = FormateadorJsonRedactado.Nombre);
        builder.Logging.AddConsoleFormatter<FormateadorJsonRedactado, Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions>();

        var otlp = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(NombreServicio))
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation(o => o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/api/v1/health"))
                    .AddHttpClientInstrumentation();
                if (otlp) t.AddOtlpExporter();
            })
            .WithMetrics(m =>
            {
                m.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
                if (otlp) m.AddOtlpExporter();
            });

        return builder;
    }

    /// <summary>Expone el traceId en la respuesta para que soporte pueda correlacionar.</summary>
    public static IApplicationBuilder UsarCabeceraTraceId(this IApplicationBuilder app) =>
        app.Use(async (contexto, siguiente) =>
        {
            contexto.Response.OnStarting(() =>
            {
                var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? contexto.TraceIdentifier;
                contexto.Response.Headers["X-Trace-Id"] = traceId;
                return Task.CompletedTask;
            });
            await siguiente(contexto);
        });
}
