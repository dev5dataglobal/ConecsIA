using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ConecsIA.Api.Errores;

/// <summary>
/// Traduce excepciones a Problem Details según ADR-020:
/// 401 credenciales inválidas, 423 cuenta bloqueada (HU-006), 403 sin permiso o fuera de alcance, 404 no encontrado (también otra organización), 409 conflicto/concurrencia,
/// 422 regla de negocio con código estable, 500 sin detalles internos.
/// </summary>
public sealed partial class ManejadorExcepciones(
    IProblemDetailsService problemDetails,
    ILogger<ManejadorExcepciones> logger) : IExceptionHandler
{
    public const string CodigoConflictoConcurrencia = "CONFLICTO_CONCURRENCIA";
    public const string CodigoRegistroDuplicado = "REGISTRO_DUPLICADO";

    private const string ViolacionUnicidad = "23505";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problema = Clasificar(exception);

        if (exception is EscrituraEntreOrganizacionesException or AccesoDenegadoException)
        {
            await AuditarDenegacionAsync(httpContext, exception, cancellationToken);
        }

        if (problema.Status >= StatusCodes.Status500InternalServerError)
        {
            // Solo el tipo: el mensaje puede contener datos personales o SQL.
            RegistrarErrorNoControlado(logger, exception.GetType().FullName ?? exception.GetType().Name);
        }

        httpContext.Response.StatusCode = problema.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problema,
            Exception = exception,
        });
    }

    public static ProblemDetails Clasificar(Exception exception) => exception switch
    {
        CredencialesInvalidasException e => Crear(StatusCodes.Status401Unauthorized, "No autenticado", e.Message, CredencialesInvalidasException.Codigo),
        CuentaBloqueadaException e => Crear(StatusCodes.Status423Locked, "Cuenta bloqueada", e.Message, CuentaBloqueadaException.Codigo),
        RecursoNoEncontradoException e => Crear(StatusCodes.Status404NotFound, "Recurso no encontrado", e.Message),
        RecursoNoDisponibleException e => Crear(StatusCodes.Status410Gone, "Recurso no disponible", e.Message, e.Codigo),
        EscrituraEntreOrganizacionesException => Crear(StatusCodes.Status404NotFound, "Recurso no encontrado", detalle: null),
        AccesoDenegadoException e => Crear(StatusCodes.Status403Forbidden, "Acceso denegado", e.Message, e.Codigo),
        ConflictoException e => Crear(StatusCodes.Status409Conflict, "Conflicto", e.Message, e.Codigo),
        ReglaNegocioException e => Crear(StatusCodes.Status422UnprocessableEntity, "Regla de negocio", e.Message, e.Codigo),
        DbUpdateConcurrencyException => Crear(
            StatusCodes.Status409Conflict,
            "Conflicto de concurrencia",
            "El registro cambió desde que fue leído. Vuelve a consultarlo e inténtalo de nuevo.",
            CodigoConflictoConcurrencia),
        DbUpdateException { InnerException: PostgresException { SqlState: ViolacionUnicidad } } => Crear(
            StatusCodes.Status409Conflict,
            "Registro duplicado",
            "Ya existe un registro con esos datos.",
            CodigoRegistroDuplicado),
        _ => Crear(StatusCodes.Status500InternalServerError, "Error interno", detalle: null),
    };

    private async Task AuditarDenegacionAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        try
        {
            await AuditoriaDenegaciones.RegistrarAsync(httpContext, exception, cancellationToken);
        }
        catch (Exception fallo)
        {
            // La respuesta 404 no depende de la auditoría; solo se registra el tipo del fallo.
            RegistrarFalloAuditoria(logger, fallo.GetType().FullName ?? fallo.GetType().Name);
        }
    }

    private static ProblemDetails Crear(int estado, string titulo, string? detalle, string? codigo = null)
    {
        var problema = new ProblemDetails { Status = estado, Title = titulo, Detail = detalle };
        if (codigo is not null)
        {
            problema.Extensions[DetallesProblema.ClaveCodigo] = codigo;
        }

        return problema;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Excepción no controlada de tipo {TipoExcepcion}")]
    private static partial void RegistrarErrorNoControlado(ILogger logger, string tipoExcepcion);

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudo auditar una denegación: {TipoExcepcion}")]
    private static partial void RegistrarFalloAuditoria(ILogger logger, string tipoExcepcion);
}
