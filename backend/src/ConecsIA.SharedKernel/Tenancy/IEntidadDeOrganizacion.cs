namespace ConecsIA.SharedKernel.Tenancy;

/// <summary>
/// Entidad de cliente atada a una organización (ADR-004). El modelo le aplica un filtro global y
/// el guardado rechaza escrituras cuya organización difiera del contexto verificado.
/// </summary>
public interface IEntidadDeOrganizacion
{
    Guid OrganizacionId { get; }
}

/// <summary>
/// Excepción explícita y revisada al filtro global: la entidad tiene organización pero se consulta
/// por otra clave (p. ej. membresías por usuario). Una prueba exige la justificación.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SinFiltroDeOrganizacionAttribute(string justificacion) : Attribute
{
    public string Justificacion { get; } = justificacion;
}
