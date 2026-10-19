using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Seguridad;

namespace ConecsIA.Api.Errores;

/// <summary>
/// Audita como <see cref="ResultadoAuditoria.Denegado"/> las denegaciones que llegan al manejador de
/// errores, con la organización y el actor del contexto verificado:
/// HU-005, escritura entre organizaciones (solo el tipo de entidad);
/// HU-007, permiso faltante o fuera de alcance (código y plantilla de ruta, sin identificadores).
/// </summary>
public static class AuditoriaDenegaciones
{
    public const string AccionEscrituraEntreOrganizaciones = "persistencia.escritura_entre_organizaciones";
    public const string AccionAccesoDenegado = "seguridad.acceso_denegado";

    public static async Task RegistrarAsync(HttpContext http, Exception excepcion, CancellationToken cancelacion)
    {
        var entrada = excepcion switch
        {
            EscrituraEntreOrganizacionesException e => new EntradaAuditoria(
                AccionEscrituraEntreOrganizaciones, e.Entidad, ObjetoId: null, ResultadoAuditoria.Denegado),
            AccesoDenegadoException e => new EntradaAuditoria(
                AccionAccesoDenegado, "Endpoint", ObjetoId: null, ResultadoAuditoria.Denegado,
                ValoresNuevos: new { e.Codigo, Metodo = http.Request.Method, Ruta = PlantillaRuta(http) }),
            _ => null,
        };
        if (entrada is null) return;

        var contexto = http.RequestServices.GetRequiredService<ConecsIADbContext>();
        // Se descartan los cambios de la operación rechazada: el guardado solo contiene el evento.
        contexto.ChangeTracker.Clear();
        http.RequestServices.GetRequiredService<IRegistroAuditoria>().Registrar(entrada);
        await contexto.SaveChangesAsync(cancelacion);
    }

    private static string? PlantillaRuta(HttpContext http) =>
        (http.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;
}
