using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Infrastructure.Repositories;
using ConecsIA.Infrastructure.UnitOfWork;
using ApiConecsia.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ConecsIA.Domain.Entities;
using ConecsIA.Application.Interfaces;

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
