using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.ArchitectureTests;

/// <summary>
/// ADR-021, condición 1: el contexto es compartido, pero cada módulo solo configura entidades de su
/// propio espacio de nombres. Así ningún módulo toma el control del esquema de otro.
/// </summary>
public class PersistenciaPorModuloTests
{
    public static TheoryData<string> Modulos() => FronterasDeModulosTests.Modulos();

    [Theory]
    [MemberData(nameof(Modulos))]
    public void Un_modulo_solo_configura_entidades_de_su_espacio_de_nombres(string nombreModulo)
    {
        var ajenas = ConfiguracionesDe(Assembly.Load(nombreModulo))
            .Where(c => c.Entidad.Namespace is null || !c.Entidad.Namespace.StartsWith(nombreModulo + ".", StringComparison.Ordinal))
            .Select(c => $"{c.Configuracion.Name} → {c.Entidad.FullName}")
            .ToList();

        Assert.True(ajenas.Count == 0, $"{nombreModulo} configura entidades ajenas: {string.Join(", ", ajenas)}");
    }

    [Fact]
    public void La_infraestructura_compartida_no_configura_entidades_de_los_modulos()
    {
        var configuraciones = ConfiguracionesDe(Ensamblados.Infrastructure)
            .Select(c => c.Configuracion.Name)
            .ToList();

        Assert.True(configuraciones.Count == 0, $"Configuraciones en Infrastructure: {string.Join(", ", configuraciones)}");
    }

    [Fact]
    public void La_regla_detecta_una_configuracion_de_entidad_ajena()
    {
        // Control de la propia prueba: un tipo de este ensamblado configura una entidad de otro espacio.
        var ajenas = ConfiguracionesDe(typeof(PersistenciaPorModuloTests).Assembly)
            .Where(c => !c.Entidad.Namespace!.StartsWith("ConecsIA.ArchitectureTests", StringComparison.Ordinal))
            .ToList();

        Assert.Single(ajenas);
    }

    private static IEnumerable<(Type Configuracion, Type Entidad)> ConfiguracionesDe(Assembly ensamblado) =>
        ensamblado.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
                .Select(i => (t, i.GetGenericArguments()[0])));

    // Solo existe para La_regla_detecta_una_configuracion_de_entidad_ajena.
    private sealed class ConfiguracionAjenaDeControl : IEntityTypeConfiguration<Uri>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Uri> builder)
        {
        }
    }
}
