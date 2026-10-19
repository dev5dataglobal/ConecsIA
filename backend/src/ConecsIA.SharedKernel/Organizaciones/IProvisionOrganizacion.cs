namespace ConecsIA.SharedKernel.Organizaciones;

/// <summary>
/// Contrato del módulo Identidad para el alta de organizaciones (HU-009): copia las plantillas de rol
/// y crea (o reutiliza por correo) al primer administrador con el rol de administración. No guarda:
/// queda en la misma unidad de trabajo que el alta.
/// </summary>
public interface IProvisionOrganizacion
{
    Task<Guid> ProvisionarAsync(Guid organizacionId, string correoAdministrador, string nombreAdministrador, CancellationToken cancelacion);
}
