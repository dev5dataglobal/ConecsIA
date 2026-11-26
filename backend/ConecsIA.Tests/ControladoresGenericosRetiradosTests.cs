using System.Net;
using System.Net.Http.Json;
using ConecsIA.Tests.Infraestructura;

namespace ConecsIA.Tests;

/// <summary>
/// Los controladores CRUD genéricos de scaffold (`api/[controller]`) se retiraron el 2026-11-10: aceptaban la entidad
/// completa del cliente, devolvían entidades internas (p. ej. <c>Usuario.PasswordHash</c>) y permitían <c>DELETE</c>
/// físico, incluso sobre <c>autorizacion</c> (saltándose ADR-029) y <c>auditoria</c> (append-only). Aunque el usuario
/// tenga todos los módulos, esas rutas ya no existen; los casos de uso reales siguen respondiendo.
/// </summary>
public sealed class ControladoresGenericosRetiradosTests(ApiFactory api) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly string[] Retirados =
    [
        "Alerta", "AlertaTransicion", "Archivo", "AreaResponsable", "Auditoria", "Autorizacion", "AutorizacionHorario",
        "AutorizacionTransicion", "Ciudad", "InvitacionEnvio", "MembresiaOrganizacion", "MembresiaPuntoAcceso",
        "MembresiaRol", "MembresiaSede", "Modulo", "MovimientoDispositivo", "MovimientoVehiculo", "Notificacion",
        "NotificacionDestinatario", "Organizacion", "OrganizacionModulo", "Outbox", "ParametroOrganizacion", "Permiso",
        "Plan", "PlanModulo", "PlantillaNotificacion", "PlantillaRol", "PlantillaRolPermiso", "RestriccionAcceso", "Rol",
        "RolPermiso", "TipoOrganizacion", "TokenQr", "Vehiculo", "Usuario",
    ];

    private HttpClient _cliente = null!;

    public async Task InitializeAsync()
    {
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var org = await api.CrearOrganizacionAsync($"Org scaffold {sufijo}");
        var rol = await api.CrearRolAsync(org, "Todo");
        foreach (var m in new[] { "dashboard", "control_accesos", "autorizaciones", "personas", "vehiculos", "dispositivos", "alertas", "sedes", "auditoria", "configuracion", "usuarios", "perfiles", "organizaciones" })
            await api.AsignarModuloAsync(org, rol, m);
        var correo = $"todo.{sufijo}@scaffold.test";
        await api.CrearUsuarioAsync(correo, org, rol);
        _cliente = await api.ClienteAutenticadoAsync(correo);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Las_rutas_genericas_ya_no_existen_aunque_el_usuario_tenga_todos_los_modulos()
    {
        var fallos = new List<string>();
        foreach (var nombre in Retirados)
        {
            var listar = await _cliente.GetAsync($"/api/{nombre}");
            var leer = await _cliente.GetAsync($"/api/{nombre}/1");
            var borrar = await _cliente.DeleteAsync($"/api/{nombre}/1");
            var crear = await _cliente.PostAsync($"/api/{nombre}", JsonContent.Create(new { }));
            foreach (var (verbo, r) in new[] { ("GET", listar), ("GET id", leer), ("DELETE", borrar), ("POST", crear) })
                if (r.StatusCode is not (HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed))
                    fallos.Add($"{verbo} /api/{nombre} => {(int)r.StatusCode}");
        }
        Assert.Empty(fallos);
    }

    [Fact]
    public async Task Los_casos_de_uso_reales_siguen_disponibles()
    {
        Assert.Equal(HttpStatusCode.OK, (await _cliente.GetAsync("/api/Usuario/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.GetAsync("/api/sedes/ciudades")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.GetAsync("/api/accesos")).StatusCode);
    }
}
