using Npgsql;
using Testcontainers.PostgreSql;

namespace ConecsIA.IntegrationTests.Infraestructura;

/// <summary>
/// PostgreSQL real en contenedor (PRD §12.7), inicializado con el mismo script de roles que el
/// entorno local: rol de migración y rol de aplicación separados (ADR-003).
/// </summary>
public sealed class BaseDatosFixture : IAsyncLifetime
{
    private const string BaseDatos = "conecsia";
    private const string ClaveMigrador = "migrador-pruebas";
    private const string ClaveApp = "app-pruebas";

    private readonly PostgreSqlContainer contenedor = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase(BaseDatos)
        .WithUsername("conecsia_admin")
        .WithPassword("admin-pruebas")
        .WithEnvironment("CONECSIA_MIGRATOR_PASSWORD", ClaveMigrador)
        .WithEnvironment("CONECSIA_APP_PASSWORD", ClaveApp)
        .WithResourceMapping(
            new FileInfo(Path.Combine(AppContext.BaseDirectory, "init", "01-roles.sh")),
            "/docker-entrypoint-initdb.d/")
        .Build();

    public string CadenaApp => Cadena("conecsia_app", ClaveApp);

    public string CadenaMigrador => Cadena("conecsia_migrator", ClaveMigrador);

    public Task InitializeAsync() => contenedor.StartAsync();

    public Task DisposeAsync() => contenedor.DisposeAsync().AsTask();

    private string Cadena(string usuario, string clave) =>
        new NpgsqlConnectionStringBuilder(contenedor.GetConnectionString())
        {
            Username = usuario,
            Password = clave,
            Database = BaseDatos,
            Pooling = false,
        }.ConnectionString;
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionBaseDatos : ICollectionFixture<BaseDatosFixture>
{
    public const string Nombre = "PostgreSQL";
}
