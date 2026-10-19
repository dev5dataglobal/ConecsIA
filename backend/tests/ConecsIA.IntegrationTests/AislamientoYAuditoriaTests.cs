using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Infrastructure.Seguridad;
using ConecsIA.IntegrationTests.Infraestructura;
using ConecsIA.Modules.Estructura.Domain;
using ConecsIA.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ConecsIA.IntegrationTests;

/// <summary>HU-005 (ADR-004) y HU-008 (ADR-014) sobre PostgreSQL real.</summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class AislamientoYAuditoriaTests(BaseDatosFixture baseDatos) : IAsyncLifetime
{
    private FabricaApi fabrica = null!;
    private Escenario e = null!;

    public async Task InitializeAsync()
    {
        fabrica = new FabricaApi(baseDatos);
        e = await Escenario.CrearAsync(fabrica);
    }

    public async Task DisposeAsync() => await fabrica.DisposeAsync();

    [Fact]
    public async Task El_filtro_global_solo_devuelve_filas_de_la_organizacion_del_contexto()
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>()
            .Establecer(e.AdminA, e.OrgA, [], alcanceTotalSedes: true, sedesAsignadas: []);
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();

        var organizaciones = await contexto.Set<Sede>().Select(s => s.OrganizacionId).Distinct().ToListAsync();

        Assert.Equal([e.OrgA], organizaciones);
    }

    [Fact]
    public async Task Sin_contexto_de_organizacion_el_filtro_no_devuelve_nada()
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();

        Assert.Empty(await contexto.Set<Sede>().ToListAsync());
    }

    [Fact]
    public async Task Guardar_una_entidad_de_otra_organizacion_se_rechaza_antes_de_llegar_a_la_base()
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>()
            .Establecer(e.AdminA, e.OrgA, [], alcanceTotalSedes: true, sedesAsignadas: []);
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        contexto.Add(Sede.Crear(e.OrgB, "Inyectada", null, null, null, "UTC", DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<EscrituraEntreOrganizacionesException>(() => contexto.SaveChangesAsync());
    }

    [Fact]
    public async Task PostgreSQL_rechaza_una_sede_de_una_organizacion_inexistente()
    {
        await using var conexion = new NpgsqlConnection(baseDatos.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            INSERT INTO sede (id, organizacion_id, nombre, nombre_normalizado, zona_horaria, estado, creada_en, actualizada_en)
            VALUES (gen_random_uuid(), gen_random_uuid(), 'Huérfana', 'huérfana', 'UTC', 'Activa', now(), now())
            """,
            conexion);

        var error = await Assert.ThrowsAsync<PostgresException>(() => comando.ExecuteNonQueryAsync());

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, error.SqlState);
    }

    [Fact]
    public async Task PostgreSQL_rechaza_nombres_de_sede_duplicados_en_la_misma_organizacion()
    {
        await using var conexion = new NpgsqlConnection(baseDatos.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            INSERT INTO sede (id, organizacion_id, nombre, nombre_normalizado, zona_horaria, estado, creada_en, actualizada_en)
            VALUES (gen_random_uuid(), @org, 'SEDE A1', 'sede a1', 'UTC', 'Activa', now(), now())
            """,
            conexion);
        comando.Parameters.AddWithValue("org", e.OrgA);

        var error = await Assert.ThrowsAsync<PostgresException>(() => comando.ExecuteNonQueryAsync());

        Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
    }

    [Theory]
    [InlineData("UPDATE auditoria_evento SET accion = 'alterada'")]
    [InlineData("DELETE FROM auditoria_evento")]
    [InlineData("TRUNCATE auditoria_evento")]
    public async Task La_auditoria_es_append_only_para_el_rol_de_aplicacion(string sql)
    {
        await using var conexion = new NpgsqlConnection(baseDatos.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);

        var error = await Assert.ThrowsAsync<PostgresException>(() => comando.ExecuteNonQueryAsync());

        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }

    [Fact]
    public void Toda_entidad_con_organizacion_tiene_filtro_global_o_una_exclusion_justificada()
    {
        using var ambito = fabrica.Services.CreateScope();
        var modelo = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>().Model;

        var sinFiltro = modelo.GetEntityTypes()
            .Where(t => t.FindProperty("OrganizacionId") is not null)
            .Where(t => t.ClrType.GetCustomAttributes(typeof(SinFiltroDeOrganizacionAttribute), false) is not [SinFiltroDeOrganizacionAttribute { Justificacion.Length: > 20 }])
            .Where(t => !typeof(IEntidadDeOrganizacion).IsAssignableFrom(t.ClrType) || t.GetDeclaredQueryFilters().Count == 0)
            .Select(t => t.ClrType.Name)
            .ToList();

        Assert.True(sinFiltro.Count == 0, $"Entidades con OrganizacionId sin filtro global: {string.Join(", ", sinFiltro)}");
    }
}
