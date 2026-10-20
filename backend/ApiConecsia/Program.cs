using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Infrastructure.Data;
using ConecsIA.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Conecsia")
    ?? Environment.GetEnvironmentVariable("CONECSIA_CONNECTION_STRING");
builder.Services.AddDbContext<ConecsiaDbContext>(options => options.UseNpgsql(connectionString ?? "Host=localhost;Port=5432;Database=conecsia_dev;Username=postgres;Password=postgres", npgsql => npgsql.MigrationsAssembly(typeof(ConecsiaDbContext).Assembly.FullName)));
builder.Services.AddScoped<IOrganizacionRepository, OrganizacionRepository>();
builder.Services.AddScoped<IPersonaRepository, PersonaRepository>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
