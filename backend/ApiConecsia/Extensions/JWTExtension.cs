using ApiConecsia.Helpers;

namespace ApiConecsia.Extensions;

public static class JwtExtension
{
    public static IServiceCollection AddJwtConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JWT>(configuration.GetSection("JWT"));
        services.AddAuthorization();

        return services;
    }
}
