using ConecsIA.Api.Composicion;
using ConecsIA.Api.Seguridad;
using NetArchTest.Rules;

namespace ConecsIA.ArchitectureTests;

/// <summary>
/// ADR-022 / HU-006: el esquema provisional de autenticación debe poder sustituirse cambiando solo la
/// composición. Contexto, permisos, alcance y auditoría no conocen el esquema concreto.
/// </summary>
public class AutenticacionTests
{
    [Fact]
    public void Solo_la_composicion_conoce_el_esquema_de_desarrollo()
    {
        var resultado = Types.InAssembly(Ensamblados.Api)
            .That()
            .DoNotHaveName(nameof(AutenticacionDesarrollo), nameof(ExtensionesServicios))
            .ShouldNot()
            .HaveDependencyOn(typeof(AutenticacionDesarrollo).FullName)
            .GetResult();

        Assert.True(resultado.IsSuccessful, Fallos.Describir(resultado));
    }

    [Theory]
    [MemberData(nameof(FronterasDeModulosTests.Modulos), MemberType = typeof(FronterasDeModulosTests))]
    public void Los_modulos_no_dependen_de_un_esquema_de_autenticacion(string nombreModulo)
    {
        var resultado = Types.InAssembly(System.Reflection.Assembly.Load(nombreModulo))
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.AspNetCore.Authentication", "ConecsIA.Api")
            .GetResult();

        Assert.True(resultado.IsSuccessful, Fallos.Describir(resultado));
    }
}
