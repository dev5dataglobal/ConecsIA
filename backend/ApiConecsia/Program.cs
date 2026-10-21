using ConecsIA.Application.Interfaces.Repositories;
using ApiConecsia.Extensions;
using ApiConecsia.Helpers.Errors;
using ConecsIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Conecsia")
    ?? Environment.GetEnvironmentVariable("CONECSIA_CONNECTION_STRING");
builder.Services.AddDbContext<ConecsiaDbContext>(options => options.UseNpgsql(connectionString ?? "Host=localhost;Port=5432;Database=conecsia_dev;Username=postgres;Password=postgres", npgsql => npgsql.MigrationsAssembly(typeof(ConecsiaDbContext).Assembly.FullName)));
builder.Services
    .ConfigureCors()
    .AddApplicationServices()
    .AddValidationErrors()
    .AddGlobalRateLimiting();

builder.Services.AddJwtConfiguration(builder.Configuration);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("CorsPolicy");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.Run();
