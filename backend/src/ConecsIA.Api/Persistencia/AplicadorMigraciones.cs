using ConecsIA.Api.Composicion;
using ConecsIA.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Api.Persistencia;

/// <summary>
/// Solo para desarrollo local: aplica migraciones con el rol de migración, que es distinto del rol
/// de la aplicación (ADR-003). Nunca se registra en producción.
/// </summary>
public sealed partial class AplicadorMigraciones(
    IConfiguration configuracion,
    EnsambladosDelModelo ensamblados,
    ILogger<AplicadorMigraciones> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var cadena = configuracion.GetConnectionString(ExtensionesServicios.CadenaMigraciones)
                     ?? throw new InvalidOperationException(
                         $"Falta la cadena de conexión '{ExtensionesServicios.CadenaMigraciones}'.");

        var opciones = OpcionesPersistencia
            .Configurar(new DbContextOptionsBuilder<ConecsIADbContext>(), cadena)
            .Options;

        await using var contexto = new ConecsIADbContext((DbContextOptions<ConecsIADbContext>)opciones, ensamblados);
        var pendientes = (await contexto.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        await contexto.Database.MigrateAsync(cancellationToken);
        RegistrarMigraciones(logger, pendientes.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Migraciones aplicadas: {Cantidad}")]
    private static partial void RegistrarMigraciones(ILogger logger, int cantidad);
}
