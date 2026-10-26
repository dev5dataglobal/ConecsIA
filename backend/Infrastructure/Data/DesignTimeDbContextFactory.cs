using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ConecsIA.Infrastructure.Data;

/// <summary>
/// Fábrica usada por las herramientas de EF (migrations, database update).
/// Resuelve la cadena de conexión con la misma precedencia que ApiConecsia/Program.cs:
/// ConnectionStrings:Conecsia → ConnectionStrings:DefaultConnection → CONECSIA_CONNECTION_STRING.
/// Los archivos appsettings se leen del directorio del proyecto de inicio (--startup-project).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ConecsiaDbContext>
{
    public ConecsiaDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connection = new[]
            {
                configuration.GetConnectionString("Conecsia"),
                configuration.GetConnectionString("DefaultConnection"),
                Environment.GetEnvironmentVariable("CONECSIA_CONNECTION_STRING"),
            }
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? throw new InvalidOperationException(
                "No se encontró una cadena de conexión para Conecsia. Defina ConnectionStrings:DefaultConnection " +
                "en appsettings.Local.json del proyecto de inicio o la variable CONECSIA_CONNECTION_STRING.");

        var options = new DbContextOptionsBuilder<ConecsiaDbContext>()
            .UseNpgsql(connection, npgsql => npgsql.MigrationsAssembly(typeof(DesignTimeDbContextFactory).Assembly.FullName))
            .Options;
        return new ConecsiaDbContext(options);
    }
}
