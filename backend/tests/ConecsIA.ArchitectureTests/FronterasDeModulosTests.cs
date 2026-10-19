using System.Reflection;
using NetArchTest.Rules;

namespace ConecsIA.ArchitectureTests;

/// <summary>
/// ADR-001: monolito modular. Cada módulo es dueño de sus capas y solo se comunica con otros
/// módulos mediante contratos públicos (ensamblados *.Contracts).
/// </summary>
public class FronterasDeModulosTests
{
    public static TheoryData<string> Modulos()
    {
        var datos = new TheoryData<string>();
        foreach (var modulo in Ensamblados.ModulosEnSalida)
        {
            datos.Add(Ensamblados.NombreModulo(modulo));
        }
        return datos;
    }

    [Fact]
    public void Existen_los_modulos_esperados_del_nucleo()
    {
        var nombres = Ensamblados.ModulosEnSalida.Select(Ensamblados.NombreModulo).ToList();

        Assert.Contains("ConecsIA.Modules.Organizaciones", nombres);
        Assert.Contains("ConecsIA.Modules.Identidad", nombres);
        Assert.Contains("ConecsIA.Modules.Estructura", nombres);
        Assert.Contains("ConecsIA.Modules.Personas", nombres);
        Assert.Contains("ConecsIA.Modules.Autorizaciones", nombres);
        Assert.Contains("ConecsIA.Modules.Accesos", nombres);
        Assert.Contains("ConecsIA.Modules.Auditoria", nombres);
    }

    [Fact]
    public void Todo_modulo_compilado_esta_registrado_en_la_composicion()
    {
        var registrados = Ensamblados.ModulosRegistrados.Select(Ensamblados.NombreModulo).ToHashSet();
        var sinRegistrar = Ensamblados.ModulosEnSalida
            .Select(Ensamblados.NombreModulo)
            .Where(n => !n.EndsWith(".Contracts", StringComparison.Ordinal) && !registrados.Contains(n))
            .ToList();

        Assert.True(sinRegistrar.Count == 0, $"Módulos sin registrar: {string.Join(", ", sinRegistrar)}");
    }

    [Theory]
    [MemberData(nameof(Modulos))]
    public void Un_modulo_no_referencia_a_otro_salvo_por_sus_contratos(string nombreModulo)
    {
        var modulo = Assembly.Load(nombreModulo);

        var indebidas = modulo
            .GetReferencedAssemblies()
            .Select(r => r.Name!)
            .Where(r => r.StartsWith(Ensamblados.PrefijoModulos, StringComparison.Ordinal))
            .Where(r => r != nombreModulo && !r.EndsWith(".Contracts", StringComparison.Ordinal))
            .ToList();

        Assert.True(indebidas.Count == 0, $"{nombreModulo} referencia: {string.Join(", ", indebidas)}");
    }

    [Theory]
    [MemberData(nameof(Modulos))]
    public void Un_modulo_no_depende_del_host_de_la_api(string nombreModulo)
    {
        var resultado = Types.InAssembly(Assembly.Load(nombreModulo))
            .ShouldNot()
            .HaveDependencyOn("ConecsIA.Api")
            .GetResult();

        Assert.True(resultado.IsSuccessful, Fallos.Describir(resultado));
    }

    [Theory]
    [MemberData(nameof(Modulos))]
    public void Los_tipos_de_un_modulo_viven_bajo_su_espacio_de_nombres(string nombreModulo)
    {
        // Se excluyen los tipos generados por el compilador y sus anidados (p. ej. <>z__...+Enumerator).
        var fuera = Assembly.Load(nombreModulo).GetTypes()
            .Where(t => !EsGeneradoPorElCompilador(t))
            .Where(t => t.Namespace is null || !t.Namespace.StartsWith(nombreModulo, StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(fuera.Count == 0, $"Tipos fuera del espacio de nombres del módulo: {string.Join(", ", fuera)}");
    }

    private static bool EsGeneradoPorElCompilador(Type tipo)
    {
        for (var t = tipo; t is not null; t = t.DeclaringType)
        {
            if (t.Name.StartsWith('<')) return true;
        }

        return false;
    }
}
