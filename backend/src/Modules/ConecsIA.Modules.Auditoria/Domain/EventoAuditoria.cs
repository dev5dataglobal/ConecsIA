using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Tenancy;

namespace ConecsIA.Modules.Auditoria.Domain;

/// <summary>
/// Registro inalterable (ADR-014): el rol de aplicación solo puede insertar. Guarda organización,
/// actor, instante, acción, objeto, resultado y valores anterior/nuevo (RF-25).
/// </summary>
[SinFiltroDeOrganizacion("Append-only; incluye intentos sobre organizaciones ajenas y eventos sin organización. La consulta para auditores (HU-046) filtrará explícitamente.")]
public sealed class EventoAuditoria
{
    private EventoAuditoria()
    {
        Accion = string.Empty;
        TipoObjeto = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid? OrganizacionId { get; private set; }
    public Guid? ActorUsuarioId { get; private set; }
    public DateTimeOffset Instante { get; private set; }
    public string Accion { get; private set; }
    public string TipoObjeto { get; private set; }
    public Guid? ObjetoId { get; private set; }
    public ResultadoAuditoria Resultado { get; private set; }
    public string? ValoresAnteriores { get; private set; }
    public string? ValoresNuevos { get; private set; }
    public string? TraceId { get; private set; }

    public static EventoAuditoria Crear(
        Guid? organizacionId,
        Guid? actorUsuarioId,
        DateTimeOffset instante,
        string accion,
        string tipoObjeto,
        Guid? objetoId,
        ResultadoAuditoria resultado,
        string? valoresAnteriores,
        string? valoresNuevos,
        string? traceId) => new()
        {
            Id = Guid.CreateVersion7(instante),
            OrganizacionId = organizacionId,
            ActorUsuarioId = actorUsuarioId,
            Instante = instante,
            Accion = accion,
            TipoObjeto = tipoObjeto,
            ObjetoId = objetoId,
            Resultado = resultado,
            ValoresAnteriores = valoresAnteriores,
            ValoresNuevos = valoresNuevos,
            TraceId = traceId,
        };
}
