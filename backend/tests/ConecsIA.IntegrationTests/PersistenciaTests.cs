using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.IntegrationTests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ConecsIA.IntegrationTests;

/// <summary>ADR-003: migraciones versionadas, script idempotente y roles separados.</summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class PersistenciaTests(BaseDatosFixture baseDatos) : IAsyncLifetime
{
    private FabricaApi fabrica = null!;

    public Task InitializeAsync()
    {
        fabrica = new FabricaApi(baseDatos);
        _ = fabrica.Server; // arranca el host: aplica migraciones con el rol de migración
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await fabrica.DisposeAsync();

    [Fact]
    public async Task Las_migraciones_quedan_aplicadas_y_registradas_en_snake_case()
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();

        var aplicadas = (await contexto.Database.GetAppliedMigrationsAsync()).ToList();
        var pendientes = (await contexto.Database.GetPendingMigrationsAsync()).ToList();

        Assert.NotEmpty(aplicadas);
        Assert.Empty(pendientes);
        Assert.True(await ExisteTablaAsync(OpcionesPersistencia.TablaHistorialMigraciones));
    }

    [Fact]
    public async Task El_script_idempotente_se_puede_aplicar_dos_veces()
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        var script = contexto.GetService<IMigrator>()
            .GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);

        await using var conexion = new NpgsqlConnection(baseDatos.CadenaMigrador);
        await conexion.OpenAsync();
        for (var i = 0; i < 2; i++)
        {
            await using var comando = new NpgsqlCommand(script, conexion);
            await comando.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task El_rol_de_aplicacion_no_puede_ejecutar_DDL()
    {
        await using var conexion = new NpgsqlConnection(baseDatos.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand("CREATE TABLE intruso (id int)", conexion);

        var error = await Assert.ThrowsAsync<PostgresException>(() => comando.ExecuteNonQueryAsync());

        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }

    [Fact]
    public async Task El_rol_de_aplicacion_recibe_DML_sobre_tablas_creadas_por_migracion()
    {
        await using (var migrador = new NpgsqlConnection(baseDatos.CadenaMigrador))
        {
            await migrador.OpenAsync();
            await using var crear = new NpgsqlCommand(
                "CREATE TABLE IF NOT EXISTS prueba_privilegios (id int primary key)", migrador);
            await crear.ExecuteNonQueryAsync();
        }

        await using var app = new NpgsqlConnection(baseDatos.CadenaApp);
        await app.OpenAsync();
        await using var insertar = new NpgsqlCommand(
            "INSERT INTO prueba_privilegios (id) VALUES (1) ON CONFLICT DO NOTHING", app);
        await insertar.ExecuteNonQueryAsync();

        await using var borrarTabla = new NpgsqlCommand("DROP TABLE prueba_privilegios", app);
        var error = await Assert.ThrowsAsync<PostgresException>(() => borrarTabla.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }

    private async Task<bool> ExisteTablaAsync(string tabla)
    {
        await using var conexion = new NpgsqlConnection(baseDatos.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = @t)", conexion);
        comando.Parameters.AddWithValue("t", tabla);
        return (bool)(await comando.ExecuteScalarAsync())!;
    }
}
