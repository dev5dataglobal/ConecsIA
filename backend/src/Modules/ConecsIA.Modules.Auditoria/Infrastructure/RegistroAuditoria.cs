using System.Diagnostics;
using System.Text.Json;
using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Auditoria.Domain;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tiempo;

namespace ConecsIA.Modules.Auditoria.Infrastructure;

/// <summary>
/// Añade el evento al contexto compartido sin guardar: queda en la misma transacción que el cambio
/// (ADR-014). Los valores se serializan como JSON; los casos de uso solo pasan datos no sensibles.
/// </summary>
internal sealed class RegistroAuditoria(
    ConecsIADbContext contexto,
    IContextoSolicitud solicitud,
    IClock reloj) : IRegistroAuditoria
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Registrar(EntradaAuditoria entrada)
    {
        contexto.Add(EventoAuditoria.Crear(
            solicitud.OrganizacionId,
            solicitud.UsuarioId,
            reloj.UtcNow,
            entrada.Accion,
            entrada.TipoObjeto,
            entrada.ObjetoId,
            entrada.Resultado,
            Serializar(entrada.ValoresAnteriores),
            Serializar(entrada.ValoresNuevos),
            Activity.Current?.TraceId.ToString()));
    }

    private static string? Serializar(object? valores) =>
        valores is null ? null : JsonSerializer.Serialize(valores, valores.GetType(), Json);
}
