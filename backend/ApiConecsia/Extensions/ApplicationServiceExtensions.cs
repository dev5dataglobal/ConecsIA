using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Infrastructure.Repositories;
using ConecsIA.Infrastructure.UnitOfWork;
using ApiConecsia.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ConecsIA.Domain.Entities;
using ConecsIA.Application.Interfaces;
using ConecsIA.Infrastructure.Services;
using ApiConecsia.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace ApiConecsia.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection ConfigureCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IOrganizacionRepository, OrganizacionRepository>();
        services.AddScoped<IPersonaRepository, PersonaRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddScoped<IUserService, UserService>();
        services.AddHttpContextAccessor();
        services.AddScoped<IOrganizationContext, OrganizationContext>();

        // Autorización dinámica por módulo (app_modulos / modulos_permisos).
        services.AddMemoryCache();
        services.AddSingleton<ModuloAccesoCacheTokens>();
        services.AddScoped<IModuloPermisoRepository, ModuloPermisoRepository>();
        services.AddScoped<IModuloAccesoService, ModuloAccesoService>();
        services.AddScoped<ModuloPermisoService>();
        services.AddSingleton<IAuthorizationPolicyProvider, ModuloPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, ModuloAuthorizationHandler>();

        return services;
    }

    public static IServiceCollection AddValidationErrors(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .SelectMany(entry => entry.Value!.Errors)
                    .Select(error => error.ErrorMessage)
                    .ToArray();

                return new BadRequestObjectResult(new ApiConecsia.Helpers.Errors.ApiValidation
                {
                    Errors = errors
                });
            };
        });

        return services;
    }
}
