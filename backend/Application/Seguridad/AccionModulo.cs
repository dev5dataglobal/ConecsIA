namespace ConecsIA.Application.Seguridad;

/// <summary>
/// Acción sobre un módulo. Cada acción se concede por separado en modulos_permisos: ninguna implica a otra
/// (Añadir o Editar no habilitan Consultar). Inactivar/activar se considera Editar.
/// </summary>
public enum AccionModulo { Consultar, Anadir, Editar }

/// <summary>Acciones concedidas sobre un módulo (unión de todos los roles activos del usuario).</summary>
public readonly record struct AccionesModulo(bool Consultar, bool Anadir, bool Editar)
{
    public static readonly AccionesModulo Todas = new(true, true, true);
    public bool Alguna => Consultar || Anadir || Editar;

    public bool Permite(AccionModulo accion) => accion switch
    {
        AccionModulo.Consultar => Consultar,
        AccionModulo.Anadir => Anadir,
        _ => Editar,
    };

    public AccionesModulo Unir(AccionesModulo otra) => new(Consultar || otra.Consultar, Anadir || otra.Anadir, Editar || otra.Editar);

    /// <summary>Acciones de esta asignación que no están en <paramref name="propias"/> (para evitar escalamiento).</summary>
    public bool ExcedeA(AccionesModulo propias) => (Consultar && !propias.Consultar) || (Anadir && !propias.Anadir) || (Editar && !propias.Editar);
}
