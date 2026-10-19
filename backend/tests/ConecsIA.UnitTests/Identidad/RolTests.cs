using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Seguridad;

namespace ConecsIA.UnitTests.Identidad;

/// <summary>HU-017: roles por organización a partir del catálogo cerrado de permisos (ADR-005).</summary>
public class RolTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Org = Guid.NewGuid();

    [Fact]
    public void Crear_normaliza_el_nombre_y_quita_permisos_repetidos()
    {
        var rol = Rol.Crear(Org, "  Portería   Norte ", null, [Permisos.SedesConsultar, Permisos.SedesConsultar], plantilla: null, Ahora);

        Assert.Equal("Portería Norte", rol.Nombre);
        Assert.Equal("portería norte", rol.NombreNormalizado);
        Assert.Equal([Permisos.SedesConsultar], rol.Permisos);
        Assert.True(rol.Activo);
    }

    [Fact]
    public void Un_permiso_fuera_del_catalogo_se_rechaza()
    {
        var error = Assert.Throws<ReglaNegocioException>(() =>
            Rol.Crear(Org, "Inventado", null, ["superpoder.total"], plantilla: null, Ahora));

        Assert.Equal(Rol.CodigoPermisoDesconocido, error.Codigo);
    }

    [Fact]
    public void El_nombre_del_rol_no_otorga_permisos()
    {
        var rol = Rol.Crear(Org, "Administrador", null, [], plantilla: null, Ahora);

        Assert.Empty(rol.Permisos);
    }

    [Fact]
    public void Inactivar_dos_veces_se_rechaza()
    {
        var rol = Rol.Crear(Org, "Auditor", null, [Permisos.RolesConsultar], plantilla: null, Ahora);
        rol.Inactivar(Ahora);

        var error = Assert.Throws<ReglaNegocioException>(() => rol.Inactivar(Ahora));
        Assert.Equal(Rol.CodigoYaInactivo, error.Codigo);
    }

    [Fact]
    public void Las_plantillas_solo_usan_permisos_del_catalogo_y_ninguna_es_superadministrador_saas()
    {
        Assert.NotEmpty(PlantillasRol.Todas);
        Assert.All(PlantillasRol.Todas, p => Assert.All(p.Permisos, permiso => Assert.Contains(permiso, Permisos.Todos)));
        Assert.DoesNotContain(PlantillasRol.Todas, p => p.Clave.Contains("saas", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void El_catalogo_describe_cada_permiso_con_recurso_y_accion()
    {
        Assert.Equal(Permisos.Todos.Order(), CatalogoPermisos.Definiciones.Select(d => d.Clave).Order());
        Assert.All(CatalogoPermisos.Definiciones, d => Assert.Equal(d.Clave, $"{d.Recurso}.{d.Accion}"));
    }
}
