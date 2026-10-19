using ConecsIA.Modules.Estructura.Domain;
using ConecsIA.SharedKernel.Errores;

namespace ConecsIA.UnitTests.Estructura;

/// <summary>HU-013: reglas del agregado Sede (zona horaria IANA, nombre normalizado, inactivación).</summary>
public class SedeTests
{
    private static readonly Guid Organizacion = Guid.NewGuid();
    private static readonly DateTimeOffset Ahora = new(2026, 10, 17, 15, 0, 0, TimeSpan.Zero);

    private static Sede CrearValida(string nombre = "Sede Norte", string zona = "America/Bogota") =>
        Sede.Crear(Organizacion, nombre, "Calle 1 # 2-3", "Bogotá", "Colombia", zona, Ahora);

    [Theory]
    [InlineData("America/Bogota")]
    [InlineData("Europe/Madrid")]
    [InlineData("UTC")]
    public void Acepta_zonas_horarias_IANA(string zona)
    {
        Assert.True(ZonaHoraria.EsIanaValida(zona));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Bogota")]
    [InlineData("America/NoExiste")]
    [InlineData("SA Pacific Standard Time")] // identificador Windows, no IANA
    [InlineData("-05:00")]
    public void Rechaza_zonas_horarias_que_no_son_IANA(string zona)
    {
        Assert.False(ZonaHoraria.EsIanaValida(zona));
    }

    [Fact]
    public void Crear_deja_la_sede_activa_con_nombre_normalizado_y_fechas_UTC()
    {
        var sede = CrearValida("  Sede   Norte ");

        Assert.Equal(EstadoSede.Activa, sede.Estado);
        Assert.Equal("Sede Norte", sede.Nombre);
        Assert.Equal("sede norte", sede.NombreNormalizado);
        Assert.Equal(Organizacion, sede.OrganizacionId);
        Assert.Equal(Ahora, sede.CreadaEn);
        Assert.Null(sede.InactivadaEn);
    }

    [Fact]
    public void Crear_con_zona_invalida_lanza_regla_de_negocio()
    {
        var error = Assert.Throws<ReglaNegocioException>(() => CrearValida(zona: "Hora/Local"));

        Assert.Equal(Sede.CodigoZonaHorariaInvalida, error.Codigo);
    }

    [Fact]
    public void Normaliza_mayusculas_tildes_no_y_espacios()
    {
        Assert.Equal("sede norte", Sede.NormalizarNombre(" SEDE  norte "));
        Assert.NotEqual(Sede.NormalizarNombre("Bogotá"), Sede.NormalizarNombre("Bogota"));
    }

    [Fact]
    public void Inactivar_cambia_estado_y_registra_instante()
    {
        var sede = CrearValida();
        var despues = Ahora.AddHours(1);

        sede.Inactivar(despues);

        Assert.Equal(EstadoSede.Inactiva, sede.Estado);
        Assert.Equal(despues, sede.InactivadaEn);
    }

    [Fact]
    public void Inactivar_dos_veces_lanza_regla_de_negocio()
    {
        var sede = CrearValida();
        sede.Inactivar(Ahora);

        var error = Assert.Throws<ReglaNegocioException>(() => sede.Inactivar(Ahora));

        Assert.Equal(Sede.CodigoYaInactiva, error.Codigo);
    }

    [Fact]
    public void Modificar_actualiza_datos_y_fecha()
    {
        var sede = CrearValida();
        var despues = Ahora.AddMinutes(5);

        sede.Modificar("Sede Sur", null, "Medellín", "Colombia", "America/Bogota", despues);

        Assert.Equal("Sede Sur", sede.Nombre);
        Assert.Equal("sede sur", sede.NombreNormalizado);
        Assert.Null(sede.Direccion);
        Assert.Equal(despues, sede.ActualizadaEn);
    }
}
