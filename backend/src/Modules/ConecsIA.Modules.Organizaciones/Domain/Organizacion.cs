namespace ConecsIA.Modules.Organizaciones.Domain;

public enum EstadoOrganizacion
{
    Activa,
    Inactiva,
}

/// <summary>
/// Frontera de aislamiento (RN-01). Mínimo necesario para la primera vertical: el alta, edición e
/// inactivación administrables llegan con HU-009 (efectos de inactivar: D-16).
/// </summary>
public sealed class Organizacion
{
    private Organizacion()
    {
        Nombre = string.Empty;
    }

    public Guid Id { get; private set; }
    public string Nombre { get; private set; }
    public EstadoOrganizacion Estado { get; private set; }
    public DateTimeOffset CreadaEn { get; private set; }

    public static Organizacion Crear(string nombre, DateTimeOffset ahora) => new()
    {
        Id = Guid.CreateVersion7(ahora),
        Nombre = nombre.Trim(),
        Estado = EstadoOrganizacion.Activa,
        CreadaEn = ahora,
    };

    /// <summary>Solo para semillas sintéticas con identificadores estables.</summary>
    public static Organizacion CrearConId(Guid id, string nombre, DateTimeOffset ahora)
    {
        var organizacion = Crear(nombre, ahora);
        organizacion.Id = id;
        return organizacion;
    }

    public void Inactivar() => Estado = EstadoOrganizacion.Inactiva;
}
