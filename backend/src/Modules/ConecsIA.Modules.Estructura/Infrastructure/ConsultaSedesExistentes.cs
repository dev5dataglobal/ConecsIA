using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Estructura.Domain;
using ConecsIA.SharedKernel.Estructura;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Modules.Estructura.Infrastructure;

internal sealed class ConsultaSedesExistentes(ConecsIADbContext contexto) : IConsultaSedes
{
    public async Task<IReadOnlySet<Guid>> ExistentesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelacion)
    {
        var lista = ids.Distinct().ToList();
        // Filtro global: una sede de otra organización no aparece.
        return (await contexto.Set<Sede>().AsNoTracking().Where(s => lista.Contains(s.Id)).Select(s => s.Id).ToListAsync(cancelacion))
            .ToHashSet();
    }
}
