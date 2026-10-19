using ConecsIA.Modules.Accesos.Api;
using ConecsIA.Modules.Auditoria.Api;
using ConecsIA.Modules.Autorizaciones.Api;
using ConecsIA.Modules.Estructura.Api;
using ConecsIA.Modules.Identidad.Api;
using ConecsIA.Modules.Organizaciones.Api;
using ConecsIA.Modules.Personas.Api;
using ConecsIA.SharedKernel.Modulos;

namespace ConecsIA.Api.Composicion;

/// <summary>
/// Composición explícita de módulos. Una prueba de arquitectura falla si un módulo compilado
/// no aparece aquí.
/// </summary>
public static class CatalogoModulos
{
    public static IReadOnlyList<IModulo> Todos { get; } =
    [
        new OrganizacionesModulo(),
        new IdentidadModulo(),
        new EstructuraModulo(),
        new PersonasModulo(),
        new AutorizacionesModulo(),
        new AccesosModulo(),
        new AuditoriaModulo(),
    ];
}
