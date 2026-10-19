using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Infrastructure.Seguridad;
using ConecsIA.Modules.Estructura.Domain;
using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.Modules.Organizaciones.Domain;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.IntegrationTests.Infraestructura;

/// <summary>
/// Datos sintéticos con dos organizaciones (A y B) para pruebas positivas y negativas de aislamiento.
/// Cada prueba crea su propio escenario con nombres únicos: comparten contenedor, no datos.
/// </summary>
public sealed record Escenario(
    Guid OrgA,
    Guid OrgB,
    Guid AdminA,
    Guid OperadorA,
    Guid AdminB,
    Guid SinPermisosA,
    Guid UsuarioInactivo,
    Guid SedeA1,
    Guid SedeA2,
    Guid SedeB1)
{
    public static readonly string[] PermisosAdmin =
    [
        Permisos.SedesConsultar, Permisos.SedesCrear, Permisos.SedesModificar, Permisos.SedesInactivar,
    ];

    public static async Task<Escenario> CrearAsync(FabricaApi fabrica)
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        var ahora = DateTimeOffset.UtcNow;
        var sufijo = Guid.NewGuid().ToString("N")[..8];

        var orgA = Organizacion.Crear($"Org A {sufijo}", ahora);
        var orgB = Organizacion.Crear($"Org B {sufijo}", ahora);
        var sedeA1 = Sede.Crear(orgA.Id, "Sede A1", null, "Bogotá", "Colombia", "America/Bogota", ahora);
        var sedeA2 = Sede.Crear(orgA.Id, "Sede A2", null, "Medellín", "Colombia", "America/Bogota", ahora);
        var sedeB1 = Sede.Crear(orgB.Id, "Sede B1", null, "Lima", "Perú", "America/Lima", ahora);

        var adminA = Usuario.Crear($"admin.a.{sufijo}@ejemplo.test", "Admin A");
        var operadorA = Usuario.Crear($"operador.a.{sufijo}@ejemplo.test", "Operador A");
        var adminB = Usuario.Crear($"admin.b.{sufijo}@ejemplo.test", "Admin B");
        var sinPermisosA = Usuario.Crear($"sinpermisos.a.{sufijo}@ejemplo.test", "Administrador sin permisos");
        var inactivo = Usuario.Crear($"inactivo.{sufijo}@ejemplo.test", "Usuario inactivo");
        inactivo.Inactivar();

        contexto.AddRange(orgA, orgB);
        contexto.AddRange(sedeA1, sedeA2, sedeB1);
        contexto.AddRange(adminA, operadorA, adminB, sinPermisosA, inactivo);
        contexto.AddRange(
            Membresia.Crear(adminA.Id, orgA.Id, PermisosAdmin, alcanceTotalSedes: true, sedesAsignadas: []),
            Membresia.Crear(operadorA.Id, orgA.Id, [Permisos.SedesConsultar, Permisos.SedesModificar], alcanceTotalSedes: false, sedesAsignadas: [sedeA1.Id]),
            Membresia.Crear(adminB.Id, orgB.Id, PermisosAdmin, alcanceTotalSedes: true, sedesAsignadas: []),
            // El nombre del rol no otorga nada: solo cuentan las claves de permiso (ADR-005).
            Membresia.Crear(sinPermisosA.Id, orgA.Id, ["Administrador"], alcanceTotalSedes: true, sedesAsignadas: []),
            Membresia.Crear(inactivo.Id, orgA.Id, PermisosAdmin, alcanceTotalSedes: true, sedesAsignadas: []));
        await contexto.SaveChangesAsync();

        return new Escenario(orgA.Id, orgB.Id, adminA.Id, operadorA.Id, adminB.Id, sinPermisosA.Id, inactivo.Id, sedeA1.Id, sedeA2.Id, sedeB1.Id);
    }
}

public static class ClientesPrueba
{
    /// <summary>Cliente autenticado con el esquema de desarrollo (sustituto de HU-006 mientras D-03 está abierta).</summary>
    public static HttpClient Como(this FabricaApi fabrica, Guid usuario, Guid? organizacion = null)
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(CabecerasSeguridad.UsuarioDesarrollo, usuario.ToString());
        if (organizacion is not null)
        {
            cliente.DefaultRequestHeaders.Add(CabecerasSeguridad.Organizacion, organizacion.ToString());
        }

        return cliente;
    }
}
