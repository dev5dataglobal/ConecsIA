using ConecsIA.Infrastructure.Data;
using ConecsIA.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace ConecsIA.Tests;

/// <summary>
/// Migración ZonasEmpresarialesPorSedeYRetiroZonasAcceso (ADR-028) sobre datos del esquema anterior: traslada a la sede solo
/// las zonas empresariales inequívocas, deja pendientes las ambiguas y conserva una referencia legible de las zonas de acceso
/// retiradas. Usa su propia base temporal; los datos se siembran sin verificar llaves foráneas porque solo interesan las
/// columnas que la migración lee.
/// </summary>
public sealed class MigracionZonasTests : IAsyncLifetime
{
    private const string MigracionAnterior = "20261030132009_SolicitudesContacto";
    private readonly string _baseDatos = $"conecsia_test_mig_{Guid.NewGuid():N}";
    private readonly string _conexion;

    public MigracionZonasTests()
    {
        var baseConexion = Environment.GetEnvironmentVariable("CONECSIA_TEST_CONNECTION") ?? ApiFactory.LeerConexionLocal();
        _conexion = new NpgsqlConnectionStringBuilder(baseConexion) { Database = _baseDatos, Pooling = false }.ConnectionString;
    }

    // Migrar desde cero (incluye el catálogo DIVIPOLA) supera los 30 s por defecto cuando corre en paralelo con otras clases.
    private ConecsiaDbContext Contexto() =>
        new(new DbContextOptionsBuilder<ConecsiaDbContext>().UseNpgsql(_conexion, o => o.CommandTimeout(300)).Options);

    public async Task InitializeAsync()
    {
        await using var db = Contexto();
        await db.GetService<IMigrator>().MigrateAsync(MigracionAnterior);
    }

    public async Task DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        var admin = new NpgsqlConnectionStringBuilder(_conexion) { Database = "postgres" }.ConnectionString;
        await using var conexion = new NpgsqlConnection(admin);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_baseDatos}\" WITH (FORCE)", conexion) { CommandTimeout = 300 };
        await comando.ExecuteNonQueryAsync();
    }

    private async Task<T?> Scalar<T>(string sql)
    {
        await using var conexion = new NpgsqlConnection(_conexion);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        var valor = await comando.ExecuteScalarAsync();
        return valor is null or DBNull ? default : (T)Convert.ChangeType(valor, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
    }

    [Fact]
    public async Task Traslada_lo_inequivoco_deja_pendiente_lo_ambiguo_y_conserva_la_historia_de_zonas()
    {
        // Organizaciones: 1 = una sede (se traslada), 2 = varias sedes (pendiente), 3 = sin sedes (pendiente),
        // 4 = vínculo inactivo (histórico), 5 = organización superior con una sede (se traslada).
        await Scalar<int?>("""
            SET session_replication_role = replica;
            INSERT INTO zona_empresarial (id_zona_empresarial, nombre, activa, creada_en) VALUES (1, 'Zona Franca', true, now()), (2, 'Parque Norte', true, now());
            INSERT INTO sede (id_sede, id_organizacion, id_ciudad, nombre, zona_horaria, activo) VALUES
                (11, 1, 1, 'Única', 'UTC', true), (21, 2, 1, 'Norte', 'UTC', true), (22, 2, 1, 'Sur', 'UTC', true),
                (41, 4, 1, 'Cerrada', 'UTC', true), (51, 5, 1, 'Administración', 'UTC', true);
            INSERT INTO zona_empresarial_organizacion (id_organizacion, id_zona_empresarial, es_administradora, activa) VALUES
                (1, 1, false, true), (2, 1, false, true), (3, 2, false, true), (4, 2, false, false), (5, 1, true, true);

            INSERT INTO zona (id_zona, id_organizacion, id_sede, nombre, activo, creado_en) VALUES (7, 1, 11, 'Recepción', true, now());
            INSERT INTO punto_acceso (id_punto_acceso, id_organizacion, id_sede, id_zona, nombre, modo_acceso, activo, creado_en) VALUES (8, 1, 11, 7, 'Portería', 'Ambos', true, now());
            INSERT INTO autorizacion (id_autorizacion, id_organizacion, id_persona, id_sede, id_area, id_membresia_responsable, id_usuario_crea, tipo_emisor, alcance, modalidad, fecha_inicio, ingresos_consumidos, estado, version, creado_en)
                VALUES (9, 1, 1, 11, 1, 1, 1, 'Empresa', 'Privado', 'Diaria', current_date, 0, 'Aprobada', 1, now());
            INSERT INTO autorizacion_zona (id_organizacion, id_autorizacion, id_zona) VALUES (1, 9, 7);
            INSERT INTO movimiento_acceso (id_movimiento, id_organizacion, id_persona, id_sede, id_punto_acceso, id_zona, id_usuario_registra, tipo_movimiento, resultado, medio, ocurrido_en)
                VALUES (10, 1, 1, 11, 8, 7, 1, 'Entrada', 'Confirmado', 'Manual', now());
            INSERT INTO restriccion_acceso (id_restriccion, id_organizacion, id_persona, id_zona, tipo_restriccion, fecha_inicio, estado, id_usuario_registra, creado_en)
                VALUES (12, 1, 1, 7, 'Bloqueo', now(), 'Activa', 1, now());
            INSERT INTO parametro_organizacion (id_organizacion, clave, valor, actualizado_en) VALUES (1, 'autorizaciones.exige_zonas', 'true', now());
            SET session_replication_role = origin;
            SELECT 1;
            """);

        await using (var db = Contexto()) await db.Database.MigrateAsync();

        // Zona empresarial resuelta desde la sede solo donde no hay ambigüedad.
        Assert.Equal(1, await Scalar<int>("SELECT id_zona_empresarial FROM sede WHERE id_sede = 11"));
        Assert.Equal(1, await Scalar<int>("SELECT id_zona_empresarial FROM sede WHERE id_sede = 51"));
        Assert.Equal(0, await Scalar<int>("SELECT count(*)::int FROM sede WHERE id_sede IN (21, 22, 41) AND id_zona_empresarial IS NOT NULL"));
        Assert.Equal("Pendiente", await Scalar<string>("SELECT resultado FROM migracion_zonas_adr028 WHERE origen = 'zona_empresarial_organizacion' AND id_organizacion = 2"));
        Assert.Equal("Pendiente", await Scalar<string>("SELECT resultado FROM migracion_zonas_adr028 WHERE origen = 'zona_empresarial_organizacion' AND id_organizacion = 3"));
        Assert.Equal("Historico", await Scalar<string>("SELECT resultado FROM migracion_zonas_adr028 WHERE origen = 'zona_empresarial_organizacion' AND id_organizacion = 4"));
        Assert.Equal("Migrado", await Scalar<string>("SELECT resultado FROM migracion_zonas_adr028 WHERE origen = 'zona_empresarial_organizacion' AND id_organizacion = 5"));

        // Zonas de acceso retiradas: el punto queda en su sede, el movimiento conserva el nombre de la zona y la
        // restricción de persona pasa a la sede de la zona.
        Assert.Equal(1, await Scalar<int>("SELECT count(*)::int FROM punto_acceso WHERE id_punto_acceso = 8 AND id_sede = 11"));
        Assert.Equal("Recepción", await Scalar<string>("SELECT zona_historica FROM movimiento_acceso WHERE id_movimiento = 10"));
        Assert.Equal(11, await Scalar<int>("SELECT id_sede FROM restriccion_acceso WHERE id_restriccion = 12"));
        Assert.Equal("Retirado", await Scalar<string>("SELECT resultado FROM migracion_zonas_adr028 WHERE origen = 'autorizacion_zona' AND id_autorizacion = 9"));
        Assert.Equal(0, await Scalar<int>("SELECT count(*)::int FROM parametro_organizacion WHERE clave = 'autorizaciones.exige_zonas'"));
        Assert.Equal(0, await Scalar<int>("SELECT count(*)::int FROM information_schema.tables WHERE table_name IN ('zona', 'autorizacion_zona', 'zona_empresarial_organizacion')"));
        Assert.Equal(0, await Scalar<int>("SELECT count(*)::int FROM information_schema.columns WHERE column_name = 'id_zona' AND table_name IN ('punto_acceso', 'movimiento_acceso', 'restriccion_acceso')"));
    }
}
