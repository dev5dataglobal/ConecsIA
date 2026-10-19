using ConecsIA.SharedKernel.Modulos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.Modules.Accesos.Api;

/// <summary>Entradas, salidas, permanencias y presencia (EP-10, EP-11). Sin funcionalidad todavía: solo fija la frontera del módulo (ADR-001).</summary>
public sealed class AccesosModulo : IModulo
{
    public string Nombre => "Accesos";

    public void RegistrarServicios(IServiceCollection servicios, IConfiguration configuracion)
    {
    }
}
