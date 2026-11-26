using ConecsIA.Infrastructure.Data;
using ConecsIA.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace ConecsIA.Tests;

/// <summary>
/// Migraciones RegistroAccesoUnificado y RestriccionesRegistroAccesoUnificado (ADR-029) sobre datos del esquema anterior:
/// toda visita queda vinculada a un registro de acceso sin perder historial (política, estado, consumo y movimientos),
/// las autorizaciones existentes reciben categoría, la política «sin autorización» se traduce en flujo Automatico y queda
/// bitácora y auditoría. Usa su propia base temporal; los datos se siembran sin verificar llaves foráneas.
/// </summary>
public sealed class MigracionAccesosTests : IAsyncLifetime
{
    private const string MigracionAnterior = "20261031145759_ZonasEmpresarialesPorSedeYRetiroZonasAcceso";
    private readonly string _baseDatos = $"conecsia_test_mig_{Guid.NewGuid():N}";
    private readonly string _conexion;

    public MigracionAccesosTests()
    {
        var baseConexion = Environment.GetEnvironmentVariable("CONECSIA_TEST_CONNECTION") ?? ApiFactory.LeerConexionLocal();
        _conexion = new NpgsqlConnectionStringBuilder(baseConexion) { Database = _baseDatos, Pooling = false }.ConnectionString;
    }

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
    public async Task Toda_visita_queda_vinculada_a_un_registro_de_acceso_sin_perder_historial()
    {
        // Visitas: 20 = ingresó y salió por política (sin autorización), 21 = pendiente sin autorización, 22 = con autorización.
        await Scalar<int?>("""
            SET session_replication_role = replica;
            INSERT INTO tipo_organizacion (id_tipo_organizacion, nombre, activo) VALUES (1, 'Empresa', true) ON CONFLICT DO NOTHING;
            INSERT INTO organizacion (id_organizacion, id_tipo_organizacion, id_plan, nombre, estado, creado_en) VALUES (1, 1, 1, 'Org', 'Activa', now());
            INSERT INTO usuario (id_usuario, correo, correo_normalizado, password_hash, sello_seguridad, intentos_fallidos, activo, creado_en)
                VALUES (1, 'a@x.test', 'A@X.TEST', 'x', 'x', 0, true, now());
            INSERT INTO membresia_organizacion (id_membresia, id_organizacion, id_usuario, estado, creado_en) VALUES (4, 1, 1, 'Activa', now());
            INSERT INTO sede (id_sede, id_organizacion, id_ciudad, nombre, zona_horaria, activo) VALUES (11, 1, 1, 'Sede', 'UTC', true);
            INSERT INTO area (id_area, id_organizacion, id_sede, nombre, activo) VALUES (3, 1, 11, 'Gerencia', true);
            INSERT INTO persona (id_persona, id_organizacion, id_tipo_persona, id_tipo_documento, numero_documento, numero_documento_normalizado, nombres, apellidos, activo, creado_en, id_usuario_crea)
                VALUES (5, 1, 1, 1, '123', '123', 'Ana', 'Gómez', true, now(), 1);
            INSERT INTO punto_acceso (id_punto_acceso, id_organizacion, id_sede, nombre, modo_acceso, activo, creado_en) VALUES (8, 1, 11, 'Portería', 'Ambos', true, now());
            INSERT INTO parametro_organizacion (id_organizacion, clave, valor, actualizado_en) VALUES (1, 'visitas.permite_sin_autorizacion', 'true', now());
            INSERT INTO autorizacion (id_autorizacion, id_organizacion, id_persona, id_sede, id_area, id_membresia_responsable, id_usuario_crea, tipo_emisor, alcance, modalidad, fecha_inicio, ingresos_consumidos, estado, version, creado_en)
                VALUES (9, 1, 5, 11, 3, 4, 1, 'Empresa', 'Privado', 'Recurrente', current_date, 0, 'Aprobada', 1, now());
            INSERT INTO visita (id_visita, id_organizacion, id_persona, id_sede, id_area, id_membresia_responsable, id_usuario_registra, politica_aplicada, fecha_esperada, estado, version, creado_en)
                VALUES (20, 1, 5, 11, 3, 4, 1, 'visitas.permite_sin_autorizacion', current_date, 'Finalizada', 3, now()),
                       (21, 1, 5, 11, 3, 4, 1, NULL, NULL, 'Pendiente', 1, now());
            INSERT INTO visita (id_visita, id_organizacion, id_persona, id_sede, id_area, id_membresia_responsable, id_usuario_registra, id_autorizacion, estado, version, creado_en)
                VALUES (22, 1, 5, 11, 3, 4, 1, 9, 'Autorizada', 1, now());
            INSERT INTO movimiento_acceso (id_movimiento, id_organizacion, id_persona, id_sede, id_punto_acceso, id_visita, id_usuario_registra, tipo_movimiento, resultado, medio, ocurrido_en)
                VALUES (30, 1, 5, 11, 8, 20, 1, 'Entrada', 'Confirmado', 'Manual', now()), (31, 1, 5, 11, 8, 20, 1, 'Salida', 'Confirmado', 'Manual', now());
            SET session_replication_role = origin;
            -- Los ids se fijaron a mano: las secuencias avanzan como lo harían con datos creados por la aplicación.
            SELECT setval(pg_get_serial_sequence('public.tipo_organizacion', 'id_tipo_organizacion'), 100);
            SELECT setval(pg_get_serial_sequence('public.autorizacion', 'id_autorizacion'), 100);
            SELECT 1;
            """);
        var visitasAntes = await Scalar<long>("SELECT count(*) FROM visita");
        var movimientosAntes = await Scalar<long>("SELECT count(*) FROM movimiento_acceso");

        await using (var db = Contexto()) await db.Database.MigrateAsync();

        // Sin pérdida: mismas visitas y movimientos, y ninguna visita sin registro de acceso.
        Assert.Equal(visitasAntes, await Scalar<long>("SELECT count(*) FROM visita"));
        Assert.Equal(movimientosAntes, await Scalar<long>("SELECT count(*) FROM movimiento_acceso"));
        Assert.Equal(0, await Scalar<long>("SELECT count(*) FROM visita WHERE id_autorizacion IS NULL"));

        // Visita por política: registro Único aprobado sin decisor humano, consumido, con sus movimientos vinculados.
        var deVisita20 = await Scalar<int>("SELECT id_autorizacion FROM visita WHERE id_visita = 20");
        Assert.Equal("Aprobada|Unica|Migracion|1|", await Scalar<string>($"SELECT estado || '|' || modalidad || '|' || origen || '|' || ingresos_consumidos || '|' || coalesce(id_usuario_decide::text, '') FROM autorizacion WHERE id_autorizacion = {deVisita20}"));
        Assert.Equal(2, await Scalar<long>($"SELECT count(*) FROM movimiento_acceso WHERE id_autorizacion = {deVisita20}"));
        Assert.Equal("Pendiente", await Scalar<string>("SELECT a.estado FROM autorizacion a JOIN visita v ON v.id_autorizacion = a.id_autorizacion WHERE v.id_visita = 21"));
        Assert.Equal(9, await Scalar<int>("SELECT id_autorizacion FROM visita WHERE id_visita = 22"));

        // Categorías: la existente por modalidad; la política «sin autorización» ⇒ VISITANTE con flujo Automatico.
        Assert.Equal("RECURRENTE", await Scalar<string>("SELECT c.codigo FROM autorizacion a JOIN categoria_acceso c ON c.id_categoria_acceso = a.id_categoria_acceso WHERE a.id_autorizacion = 9"));
        Assert.Equal("Automatico", await Scalar<string>("SELECT flujo_aprobacion FROM categoria_acceso WHERE id_organizacion = 1 AND codigo = 'VISITANTE'"));
        Assert.Equal(0, await Scalar<long>("SELECT count(*) FROM autorizacion WHERE id_categoria_acceso IS NULL"));

        // Trazabilidad de la migración.
        Assert.Equal(2, await Scalar<long>("SELECT count(*) FROM migracion_accesos_adr029 WHERE origen = 'visita' AND resultado = 'Migrado'"));
        Assert.Equal(2, await Scalar<long>("SELECT count(*) FROM auditoria WHERE accion = 'Migrar' AND entidad = 'Autorizacion' AND tipo_actor = 'Sistema'"));
        Assert.Equal(2, await Scalar<long>("SELECT count(*) FROM autorizacion_transicion WHERE observacion LIKE 'Migración ADR-029%'"));

        // Restricciones para filas nuevas y menú sin Visitas ni Autorizaciones.
        Assert.Equal(3, await Scalar<long>("SELECT count(*) FROM pg_constraint WHERE conname IN ('ck_autorizacion_categoria', 'ck_autorizacion_motivo_decision', 'ck_visita_autorizacion')"));
        Assert.Equal(0, await Scalar<long>("SELECT count(*) FROM app_modulos WHERE codigo IN ('visitas', 'autorizaciones') AND ruta IS NOT NULL"));
    }
}
