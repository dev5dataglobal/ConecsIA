using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace ConecsIA.Infrastructure.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ConecsiaDbContext>
{
    public ConecsiaDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("CONECSIA_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "Defina CONECSIA_CONNECTION_STRING para ejecutar herramientas de Entity Framework.");
        var options = new DbContextOptionsBuilder<ConecsiaDbContext>().UseNpgsql(connection, npgsql => npgsql.MigrationsAssembly(typeof(DesignTimeDbContextFactory).Assembly.FullName)).Options;
        return new ConecsiaDbContext(options);
    }
}
