using System.Reflection;
using System.Text;
using ApiConecsia.Extensions;
using ApiConecsia.Helpers;
using ApiConecsia.Helpers.Errors;
using ApiConecsia.Profiles;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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

if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException(
        "La clave JWT no está configurada.");
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
    });

builder.Services.AddAuthorization();

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
app.UseAuthorization();
app.MapControllers();

app.Run();
