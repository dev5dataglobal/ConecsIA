using ConecsIA.Modules.Organizaciones.Infrastructure;
using ConecsIA.SharedKernel.Modulos;
using ConecsIA.SharedKernel.Organizaciones;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.Modules.Organizaciones.Api;

/// <summary>Plano SaaS: organizaciones, tipos, planes y módulos habilitados (EP-03). Por ahora solo la entidad y su consulta de estado.</summary>
public sealed class OrganizacionesModulo : IModulo
{
    public string Nombre => "Organizaciones";

    public void RegistrarServicios(IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddScoped<IConsultaOrganizaciones, ConsultaOrganizaciones>();
    }
}
