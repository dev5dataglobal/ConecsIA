using ConecsIA.Infrastructure.Seguridad;

namespace ConecsIA.Api.Semillas;

/// <summary>
/// Carga de datos sintéticos para desarrollo (HU-003). Solo se permite en Development: nunca hay
/// datos reales hasta que se defina la privacidad (D-14). Cada conjunto es idempotente.
/// </summary>
public static partial class Sembrador
{
    public const string Argumento = "--sembrar";
    public const string ClaveSembrarAlIniciar = "Semillas:SembrarAlIniciar";

    public static bool FueSolicitado(string[] args) => args.Contains(Argumento, StringComparer.Ordinal);

    public static bool SembrarAlIniciar(IConfiguration configuracion) => configuracion.GetValue<bool>(ClaveSembrarAlIniciar);

    public static void ValidarEntorno(IHostEnvironment entorno)
    {
        if (!entorno.IsDevelopment())
        {
            throw new InvalidOperationException(
                $"El sembrado de datos sintéticos solo está permitido en Development (entorno actual: {entorno.EnvironmentName}).");
        }
    }

    public static async Task EjecutarAsync(IServiceProvider servicios, CancellationToken cancelacion)
    {
        ValidarEntorno(servicios.GetRequiredService<IHostEnvironment>());

        await using var ambito = servicios.CreateAsyncScope();
        // Proceso interno sin petición HTTP: se identifica como sistema para escribir en varias organizaciones.
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        foreach (var conjunto in ambito.ServiceProvider.GetServices<IConjuntoSemillas>())
        {
            await conjunto.SembrarAsync(cancelacion);
        }

        var logger = servicios.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(Sembrador));
        RegistrarFin(logger);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sembrado sintético completado")]
    private static partial void RegistrarFin(ILogger logger);
}

/// <summary>Conjunto de semillas idempotente: ejecutarlo dos veces no duplica registros.</summary>
public interface IConjuntoSemillas
{
    Task SembrarAsync(CancellationToken cancelacion);
}
