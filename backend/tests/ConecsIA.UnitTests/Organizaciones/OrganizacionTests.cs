using ConecsIA.Modules.Organizaciones.Domain;
using ConecsIA.SharedKernel.Errores;

namespace ConecsIA.UnitTests.Organizaciones;

/// <summary>HU-009 y HU-010: estados, tipo y normalización del nombre de la organización.</summary>
public class OrganizacionTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Una_organizacion_nueva_queda_activa_y_con_el_nombre_normalizado()
    {
        var organizacion = Organizacion.Crear("  Conjunto   Los Pinos ", "conjunto-residencial", null, "America/Bogota", null, Ahora);

        Assert.Equal(EstadoOrganizacion.Activa, organizacion.Estado);
        Assert.Equal("Conjunto Los Pinos", organizacion.Nombre);
        Assert.Equal("conjunto los pinos", organizacion.NombreNormalizado);
    }

    [Fact]
    public void Inactivar_y_reactivar_registran_las_fechas_y_rechazan_repetir_el_estado()
    {
        var organizacion = Organizacion.Crear("Club", "club", null, null, null, Ahora);

        organizacion.Inactivar(Ahora);
        Assert.Equal(Ahora, organizacion.InactivadaEn);
        Assert.Equal(Organizacion.CodigoYaInactiva, Assert.Throws<ReglaNegocioException>(() => organizacion.Inactivar(Ahora)).Codigo);

        organizacion.Reactivar(Ahora);
        Assert.Null(organizacion.InactivadaEn);
        Assert.Equal(Organizacion.CodigoYaActiva, Assert.Throws<ReglaNegocioException>(() => organizacion.Reactivar(Ahora)).Codigo);
    }

    [Fact]
    public void Un_tipo_inexistente_se_rechaza()
    {
        var error = Assert.Throws<ReglaNegocioException>(() => Organizacion.Crear("X", "nave", null, null, null, Ahora));

        Assert.Equal(TiposOrganizacion.CodigoTipoDesconocido, error.Codigo);
    }

    [Fact]
    public void Una_zona_horaria_que_no_es_IANA_se_rechaza()
    {
        var error = Assert.Throws<ReglaNegocioException>(() => Organizacion.Crear("X", "empresa", null, "SA Pacific Standard Time", null, Ahora));

        Assert.Equal(Organizacion.CodigoZonaHorariaInvalida, error.Codigo);
    }

    [Fact]
    public void El_administrador_no_cambia_el_tipo_al_editar_la_informacion_general()
    {
        var organizacion = Organizacion.Crear("Andes", "empresa", null, null, null, Ahora);

        organizacion.ModificarInformacionGeneral("Andes S.A.", "900", "America/Lima", "Gerencia", Ahora);

        Assert.Equal("empresa", organizacion.Tipo);
        Assert.Equal("Andes S.A.", organizacion.Nombre);
    }
}
