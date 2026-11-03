using System.Net;
using System.Text.Json;

namespace ApiConecsia.Helpers.Errors;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException exception) when (!context.Response.HasStarted)
        {
            // Escritura fuera de la organización activa (validación del DbContext) u operación sin organización.
            _logger.LogWarning("Operación rechazada por alcance de organización: {Mensaje}", exception.Message);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 403, Title = "Acceso denegado", Detail = "La operación no pertenece a la organización activa." },
                options: null, contentType: "application/problem+json");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error no controlado durante la solicitud.");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = _environment.IsDevelopment()
                ? new ApiException(
                    context.Response.StatusCode,
                    exception.Message,
                    exception.StackTrace)
                : new ApiException(context.Response.StatusCode);

            await context.Response.WriteAsJsonAsync(
                response,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
        }
    }
}
