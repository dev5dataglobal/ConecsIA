using ConecsIA.SharedKernel.Modulos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.Modules.Personas.Api;

/// <summary>Registro reutilizable de personas por organización (EP-06). Sin funcionalidad todavía: solo fija la frontera del módulo (ADR-001).</summary>
public sealed class PersonasModulo : IModulo
{
    public string Nombre => "Personas";

    public void RegistrarServicios(IServiceCollection servicios, IConfiguration configuracion)
    {
    }
}
