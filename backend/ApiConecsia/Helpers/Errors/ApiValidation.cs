namespace ApiConecsia.Helpers.Errors;

public sealed class ApiValidation : ApiResponse
{
    public ApiValidation()
        : base(StatusCodes.Status400BadRequest)
    {
    }

    public IEnumerable<string> Errors { get; init; } = [];
}
