using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Infrastructure.Seguridad;
using ConecsIA.IntegrationTests.Infraestructura;
using ConecsIA.Api.Errores;
using ConecsIA.Modules.Auditoria.Domain;
using ConecsIA.Modules.Estructura.Domain;
using ConecsIA.SharedKernel.Auditoria;
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

    [Fact]
    public void Todo_indice_unico_de_una_entidad_de_organizacion_incluye_la_organizacion()
    {
        var modelo = Modelo();

        var indebidos = modelo.GetEntityTypes()
            .Where(t => typeof(IEntidadDeOrganizacion).IsAssignableFrom(t.ClrType))
            .SelectMany(t => t.GetIndexes().Where(i => i.IsUnique)
                .Where(i => i.Properties.All(p => p.Name != nameof(IEntidadDeOrganizacion.OrganizacionId)))
                .Where(i => !i.Properties.All(p => p.PropertyInfo?.GetCustomAttributes(typeof(UnicidadGlobalAttribute), false)
                    is [UnicidadGlobalAttribute { Justificacion.Length: > 20 }]))
                .Select(i => $"{t.ClrType.Name}({string.Join(", ", i.Properties.Select(p => p.Name))})"))
            .ToList();

        Assert.True(indebidos.Count == 0, $"Índices únicos sin organización: {string.Join("; ", indebidos)}");
    }

    [Fact]
    public void Toda_relacion_entre_entidades_de_organizacion_usa_una_clave_compuesta_con_la_organizacion()
    {
        var modelo = Modelo();

        var indebidas = modelo.GetEntityTypes()
            .Where(t => typeof(IEntidadDeOrganizacion).IsAssignableFrom(t.ClrType))
            .SelectMany(t => t.GetForeignKeys()
                .Where(fk => typeof(IEntidadDeOrganizacion).IsAssignableFrom(fk.PrincipalEntityType.ClrType))
                .Where(fk => fk.Properties.All(p => p.Name != nameof(IEntidadDeOrganizacion.OrganizacionId)))
                .Select(fk => $"{t.ClrType.Name} → {fk.PrincipalEntityType.ClrType.Name}"))
            .ToList();

        Assert.True(indebidas.Count == 0, $"Relaciones que podrían cruzar organizaciones: {string.Join("; ", indebidas)}");
    }

    [Fact]
    public async Task Una_escritura_entre_organizaciones_responde_404_y_queda_auditada_como_denegada()
    {
        var respuesta = await fabrica.Como(e.AdminA).PostAsync($"/api/v1/soporte-pruebas/escritura-cruzada/{e.OrgB}", null);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.DoesNotContain(e.OrgB.ToString(), await SuiteAislamientoTests.CuerpoSinInstanciaAsync(respuesta));

        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        var evento = await contexto.Set<EventoAuditoria>()
            .SingleAsync(a => a.ActorUsuarioId == e.AdminA && a.Accion == AuditoriaDenegaciones.AccionEscrituraEntreOrganizaciones);
        Assert.Equal(e.OrgA, evento.OrganizacionId);
        Assert.Equal(ResultadoAuditoria.Denegado, evento.Resultado);
        Assert.Equal("Sede", evento.TipoObjeto);
        Assert.Empty(await contexto.Set<Sede>().Where(s => s.Nombre == "Inyectada por soporte").ToListAsync());
    }

    [Fact]
    public async Task Si_la_auditoria_no_se_puede_escribir_la_operacion_completa_falla()
    {
        var respuesta = await fabrica.Como(e.AdminA).PostAsync("/api/v1/soporte-pruebas/auditoria-que-falla", null);

        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, respuesta.StatusCode);
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        Assert.False(await contexto.Set<Sede>().AnyAsync(s => s.OrganizacionId == e.OrgA && s.Nombre == "Sin auditoría"));
    }

    [Fact]
    public async Task Una_operacion_rechazada_no_deja_evento_de_exito()
    {
        var antes = await EventosExitososAsync(e.AdminA);

        var respuesta = await fabrica.Como(e.AdminA).PostAsync("/api/v1/sedes",
            System.Net.Http.Json.JsonContent.Create(new { nombre = "SEDE A1", zonaHoraria = "UTC" }));

        Assert.Equal(System.Net.HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(antes, await EventosExitososAsync(e.AdminA));
    }

    private async Task<int> EventosExitososAsync(Guid actor)
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        return await ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>().Set<EventoAuditoria>()
            .CountAsync(a => a.ActorUsuarioId == actor && a.Resultado == ResultadoAuditoria.Exitoso);
    }

    private Microsoft.EntityFrameworkCore.Metadata.IModel Modelo()
    {
        using var ambito = fabrica.Services.CreateScope();
        return ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>().Model;
    }
}
