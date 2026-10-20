using ConecsIA.Modules.Estructura.Domain;
using ConecsIA.SharedKernel.Errores;

namespace ConecsIA.UnitTests.Estructura;

/// <summary>HU-014: asignaciones con vigencia; nunca se borra el historial.</summary>
public class AreaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly Guid Beto = Guid.NewGuid();

    private static Area Nueva() => Area.Crear(Guid.NewGuid(), Guid.NewGuid(), "  Torre   1 ", "Torre", Ahora);

    [Fact]
    public void Normaliza_el_nombre()
    {
        var area = Nueva();

        Assert.Equal("Torre 1", area.Nombre);
        Assert.Equal("torre 1", area.NombreNormalizado);
    }

    [Fact]
    public void Reemplazar_vigentes_retira_con_fecha_y_agrega_sin_duplicar()
    {
        var area = Nueva();
        area.AsignarVigentes(FuncionEnArea.Autorizador, [Ana, Beto], null, Ahora);

        var (agregados, retirados) = area.AsignarVigentes(FuncionEnArea.Autorizador, [Beto, Beto], null, Ahora.AddDays(1));

        Assert.Empty(agregados);
        Assert.Equal([Ana], retirados);
        Assert.Equal(2, area.Asignaciones.Count);
        Assert.Equal(Ahora.AddDays(1), area.Asignaciones.Single(a => a.UsuarioId == Ana).HastaEn);
    }

    [Fact]
    public void Las_funciones_son_independientes()
    {
        var area = Nueva();
        area.AsignarVigentes(FuncionEnArea.Responsable, [Ana], null, Ahora);

        area.AsignarVigentes(FuncionEnArea.Autorizador, [], null, Ahora);

        Assert.Null(Assert.Single(area.Asignaciones).HastaEn);
    }

    [Fact]
    public void Volver_a_asignar_a_un_retirado_crea_una_vigencia_nueva()
    {
        var area = Nueva();
        area.AsignarVigentes(FuncionEnArea.Responsable, [Ana], null, Ahora);
        area.AsignarVigentes(FuncionEnArea.Responsable, [], null, Ahora.AddDays(1));

        area.AsignarVigentes(FuncionEnArea.Responsable, [Ana], null, Ahora.AddDays(2));

        Assert.Equal(2, area.Asignaciones.Count(a => a.UsuarioId == Ana));
    }

    [Fact]
    public void Inactivar_dos_veces_se_rechaza()
    {
        var area = Nueva();
        area.Inactivar(Ahora);

        Assert.Equal(Area.CodigoYaInactiva, Assert.Throws<ReglaNegocioException>(() => area.Inactivar(Ahora)).Codigo);
    }
}
