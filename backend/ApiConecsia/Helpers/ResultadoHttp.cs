using ApiConecsia.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Helpers;

/// <summary>Traduce <see cref="Resultado{T}"/> a respuestas HTTP con ProblemDetails (ADR-020).</summary>
public static class ResultadoHttp
{
    public static ActionResult Respuesta<T>(this ControllerBase controller, Resultado<T> resultado) => resultado.Estado switch
    {
        ResultadoGestion.Ok => controller.Ok(resultado.Valor),
        ResultadoGestion.NoEncontrado => controller.Problem(statusCode: StatusCodes.Status404NotFound, title: "No encontrado", detail: resultado.Detalle),
        ResultadoGestion.Prohibido => controller.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Acceso denegado", detail: resultado.Detalle),
        ResultadoGestion.Conflicto => controller.Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflicto", detail: resultado.Detalle),
        _ => controller.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Solicitud no válida", detail: resultado.Detalle),
    };
}
