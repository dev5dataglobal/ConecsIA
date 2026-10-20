namespace ConecsIA.SharedKernel.Seguridad;

/// <summary>
/// Catálogo cerrado de claves de permiso (ADR-005, HU-017). El código comprueba claves, nunca
/// nombres de rol. Formato <c>recurso.accion</c>; cada historia agrega las claves de su recurso.
/// </summary>
public static class Permisos
{
    public const string SedesConsultar = "sedes.consultar";
    public const string SedesCrear = "sedes.crear";
    public const string SedesModificar = "sedes.modificar";
    public const string SedesInactivar = "sedes.inactivar";

    public const string AreasConsultar = "areas.consultar";
    public const string AreasCrear = "areas.crear";
    public const string AreasModificar = "areas.modificar";
    public const string AreasInactivar = "areas.inactivar";
    public const string AreasAsignar = "areas.asignar";

    public const string OrganizacionConsultar = "organizacion.consultar";
    public const string OrganizacionModificar = "organizacion.modificar";

    public const string UsuariosConsultar = "usuarios.consultar";
    public const string UsuariosInvitar = "usuarios.invitar";
    public const string UsuariosAdministrar = "usuarios.administrar";

    public const string ConfiguracionModificar = "configuracion.modificar";

    public const string RolesConsultar = "roles.consultar";
    public const string RolesAdministrar = "roles.administrar";

    public static IReadOnlyList<string> Todos { get; } =
        CatalogoPermisos.Definiciones.Select(d => d.Clave).ToList();
}

/// <summary>Permiso del catálogo con su recurso y acción, para agrupar en la interfaz.</summary>
public sealed record DefinicionPermiso(string Clave, string Recurso, string Accion, string Descripcion);

public static class CatalogoPermisos
{
    public static IReadOnlyList<DefinicionPermiso> Definiciones { get; } =
    [
        Definir(Permisos.SedesConsultar, "Consultar sedes"),
        Definir(Permisos.SedesCrear, "Crear sedes"),
        Definir(Permisos.SedesModificar, "Modificar sedes"),
        Definir(Permisos.SedesInactivar, "Inactivar sedes"),
        Definir(Permisos.AreasConsultar, "Consultar áreas o destinos"),
        Definir(Permisos.AreasCrear, "Crear áreas o destinos"),
        Definir(Permisos.AreasModificar, "Modificar áreas o destinos"),
        Definir(Permisos.AreasInactivar, "Inactivar áreas o destinos"),
        Definir(Permisos.AreasAsignar, "Asignar responsables y autorizadores de áreas"),
        Definir(Permisos.OrganizacionConsultar, "Consultar la información general de la organización"),
        Definir(Permisos.OrganizacionModificar, "Modificar la información general de la organización"),
        Definir(Permisos.UsuariosConsultar, "Consultar los usuarios de la organización"),
        Definir(Permisos.UsuariosInvitar, "Invitar usuarios"),
        Definir(Permisos.UsuariosAdministrar, "Asignar roles y alcance, inactivar y reactivar usuarios"),
        Definir(Permisos.ConfiguracionModificar, "Modificar la configuración de la organización (vocabulario)"),
        Definir(Permisos.RolesConsultar, "Consultar roles y el catálogo de permisos"),
        Definir(Permisos.RolesAdministrar, "Crear, modificar e inactivar roles"),
    ];

    public static bool Existe(string clave) => Definiciones.Any(d => d.Clave == clave);

    private static DefinicionPermiso Definir(string clave, string descripcion)
    {
        var punto = clave.IndexOf('.', StringComparison.Ordinal);
        return new DefinicionPermiso(clave, clave[..punto], clave[(punto + 1)..], descripcion);
    }
}
