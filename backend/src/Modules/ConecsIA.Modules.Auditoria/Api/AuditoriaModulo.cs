using ConecsIA.Modules.Auditoria.Infrastructure;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Modulos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.Modules.Auditoria.Api;

/// <summary>Auditoría append-only (ADR-014, HU-008). La consulta para auditores llega con HU-046.</summary>
public sealed class AuditoriaModulo : IModulo
{
    public string Nombre => "Auditoria";

    public void RegistrarServicios(IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddScoped<IRegistroAuditoria, RegistroAuditoria>();
    }
}
