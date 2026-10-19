using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Infrastructure.Persistencia;

public static class OpcionesPersistencia
{
    public const string EnsambladoMigraciones = "ConecsIA.Migrations";
    public const string TablaHistorialMigraciones = "__ef_migrations_history";

    /// <summary>
    /// Configuración común del proveedor (ADR-003): Npgsql, snake_case y migraciones en su propio
    /// ensamblado, que es el único que conoce a todos los módulos.
    /// </summary>
    public static DbContextOptionsBuilder Configurar(DbContextOptionsBuilder opciones, string cadenaConexion) =>
        opciones
            .UseNpgsql(cadenaConexion, npgsql => npgsql
                .MigrationsAssembly(EnsambladoMigraciones)
                .MigrationsHistoryTable(TablaHistorialMigraciones))
            .UseSnakeCaseNamingConvention();
}
