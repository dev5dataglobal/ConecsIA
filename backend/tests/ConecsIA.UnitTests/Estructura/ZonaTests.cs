using ConecsIA.Modules.Estructura.Domain;
using ConecsIA.SharedKernel.Errores;

namespace ConecsIA.UnitTests.Estructura;

/// <summary>HU-015 (ADR-019): invariantes modo/punto de acceso y operaciones admitidas.</summary>
public class ZonaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    private static Zona Nueva(bool punto, ModoPuntoAcceso? modo) =>
        Zona.Crear(Guid.NewGuid(), Guid.NewGuid(), "Portería", null, punto, modo, Ahora);

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, ModoPuntoAcceso.Entrada)]
    public void Modo_y_punto_de_acceso_deben_ser_consistentes(bool punto, ModoPuntoAcceso? modo)
    {
        Assert.Equal(Zona.CodigoModoInconsistente, Assert.Throws<ReglaNegocioException>(() => Nueva(punto, modo)).Codigo);
    }

    [Theory]
    [InlineData(ModoPuntoAcceso.Entrada, ModoPuntoAcceso.Entrada, true)]
    [InlineData(ModoPuntoAcceso.Entrada, ModoPuntoAcceso.Salida, false)]
    [InlineData(ModoPuntoAcceso.Ambos, ModoPuntoAcceso.Salida, true)]
    public void Admite_solo_las_operaciones_de_su_modo(ModoPuntoAcceso modo, ModoPuntoAcceso operacion, bool esperado)
    {
        Assert.Equal(esperado, Nueva(true, modo).Admite(operacion));
    }

    [Fact]
    public void Una_zona_que_no_es_punto_o_inactiva_no_admite_operaciones()
    {
        Assert.False(Nueva(false, null).Admite(ModoPuntoAcceso.Entrada));
        var inactiva = Nueva(true, ModoPuntoAcceso.Ambos);
        inactiva.Inactivar(Ahora);
        Assert.False(inactiva.Admite(ModoPuntoAcceso.Entrada));
    }
}
