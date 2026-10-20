using ConecsIA.SharedKernel.Tenancy;

namespace ConecsIA.Modules.Organizaciones.Domain;

/// <summary>
/// Estado de un módulo opcional en una organización (HU-011): quién y cuándo lo cambió. Deshabilitar
/// conserva la fila y los datos del módulo; solo impide operaciones nuevas.
/// </summary>
public sealed class ModuloOrganizacion : IEntidadDeOrganizacion
{
    private ModuloOrganizacion()
    {
        Modulo = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public string Modulo { get; private set; }
    public bool Habilitado { get; private set; }
    public DateTimeOffset CambiadoEn { get; private set; }
    public Guid? CambiadoPor { get; private set; }

    public static ModuloOrganizacion Crear(Guid organizacionId, string modulo, bool habilitado, Guid? actor, DateTimeOffset ahora) => new()
    {
        Id = Guid.CreateVersion7(ahora),
        OrganizacionId = organizacionId,
        Modulo = modulo,
        Habilitado = habilitado,
        CambiadoEn = ahora,
        CambiadoPor = actor,
    };

    /// <summary>Devuelve true si el estado cambió.</summary>
    public bool Cambiar(bool habilitado, Guid? actor, DateTimeOffset ahora)
    {
        if (Habilitado == habilitado) return false;
        Habilitado = habilitado;
        CambiadoEn = ahora;
        CambiadoPor = actor;
        return true;
    }
}
