namespace ConecsIA.SharedKernel.Seguridad;

/// <summary>
/// Falta un permiso o la operación está fuera del alcance asignado dentro de la propia organización.
/// Se traduce a 403 con código estable. Un recurso de otra organización es 404, no 403 (ADR-020).
/// </summary>
public class AccesoDenegadoException(string codigo, string mensaje) : Exception(mensaje)
{
    public const string CodigoPermisoRequerido = "PERMISO_REQUERIDO";
    public const string CodigoFueraDeAlcance = "FUERA_DE_ALCANCE";

    public string Codigo { get; } = codigo;

    public static void ExigirPermiso(IContextoSolicitud contexto, string permiso)
    {
        if (!contexto.TienePermiso(permiso))
        {
            throw new AccesoDenegadoException(CodigoPermisoRequerido, "No tienes el permiso requerido para esta operación.");
        }
    }

    public static void ExigirPermisoSaas(IContextoSolicitud contexto, string permiso)
    {
        if (!contexto.TienePermisoSaas(permiso))
        {
            throw new AccesoDenegadoException(CodigoPermisoRequerido, "No tienes el permiso de plataforma requerido para esta operación.");
        }
    }

    public static void ExigirPunto(IContextoSolicitud contexto, Guid puntoId, Guid sedeId)
    {
        if (!contexto.PuedeOperarEnPunto(puntoId, sedeId))
        {
            throw new AccesoDenegadoException(CodigoFueraDeAlcance, "El punto de acceso está fuera de tu alcance asignado.");
        }
    }

    public static void ExigirSede(IContextoSolicitud contexto, Guid sedeId)
    {
        if (!contexto.PuedeOperarEnSede(sedeId))
        {
            throw new AccesoDenegadoException(CodigoFueraDeAlcance, "La sede está fuera de tu alcance asignado.");
        }
    }
}
