using ConecsIA.SharedKernel.Modulos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.Modules.Autorizaciones.Api;

/// <summary>Autorizaciones, decisiones, visitas y QR (EP-07 a EP-09). Sin funcionalidad todavía: solo fija la frontera del módulo (ADR-001).</summary>
public sealed class AutorizacionesModulo : IModulo
{
    public string Nombre => "Autorizaciones";

    public void RegistrarServicios(IServiceCollection servicios, IConfiguration configuracion)
    {
    }
}
