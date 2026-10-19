using ConecsIA.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ConecsIA.Migrations;

/// <summary>
/// Fábrica para `dotnet ef` (ADR-003). Este ensamblado es el único que conoce a todos los módulos,
/// por eso las migraciones viven aquí y no en la infraestructura compartida.
/// La cadena de diseño no se usa para conectarse al generar scripts.
/// </summary>
public sealed class FabricaDisenoConecsIA : IDesignTimeDbContextFactory<ConecsIADbContext>
{
    public ConecsIADbContext CreateDbContext(string[] args)
    {
        var cadena = Environment.GetEnvironmentVariable("CONECSIA_MIGRACIONES")
                     ?? "Host=localhost;Database=conecsia;Username=conecsia_migrator";

        var opciones = OpcionesPersistencia.Configurar(new DbContextOptionsBuilder<ConecsIADbContext>(), cadena).Options;
        return new ConecsIADbContext((DbContextOptions<ConecsIADbContext>)opciones, EnsambladosModulos.Modelo);
    }
}
