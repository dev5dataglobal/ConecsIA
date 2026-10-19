using ConecsIA.Api.Errores;
using ConecsIA.SharedKernel.Errores;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ConecsIA.UnitTests.Errores;

/// <summary>Mapeo de excepciones a códigos HTTP según ADR-020.</summary>
public class ManejadorExcepcionesTests
{
    [Fact]
    public void Recurso_no_encontrado_responde_404()
    {
        var problema = ManejadorExcepciones.Clasificar(new RecursoNoEncontradoException("la sede"));

        Assert.Equal(StatusCodes.Status404NotFound, problema.Status);
    }

    [Fact]
    public void Regla_de_negocio_responde_422_con_codigo_estable()
    {
        var problema = ManejadorExcepciones.Clasificar(new ReglaNegocioException("SEDE_INACTIVA", "La sede está inactiva."));

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, problema.Status);
        Assert.Equal("SEDE_INACTIVA", problema.Extensions[DetallesProblema.ClaveCodigo]);
        Assert.Equal("La sede está inactiva.", problema.Detail);
    }

    [Fact]
    public void Conflicto_de_concurrencia_responde_409()
    {
        var problema = ManejadorExcepciones.Clasificar(new DbUpdateConcurrencyException("cambió"));

        Assert.Equal(StatusCodes.Status409Conflict, problema.Status);
        Assert.Equal(ManejadorExcepciones.CodigoConflictoConcurrencia, problema.Extensions[DetallesProblema.ClaveCodigo]);
    }

    [Fact]
    public void Violacion_de_unicidad_de_postgres_responde_409_sin_exponer_sql()
    {
        var postgres = new PostgresException(
            messageText: "duplicate key value violates unique constraint \"ux_persona_documento\"",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: "23505");

        var problema = ManejadorExcepciones.Clasificar(new DbUpdateException("error", postgres));

        Assert.Equal(StatusCodes.Status409Conflict, problema.Status);
        Assert.Equal(ManejadorExcepciones.CodigoRegistroDuplicado, problema.Extensions[DetallesProblema.ClaveCodigo]);
        Assert.DoesNotContain("ux_persona_documento", problema.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Otra_excepcion_de_base_de_datos_responde_500()
    {
        var postgres = new PostgresException("otro", "ERROR", "ERROR", "42P01");

        var problema = ManejadorExcepciones.Clasificar(new DbUpdateException("error", postgres));

        Assert.Equal(StatusCodes.Status500InternalServerError, problema.Status);
    }

    [Fact]
    public void Excepcion_no_controlada_responde_500_sin_detalle()
    {
        var problema = ManejadorExcepciones.Clasificar(new InvalidOperationException("SELECT * FROM persona WHERE documento='123'"));

        Assert.Equal(StatusCodes.Status500InternalServerError, problema.Status);
        Assert.Null(problema.Detail);
    }
}
