using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConecsIA.ArchitectureTests;

/// <summary>
/// ADR-002 (controladores MVC) y ADR-020 (API REST versionada, Problem Details).
/// </summary>
public class ConvencionesApiTests
{
    private static IEnumerable<Type> Controladores() =>
        new[] { Ensamblados.Api }
            .Concat(Ensamblados.ModulosRegistrados)
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsClass: true } && typeof(ControllerBase).IsAssignableFrom(t));

    [Fact]
    public void Existe_al_menos_un_controlador()
    {
        Assert.NotEmpty(Controladores());
    }

    [Fact]
    public void Los_controladores_terminan_en_Controller_y_viven_en_una_capa_Api()
    {
        var indebidos = Controladores()
            .Where(t => !t.Name.EndsWith("Controller", StringComparison.Ordinal)
                        || t.Namespace is null
                        || !(t.Namespace.StartsWith("ConecsIA.Api", StringComparison.Ordinal)
                             || t.Namespace.Contains(".Api", StringComparison.Ordinal)))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(indebidos.Count == 0, $"Controladores fuera de convención: {string.Join(", ", indebidos)}");
    }

    [Fact]
    public void Los_controladores_usan_ApiController_y_rutas_bajo_api_v1()
    {
        var indebidos = Controladores()
            .Where(t => t.GetCustomAttribute<ApiControllerAttribute>(inherit: true) is null
                        || t.GetCustomAttribute<RouteAttribute>(inherit: true) is not { } ruta
                        || !ruta.Template.StartsWith("api/v1/", StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(indebidos.Count == 0, $"Controladores sin [ApiController] o ruta api/v1: {string.Join(", ", indebidos)}");
    }

    [Fact]
    public void Los_controladores_no_acceden_directamente_a_la_base_de_datos()
    {
        var indebidos = Controladores()
            .Where(t => t.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Any(p => p.ParameterType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true
                          || p.ParameterType.Name.EndsWith("DbContext", StringComparison.Ordinal)))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(indebidos.Count == 0, $"Controladores que reciben un DbContext: {string.Join(", ", indebidos)}");
    }

    /// <summary>
    /// HU-007 (DoD): todo endpoint exige autenticación por el filtro global; los anónimos son una lista
    /// cerrada y revisada. Los permisos se exigen en los casos de uso (<c>AccesoDenegadoException.ExigirPermiso</c>).
    /// </summary>
    [Fact]
    public void Solo_los_endpoints_revisados_admiten_acceso_anonimo()
    {
        string[] revisados =
        [
            "AutenticacionController.Cerrar",   // cerrar sesión funciona aunque la sesión ya no sea válida
            "AutenticacionController.Ingresar",
            "CsrfController.Obtener",
            "DesarrolloController.Usuarios",    // responde 404 fuera de Development/Testing
            "InvitacionesController.Aceptar",   // el token de un solo uso prueba la posesión del correo (HU-016)
            "SistemaController.ObtenerVersion",
        ];

        var anonimos = Controladores()
            .SelectMany(c => c.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => c.GetCustomAttribute<AllowAnonymousAttribute>() is not null
                            || m.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
                .Select(m => $"{c.Name}.{m.Name}"))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(revisados.Order(StringComparer.Ordinal), anonimos);
    }
}
