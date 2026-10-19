using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Infrastructure.Seguridad;
using ConecsIA.IntegrationTests.Infraestructura;
using ConecsIA.Modules.Identidad.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.IntegrationTests;

/// <summary>
/// Contrato que la autenticación real (HU-006) debe conservar al sustituir el esquema provisional
/// (ADR-022): la sesión solo es útil con un usuario activo y una membresía activa, y retirar el
/// acceso surte efecto en la siguiente petición (PRD §5.2.8).
/// </summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class ContratoAutenticacionTests(BaseDatosFixture baseDatos) : IAsyncLifetime
{
    private const string Ruta = "/api/v1/me";
    private FabricaApi fabrica = null!;
    private Escenario e = null!;

    public async Task InitializeAsync()
    {
        fabrica = new FabricaApi(baseDatos);
        e = await Escenario.CrearAsync(fabrica);
    }

    public async Task DisposeAsync() => await fabrica.DisposeAsync();

    [Fact]
    public async Task Usuario_activo_sin_membresia_responde_403_sin_membresia()
    {
        var usuario = Usuario.Crear($"sinmembresia.{Guid.NewGuid():N}@ejemplo.test", "Sin membresía");
        await ModificarAsync(c => { c.Add(usuario); return Task.CompletedTask; });

        var respuesta = await fabrica.Como(usuario.Id).GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("SIN_MEMBRESIA", await CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Inactivar_la_membresia_rechaza_la_siguiente_peticion()
    {
        var cliente = fabrica.Como(e.OperadorA);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(Ruta)).StatusCode);

        await ModificarAsync(async c =>
            (await c.Set<Membresia>().SingleAsync(m => m.UsuarioId == e.OperadorA)).Inactivar());

        var respuesta = await cliente.GetAsync(Ruta);
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("SIN_MEMBRESIA", await CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Inactivar_el_usuario_rechaza_la_siguiente_peticion_con_401()
    {
        var cliente = fabrica.Como(e.AdminA);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(Ruta)).StatusCode);

        await ModificarAsync(async c => (await c.Set<Usuario>().SingleAsync(u => u.Id == e.AdminA)).Inactivar());

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync(Ruta)).StatusCode);
    }

    private async Task ModificarAsync(Func<ConecsIADbContext, Task> cambio)
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        await cambio(contexto);
        await contexto.SaveChangesAsync();
    }

    private static async Task<string?> CodigoAsync(HttpResponseMessage respuesta)
    {
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return cuerpo.TryGetProperty("code", out var codigo) ? codigo.GetString() : null;
    }
}
