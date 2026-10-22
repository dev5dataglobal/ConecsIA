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

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Conecsia")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("CONECSIA_CONNECTION_STRING")
    ?? throw new InvalidOperationException(
        "No se encontró una cadena de conexión para Conecsia.");

builder.Services.AddDbContext<ConecsiaDbContext>(options =>
{
    options.UseNpgsql(
        connectionString,
        npgsql => npgsql.MigrationsAssembly(
            typeof(ConecsiaDbContext).Assembly.FullName));
});

builder.Services.ConfigureCors();
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
builder.Services.AddApplicationServices();
builder.Services.AddGlobalRateLimiting();
builder.Services.AddValidationErrors();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

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
    app.MapOpenApi();
}

app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
