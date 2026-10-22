namespace ApiConecsia.Helpers.Errors;

public class ApiResponse
{
    public ApiResponse(int statusCode, string? message = null)
    {
        StatusCode = statusCode;
        Message = message ?? GetDefaultMessage(statusCode);
    }

    public int StatusCode { get; }
    public string Message { get; }

    private static string GetDefaultMessage(int statusCode)
    {
        return statusCode switch
        {
            400 => "La solicitud no es válida.",
            401 => "El usuario no está autenticado.",
            403 => "El usuario no tiene permisos suficientes.",
            404 => "El recurso solicitado no existe.",
            405 => "El método HTTP no está permitido.",
            429 => "Se superó el límite de solicitudes.",
            500 => "Ocurrió un error interno en el servidor.",
            _ => "Ocurrió un error al procesar la solicitud."
        };
    }
}
