using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.SharedKernel.Auditoria;

namespace ConecsIA.Api.Errores;

/// <summary>
/// HU-005: un intento de escritura entre organizaciones es un defecto o un ataque; se audita como
/// <see cref="ResultadoAuditoria.Denegado"/> con la organización y el actor del contexto verificado.
/// Solo se registra el tipo de entidad: nada de la organización ajena.
/// </summary>
public static class AuditoriaEscrituraEntreOrganizaciones
{
    public const string Accion = "persistencia.escritura_entre_organizaciones";

    public static async Task RegistrarAsync(IServiceProvider servicios, EscrituraEntreOrganizacionesException excepcion, CancellationToken cancelacion)
    {
        var contexto = servicios.GetRequiredService<ConecsIADbContext>();
        // Se descartan los cambios rechazados para que el guardado solo contenga el evento.
        contexto.ChangeTracker.Clear();
        servicios.GetRequiredService<IRegistroAuditoria>()
            .Registrar(new EntradaAuditoria(Accion, excepcion.Entidad, ObjetoId: null, ResultadoAuditoria.Denegado));
        await contexto.SaveChangesAsync(cancelacion);
    }
}
