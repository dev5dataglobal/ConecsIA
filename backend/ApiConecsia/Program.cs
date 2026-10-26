using System.Reflection;
using System.Text;
using ApiConecsia.Extensions;
using ApiConecsia.Helpers;
using ApiConecsia.Helpers.Errors;
using ApiConecsia.Profiles;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    "appsettings.Local.json",
    optional: true,
    reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("Conecsia");

if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString(
        "DefaultConnection");
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = Environment.GetEnvironmentVariable(
        "CONECSIA_CONNECTION_STRING");
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No se encontró una cadena de conexión para Conecsia.");
}

builder.Services.AddDbContext<ConecsiaDbContext>(options =>
{
    options.UseNpgsql(
        connectionString,
        npgsql => npgsql.MigrationsAssembly(
            typeof(ConecsiaDbContext).Assembly.FullName));
});

builder.Services.ConfigureCors();
builder.Services.AddAutoMapper(
    _ => { },
    Assembly.GetExecutingAssembly());
builder.Services.AddApplicationServices();
builder.Services.AddGlobalRateLimiting();
builder.Services.AddValidationErrors();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ConecsIA API",
        Version = "v1",
        Description = "API para la gestión de accesos y organizaciones."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Escriba: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var jwtSection = builder.Configuration.GetSection("JWT");
var jwtSettings = jwtSection.Get<JWT>()
    ?? throw new InvalidOperationException(
        "La sección JWT no está configurada.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
{
    throw new InvalidOperationException(
        "La clave JWT no está configurada.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) ||
    string.IsNullOrWhiteSpace(jwtSettings.Audience) ||
    jwtSettings.DurationInMinutes <= 0 || jwtSettings.DurationInMinutes > 120)
{
    throw new InvalidOperationException(
        "JWT:Issuer, JWT:Audience y JWT:DurationInMinutes deben estar configurados; la duración debe ser mayor que 0 y no superar 120 minutos.");
}

builder.Services.Configure<JWT>(jwtSection);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Principal?.FindFirstValue("sub");
                if (!int.TryParse(subject, out var userId))
                {
                    context.Fail("El token no contiene un identificador de usuario válido.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<ConecsiaDbContext>();
                var user = await db.Usuarios.IgnoreQueryFilters()
                    .SingleOrDefaultAsync(x => x.IdUsuario == userId);
                var stamp = context.Principal?.FindFirstValue("security_stamp");
                if (user is null || !user.Activo || user.SelloSeguridad != stamp)
                {
                    context.Fail("La sesión ya no es válida.");
                    return;
                }

                // Una membresía inactivada (o una organización inactivada) cierra las sesiones abiertas en ella.
                if (!UserAuthorization.EsUsuarioAdministradorGeneral(userId)
                    && int.TryParse(context.Principal?.FindFirstValue(UserAuthorization.Claims.OrganizationId), out var orgId)
                    && !await db.MembresiasOrganizacion.IgnoreQueryFilters().AnyAsync(m =>
                        m.IdUsuario == userId && m.IdOrganizacion == orgId && (m.Estado == "Activa" || m.Estado == "Aceptada")
                        && db.Organizaciones.Any(o => o.IdOrganizacion == orgId && o.Estado == "Activa")))
                {
                    context.Fail("La sesión ya no es válida.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.AddPolicy(UserAuthorization.Policies.Authenticated,
        policy => policy.RequireAuthenticatedUser());
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "ConecsIA API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<ApiConecsia.Services.ContextoOrganizacionMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>Expuesto para las pruebas de integración (WebApplicationFactory).</summary>
public partial class Program;
