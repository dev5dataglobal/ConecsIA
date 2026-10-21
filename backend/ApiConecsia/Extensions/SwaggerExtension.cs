namespace ApiConecsia.Extensions;

public static class SwaggerExtension
{
    public static IServiceCollection AddSwaggerDocumentation(
        this IServiceCollection services)
    {
        services.AddOpenApi();
        return services;
    }
}
