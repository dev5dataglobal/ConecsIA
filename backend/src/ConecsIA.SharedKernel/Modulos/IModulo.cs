using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.SharedKernel.Modulos;

/// <summary>
/// Punto de entrada de un módulo (ADR-001). La API compone los módulos a través de esta interfaz
/// sin conocer sus detalles internos.
/// </summary>
public interface IModulo
{
    string Nombre { get; }

    void RegistrarServicios(IServiceCollection servicios, IConfiguration configuracion);
}
