namespace ConecsIA.SharedKernel.Seguridad;

/// <summary>
/// Permisos del plano SaaS (HU-009, PRD §5.2.6). Pertenecen al usuario, no a una membresía, y no
/// conceden acceso ordinario a los datos de las organizaciones cliente: ese acceso exige membresía.
/// </summary>
public static class PermisosSaas
{
    public const string OrganizacionesConsultar = "saas.organizaciones.consultar";
    public const string OrganizacionesAdministrar = "saas.organizaciones.administrar";

    public static IReadOnlyList<string> Todos { get; } = [OrganizacionesConsultar, OrganizacionesAdministrar];
}

/// <summary>
/// Permite a un caso de uso del plano SaaS escribir en una organización concreta (p. ej. aprovisionar
/// la que acaba de crear). Solo funciona si el usuario tiene el permiso SaaS de administración; no
/// concede permisos de organización.
/// </summary>
public interface IContextoPlanoSaas
{
    void OperarEnOrganizacion(Guid organizacionId);
}
