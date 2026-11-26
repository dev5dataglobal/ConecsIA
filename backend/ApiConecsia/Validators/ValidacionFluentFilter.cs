using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ApiConecsia.Validators;

/// <summary>
/// Ejecuta el <see cref="IValidator{T}"/> registrado para cada argumento de la acción y responde 400 (ValidationProblem)
/// si falla. Los argumentos sin validador no se tocan, así que los controladores existentes no cambian.
/// </summary>
public sealed class ValidacionFluentFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argumento in context.ActionArguments.Values)
        {
            if (argumento is null) continue;
            if (services.GetService(typeof(IValidator<>).MakeGenericType(argumento.GetType())) is not IValidator validador) continue;
            var resultado = await validador.ValidateAsync(new ValidationContext<object>(argumento), context.HttpContext.RequestAborted);
            if (resultado.IsValid) continue;
            foreach (var error in resultado.Errors) context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            var problema = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Solicitud no válida",
                Detail = string.Join(" ", resultado.Errors.Select(e => e.ErrorMessage).Distinct()),
            };
            context.Result = new BadRequestObjectResult(problema);
            return;
        }
        await next();
    }
}
