using ConecsIA.Api.Observabilidad;
using Microsoft.Extensions.Logging;

namespace ConecsIA.UnitTests.Observabilidad;

public class RedactorDatosSensiblesTests
{
    [Theory]
    [InlineData("password=Secreta123", "Secreta123")]
    [InlineData("{\"password\": \"Secreta 123\"}", "Secreta 123")]
    [InlineData("contraseña: hunter2", "hunter2")]
    [InlineData("qr_token=AbCdEf0123456789xyz", "AbCdEf0123456789xyz")]
    [InlineData("Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.abc.def", "eyJhbGciOiJIUzI1NiJ9")]
    [InlineData("api-key='k-123-zzz'", "k-123-zzz")]
    [InlineData("Persona con documento 1.023.456.789 ingresó", "1.023.456.789")]
    [InlineData("documento=CC-80123456", "CC-80123456")]
    [InlineData("Contacto: visitante@correo.com", "visitante@correo.com")]
    [InlineData("Teléfono +57 300 123 4567 registrado", "300 123 4567")]
    public void Enmascara_datos_sensibles(string entrada, string valorSensible)
    {
        var resultado = RedactorDatosSensibles.Redactar(entrada);

        Assert.DoesNotContain(valorSensible, resultado, StringComparison.Ordinal);
        Assert.Contains(RedactorDatosSensibles.Mascara, resultado, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Migraciones aplicadas: 3")]
    [InlineData("GET /api/v1/health respondió 200 en 12 ms")]
    [InlineData("Organización 7f3c2a10-1b2c-4d5e-8f90-a1b2c3d4e5f6 creada")]
    public void No_altera_texto_operativo_sin_datos_sensibles(string entrada)
    {
        Assert.Equal(entrada, RedactorDatosSensibles.Redactar(entrada));
    }

    [Fact]
    public void El_formateador_json_no_incluye_el_mensaje_de_la_excepcion_ni_datos_sensibles()
    {
        var formateador = new FormateadorJsonRedactado();
        var excepcion = new InvalidOperationException("Fallo con correo jefe@empresa.com y password=abc");

        var linea = formateador.Formatear(LogLevel.Error, "Prueba", "Usuario admin@empresa.com token=XYZ987654", excepcion);

        Assert.DoesNotContain("admin@empresa.com", linea, StringComparison.Ordinal);
        Assert.DoesNotContain("XYZ987654", linea, StringComparison.Ordinal);
        Assert.DoesNotContain("jefe@empresa.com", linea, StringComparison.Ordinal);
        Assert.DoesNotContain("password=abc", linea, StringComparison.Ordinal);
        Assert.Contains("\"exceptionType\":\"System.InvalidOperationException\"", linea, StringComparison.Ordinal);
    }

    [Fact]
    public void El_formateador_produce_json_valido_con_nivel_y_categoria()
    {
        var linea = new FormateadorJsonRedactado().Formatear(LogLevel.Information, "Categoria.X", "Hola");

        using var documento = System.Text.Json.JsonDocument.Parse(linea);
        Assert.Equal("Information", documento.RootElement.GetProperty("level").GetString());
        Assert.Equal("Categoria.X", documento.RootElement.GetProperty("category").GetString());
        Assert.Equal("Hola", documento.RootElement.GetProperty("message").GetString());
    }
}
