using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace ConecsIA.Infrastructure.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ConecsiaDbContext>
{
    public ConecsiaDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("CONECSIA_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=conecsia_dev;Username=postgres;Password=Lau05032015";
        var options = new DbContextOptionsBuilder<ConecsiaDbContext>().UseNpgsql(connection, npgsql => npgsql.MigrationsAssembly(typeof(DesignTimeDbContextFactory).Assembly.FullName)).Options;
        return new ConecsiaDbContext(options);
    }
}
