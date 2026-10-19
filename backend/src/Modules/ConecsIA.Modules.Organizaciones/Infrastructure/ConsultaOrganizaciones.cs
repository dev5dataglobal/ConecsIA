using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Organizaciones.Domain;
using ConecsIA.SharedKernel.Organizaciones;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Modules.Organizaciones.Infrastructure;

internal sealed class ConsultaOrganizaciones(ConecsIADbContext contexto) : IConsultaOrganizaciones
{
    public Task<OrganizacionResumen?> ObtenerAsync(Guid id, CancellationToken cancelacion) =>
        contexto.Set<Organizacion>()
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OrganizacionResumen(o.Id, o.Nombre, o.Estado == EstadoOrganizacion.Activa))
            .SingleOrDefaultAsync(cancelacion);
}
