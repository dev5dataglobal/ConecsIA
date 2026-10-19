using System.Text.Json;
using ConecsIA.Modules.Auditoria.Infrastructure;

namespace ConecsIA.UnitTests.Auditoria;

/// <summary>HU-008 (ADR-014, ADR-018): la auditoría nunca guarda secretos aunque un caso de uso los pase.</summary>
public class MinimizadorValoresAuditoriaTests
{
    [Fact]
    public void Sin_valores_no_serializa_nada()
    {
        Assert.Null(MinimizadorValoresAuditoria.Serializar(null));
    }

    [Fact]
    public void Conserva_los_valores_de_negocio_en_camelCase()
    {
        var json = MinimizadorValoresAuditoria.Serializar(new { Nombre = "Sede Norte", ZonaHoraria = "America/Bogota" })!;

        var valores = JsonDocument.Parse(json).RootElement;
        Assert.Equal("Sede Norte", valores.GetProperty("nombre").GetString());
        Assert.Equal("America/Bogota", valores.GetProperty("zonaHoraria").GetString());
    }

    [Theory]
    [InlineData("Contrasena")]
    [InlineData("HashContrasena")]
    [InlineData("Password")]
    [InlineData("TokenQr")]
    [InlineData("SelloSeguridad")]
    [InlineData("ApiKey")]
    [InlineData("Secreto")]
    public void Enmascara_las_propiedades_sensibles(string propiedad)
    {
        var valores = new Dictionary<string, object> { [propiedad] = "valor-secreto-123", ["Nombre"] = "Ana" };

        var json = MinimizadorValoresAuditoria.Serializar(valores)!;

        Assert.DoesNotContain("valor-secreto-123", json, StringComparison.Ordinal);
        Assert.Contains(MinimizadorValoresAuditoria.Mascara, json, StringComparison.Ordinal);
        Assert.Contains("Ana", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Enmascara_tambien_en_objetos_anidados_y_listas()
    {
        var json = MinimizadorValoresAuditoria.Serializar(new
        {
            Usuario = new { Correo = "a@b.test", Credencial = new { Token = "t-1" } },
            Sesiones = new[] { new { Token = "t-2" } },
        })!;

        Assert.DoesNotContain("t-1", json, StringComparison.Ordinal);
        Assert.DoesNotContain("t-2", json, StringComparison.Ordinal);
    }
}
