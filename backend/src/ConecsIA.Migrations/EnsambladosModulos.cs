using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Accesos.Api;
using ConecsIA.Modules.Auditoria.Api;
using ConecsIA.Modules.Autorizaciones.Api;
using ConecsIA.Modules.Estructura.Api;
using ConecsIA.Modules.Identidad.Api;
using ConecsIA.Modules.Organizaciones.Api;
using ConecsIA.Modules.Personas.Api;

namespace ConecsIA.Migrations;

/// <summary>Módulos que aportan entidades al modelo usado para generar migraciones.</summary>
internal static class EnsambladosModulos
{
    public static EnsambladosDelModelo Modelo { get; } = new(
    [
        typeof(OrganizacionesModulo).Assembly,
        typeof(IdentidadModulo).Assembly,
        typeof(EstructuraModulo).Assembly,
        typeof(PersonasModulo).Assembly,
        typeof(AutorizacionesModulo).Assembly,
        typeof(AccesosModulo).Assembly,
        typeof(AuditoriaModulo).Assembly,
    ]);
}
