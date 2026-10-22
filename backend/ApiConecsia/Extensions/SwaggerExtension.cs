namespace ApiConecsia.Extensions;

public static class SwaggerExtension
{
    public static IServiceCollection AddSwaggerDocumentation(
        this IServiceCollection services)
    {
        services.AddSwaggerGen();
        return services;
    }
}
