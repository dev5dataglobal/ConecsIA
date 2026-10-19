using System.Diagnostics;
using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Auditoria.Domain;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tiempo;

namespace ConecsIA.Modules.Auditoria.Infrastructure;

/// <summary>
/// Añade el evento al contexto compartido sin guardar: queda en la misma transacción que el cambio
/// (ADR-014). Los valores se serializan como JSON minimizado (MinimizadorValoresAuditoria).
/// </summary>
internal sealed class RegistroAuditoria(
    ConecsIADbContext contexto,
    IContextoSolicitud solicitud,
    IClock reloj) : IRegistroAuditoria
{
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
            MinimizadorValoresAuditoria.Serializar(entrada.ValoresAnteriores),
            MinimizadorValoresAuditoria.Serializar(entrada.ValoresNuevos),
            Activity.Current?.TraceId.ToString()));
    }
}
