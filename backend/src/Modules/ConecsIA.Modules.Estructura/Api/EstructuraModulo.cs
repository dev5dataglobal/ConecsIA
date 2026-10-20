using ConecsIA.Modules.Estructura.Application;
using ConecsIA.Modules.Estructura.Infrastructure;
using ConecsIA.SharedKernel.Modulos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.Modules.Estructura.Api;

/// <summary>Sedes, áreas/destinos, zonas y puntos de acceso (EP-04). Implementado: sedes (HU-013).</summary>
public sealed class EstructuraModulo : IModulo
{
    public string Nombre => "Estructura";

    public void RegistrarServicios(IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddScoped<IRepositorioSedes, RepositorioSedes>();
        servicios.AddScoped<ServicioSedes>();
        servicios.AddScoped<ConecsIA.SharedKernel.Estructura.IConsultaSedes, ConsultaSedesExistentes>();
    }
}
