using System.Reflection;
using NetArchTest.Rules;

namespace ConecsIA.ArchitectureTests;

/// <summary>
/// ADR-001: dentro de cada módulo, Dominio ← Aplicación ← Infraestructura/Api.
/// Los casos de uso no dependen de proveedores concretos (correo, almacenamiento, base de datos).
/// </summary>
public class CapasTests
{
    private static readonly string[] Frameworks =
    [
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
    ];

    public static TheoryData<string> Modulos() => FronterasDeModulosTests.Modulos();

    [Theory]
    [MemberData(nameof(Modulos))]
    public void El_dominio_no_depende_de_otras_capas_ni_de_frameworks(string nombreModulo)
    {
        var prohibidas = new[]
        {
            $"{nombreModulo}.Application",
            $"{nombreModulo}.Infrastructure",
            $"{nombreModulo}.Api",
            "FluentValidation",
        }.Concat(Frameworks).ToArray();

        var resultado = Types.InAssembly(Assembly.Load(nombreModulo))
            .That()
            .ResideInNamespace($"{nombreModulo}.Domain")
            .ShouldNot()
            .HaveDependencyOnAny(prohibidas)
            .GetResult();

        Assert.True(resultado.IsSuccessful, Fallos.Describir(resultado));
    }

    [Theory]
    [MemberData(nameof(Modulos))]
    public void La_aplicacion_no_depende_de_infraestructura_ni_de_la_api(string nombreModulo)
    {
        var prohibidas = new[]
        {
            $"{nombreModulo}.Infrastructure",
            $"{nombreModulo}.Api",
        }.Concat(Frameworks).ToArray();

        var resultado = Types.InAssembly(Assembly.Load(nombreModulo))
            .That()
            .ResideInNamespace($"{nombreModulo}.Application")
            .ShouldNot()
            .HaveDependencyOnAny(prohibidas)
            .GetResult();

        Assert.True(resultado.IsSuccessful, Fallos.Describir(resultado));
    }

    [Theory]
    [MemberData(nameof(Modulos))]
    public void La_infraestructura_no_depende_de_la_capa_api(string nombreModulo)
    {
        var resultado = Types.InAssembly(Assembly.Load(nombreModulo))
            .That()
            .ResideInNamespace($"{nombreModulo}.Infrastructure")
            .ShouldNot()
            .HaveDependencyOnAny($"{nombreModulo}.Api", "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(resultado.IsSuccessful, Fallos.Describir(resultado));
    }

    [Fact]
    public void El_shared_kernel_no_depende_de_modulos_infraestructura_ni_frameworks_web_o_de_datos()
    {
        var prohibidas = new[] { "ConecsIA.Modules", "ConecsIA.Infrastructure", "ConecsIA.Api" }
            .Concat(Frameworks)
            .ToArray();

        var resultado = Types.InAssembly(Ensamblados.SharedKernel)
            .ShouldNot()
            .HaveDependencyOnAny(prohibidas)
            .GetResult();

        Assert.True(resultado.IsSuccessful, Fallos.Describir(resultado));
    }

    [Fact]
    public void La_infraestructura_compartida_no_depende_de_la_api_ni_de_los_modulos()
    {
        var resultado = Types.InAssembly(Ensamblados.Infrastructure)
            .ShouldNot()
            .HaveDependencyOnAny("ConecsIA.Api", "ConecsIA.Modules", "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(resultado.IsSuccessful, Fallos.Describir(resultado));
    }
}
