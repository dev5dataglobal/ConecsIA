using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ConecsIA.Api.Validacion;

/// <summary>
/// Valida con FluentValidation (ADR-002) todo argumento de acción que tenga validador registrado.
/// Responde 400 con Problem Details y errores por campo (ADR-020, HU-004).
/// </summary>
public sealed class FiltroValidacion(IServiceProvider proveedor, ProblemDetailsFactory fabricaProblemas) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var contexto = context;
        var errores = new Dictionary<string, string[]>(StringComparer.Ordinal);

        // Errores de enlace (JSON malformado, cuerpo vacío, tipos inválidos): 400, nunca un argumento
        // null que llegue al caso de uso. No se repite el contenido recibido en el mensaje.
        foreach (var campo in contexto.ModelState.Where(e => e.Value?.Errors.Count > 0).Select(e => e.Key))
        {
            // Mensaje fijo: los del formateador JSON incluyen nombres de tipos internos.
            errores[CamelCase(campo.TrimStart('$', '.'))] = ["El cuerpo de la solicitud no es válido."];
        }

        foreach (var argumento in contexto.ActionArguments.Values)
        {
            if (argumento is null) continue;

            var tipoValidador = typeof(IValidator<>).MakeGenericType(argumento.GetType());
            if (proveedor.GetService(tipoValidador) is not IValidator validador) continue;

            var resultado = await validador.ValidateAsync(
                new ValidationContext<object>(argumento),
                contexto.HttpContext.RequestAborted);

            foreach (var grupo in resultado.Errors.GroupBy(e => CamelCase(e.PropertyName)))
            {
                errores[grupo.Key] = grupo.Select(e => e.ErrorMessage).ToArray();
            }
        }

        if (errores.Count == 0)
        {
            await next();
            return;
        }

        var estadoModelo = new ModelStateDictionary();
        foreach (var (campo, mensajes) in errores)
        {
            foreach (var mensaje in mensajes) estadoModelo.AddModelError(campo, mensaje);
        }

        // La fábrica de MVC conserva "errors" y aplica CustomizeProblemDetails (traceId).
        var problema = fabricaProblemas.CreateValidationProblemDetails(
            contexto.HttpContext,
            estadoModelo,
            StatusCodes.Status400BadRequest,
            title: "Error de validación");

        contexto.Result = new ObjectResult(problema)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" },
        };
    }

    private static string CamelCase(string nombre) =>
        string.IsNullOrEmpty(nombre) ? nombre : char.ToLowerInvariant(nombre[0]) + nombre[1..];
}
