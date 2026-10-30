using System.Net;
using System.Net.Http.Json;
using ConecsIA.Tests.Infraestructura;

namespace ConecsIA.Tests;

/// <summary>
/// Formulario público de contacto: anónimo, validado en servidor, campo trampa y límite de 5 envíos por IP y hora.
/// Todos los envíos de esta clase comparten el límite, por eso viven en una sola prueba ordenada.
/// </summary>
public sealed class ContactoTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private const string Ruta = "/api/publico/contactos";

    private static object Solicitud(string correo, string tipo = "Coworking", string? sitioWeb = null) => new
    {
        organizacion = "  Organización demo  ",
        nombre = "Persona demo",
        correo,
        telefono = "",
        tipoOrganizacion = tipo,
        numeroSedes = 2,
        mensaje = "Necesitamos controlar visitas en dos sedes.",
        sitioWeb,
    };

    [Fact]
    public async Task Registra_valida_descarta_trampa_y_limita()
    {
        var cliente = api.CreateClient();
        var sufijo = Guid.NewGuid().ToString("N")[..8];

        // 1. Anónimo y válido ⇒ 202, guardado normalizado y sin organización.
        var correo = $"Contacto.{sufijo}@Ejemplo.test";
        var r = await cliente.PostAsJsonAsync(Ruta, Solicitud(correo));
        Assert.Equal(HttpStatusCode.Accepted, r.StatusCode);
        Assert.Equal("", await r.Content.ReadAsStringAsync());
        Assert.Equal("Organización demo", await api.ScalarAsync<string>(
            "SELECT organizacion FROM solicitud_contacto WHERE correo = @p0", correo.ToLowerInvariant()));
        Assert.Null(await api.ScalarAsync<string>(
            "SELECT telefono FROM solicitud_contacto WHERE correo = @p0", correo.ToLowerInvariant()));

        // 2. Campos inválidos ⇒ 400 con errores por campo.
        r = await cliente.PostAsJsonAsync(Ruta, new { organizacion = "", nombre = "x", correo = "no-es-correo", tipoOrganizacion = "Coworking", mensaje = "corto", numeroSedes = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var cuerpo = await r.Content.ReadAsStringAsync();
        foreach (var campo in new[] { "Organizacion", "Correo", "Mensaje", "NumeroSedes" })
            Assert.Contains(campo, cuerpo, StringComparison.OrdinalIgnoreCase);

        // 3. Tipo fuera del catálogo ⇒ 400.
        r = await cliente.PostAsJsonAsync(Ruta, Solicitud($"tipo.{sufijo}@ejemplo.test", tipo: "Banco"));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);

        // 4. Campo trampa ⇒ 202 pero no se guarda.
        var trampa = $"bot.{sufijo}@ejemplo.test";
        r = await cliente.PostAsJsonAsync(Ruta, Solicitud(trampa, sitioWeb: "http://spam.test"));
        Assert.Equal(HttpStatusCode.Accepted, r.StatusCode);
        Assert.Equal(0, await api.ScalarAsync<int>("SELECT count(*)::int FROM solicitud_contacto WHERE correo = @p0", trampa));

        // 5. Quinto envío permitido, sexto ⇒ 429.
        r = await cliente.PostAsJsonAsync(Ruta, Solicitud($"quinto.{sufijo}@ejemplo.test"));
        Assert.Equal(HttpStatusCode.Accepted, r.StatusCode);
        r = await cliente.PostAsJsonAsync(Ruta, Solicitud($"sexto.{sufijo}@ejemplo.test"));
        Assert.Equal((HttpStatusCode)429, r.StatusCode);
    }
}
