using ConecsIA.Modules.Organizaciones.Application;
using ConecsIA.Modules.Organizaciones.Infrastructure;
using ConecsIA.SharedKernel.Modulos;
using ConecsIA.SharedKernel.Organizaciones;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.Modules.Organizaciones.Api;

/// <summary>Plano SaaS: organizaciones, tipos, planes y módulos habilitados (EP-03). Implementado: alta, edición e inactivación (HU-009).</summary>
public sealed class OrganizacionesModulo : IModulo
{
    public string Nombre => "Organizaciones";

    public void RegistrarServicios(IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddScoped<IConsultaOrganizaciones, ConsultaOrganizaciones>();
        servicios.AddScoped<IRepositorioOrganizaciones, RepositorioOrganizaciones>();
        servicios.AddScoped<ServicioOrganizacionesSaas>();
        servicios.AddScoped<ServicioMiOrganizacion>();
    }
}
