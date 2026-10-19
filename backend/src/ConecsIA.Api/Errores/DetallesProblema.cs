using System.Diagnostics;

namespace ConecsIA.Api.Errores;

public static class DetallesProblema
{
    public const string ClaveTraceId = "traceId";
    public const string ClaveCodigo = "code";

    /// <summary>
    /// Toda respuesta Problem Details lleva un traceId para soporte (HU-004) y nunca detalles
    /// internos como trazas o SQL.
    /// </summary>
    public static void Enriquecer(ProblemDetailsContext contexto)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? contexto.HttpContext.TraceIdentifier;
        contexto.ProblemDetails.Extensions[ClaveTraceId] = traceId;
        contexto.ProblemDetails.Instance ??= contexto.HttpContext.Request.Path;

        if (contexto.ProblemDetails.Status >= StatusCodes.Status500InternalServerError)
        {
            contexto.ProblemDetails.Detail = null;
            contexto.ProblemDetails.Extensions.Remove("exception");
        }
    }
}
