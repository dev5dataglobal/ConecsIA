using ConecsIA.Api.Composicion;
using ConecsIA.Api.Observabilidad;
using ConecsIA.Api.Seguridad;
using ConecsIA.Api.Semillas;
using ConecsIA.Api.Sistema;

var builder = WebApplication.CreateBuilder(args);

builder.AgregarObservabilidad();
builder.Services
    .AgregarApi()
    .AgregarModulos(builder.Configuration)
    .AgregarPersistencia(builder.Configuration, builder.Environment)
    .AgregarSeguridad(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UsarCabeceraTraceId();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.json").AllowAnonymous();
}
else
{
    app.UseHsts();
}

app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<MiddlewareCsrf>();
app.UseMiddleware<MiddlewareContextoOrganizacion>();
app.UseAuthorization();

app.MapearSalud();
app.MapControllers();

if (Sembrador.FueSolicitado(args))
{
    // StartAsync ejecuta los servicios hospedados (migraciones locales) antes de sembrar.
    await app.StartAsync();
    await Sembrador.EjecutarAsync(app.Services, CancellationToken.None);
    await app.StopAsync();
    return;
}

if (Sembrador.SembrarAlIniciar(app.Configuration))
{
    // Entorno local con Docker Compose: datos sintéticos listos para probar el flujo (HU-003).
    await app.StartAsync();
    await Sembrador.EjecutarAsync(app.Services, CancellationToken.None);
    await app.WaitForShutdownAsync();
    return;
}

await app.RunAsync();

/// <summary>Punto de entrada expuesto para WebApplicationFactory en pruebas de integración.</summary>
public partial class Program;
