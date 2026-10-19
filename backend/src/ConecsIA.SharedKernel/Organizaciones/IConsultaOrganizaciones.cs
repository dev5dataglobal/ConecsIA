namespace ConecsIA.SharedKernel.Organizaciones;

public sealed record OrganizacionResumen(Guid Id, string Nombre, bool Activa);

/// <summary>Contrato del módulo Organizaciones para que otros módulos verifiquen su estado sin referenciarlo.</summary>
public interface IConsultaOrganizaciones
{
    Task<OrganizacionResumen?> ObtenerAsync(Guid id, CancellationToken cancelacion);
}
