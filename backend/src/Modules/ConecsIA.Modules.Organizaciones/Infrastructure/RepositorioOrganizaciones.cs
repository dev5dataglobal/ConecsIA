using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Organizaciones.Application;
using ConecsIA.Modules.Organizaciones.Domain;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Modules.Organizaciones.Infrastructure;

internal sealed class RepositorioOrganizaciones(ConecsIADbContext contexto) : IRepositorioOrganizaciones
{
    private DbSet<Organizacion> Organizaciones => contexto.Set<Organizacion>();

    public Task<Organizacion?> ObtenerAsync(Guid id, CancellationToken cancelacion) =>
        Organizaciones.SingleOrDefaultAsync(o => o.Id == id, cancelacion);

    public Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? excluir, CancellationToken cancelacion) =>
        Organizaciones.AnyAsync(o => o.NombreNormalizado == nombreNormalizado && o.Id != excluir, cancelacion);

    public async Task<(IReadOnlyList<Organizacion> Elementos, int Total)> ListarAsync(
        EstadoOrganizacion? estado, int pagina, int tamano, CancellationToken cancelacion)
    {
        var consulta = Organizaciones.AsNoTracking();
        if (estado is not null) consulta = consulta.Where(o => o.Estado == estado);

        var total = await consulta.CountAsync(cancelacion);
        var elementos = await consulta
            .OrderBy(o => o.NombreNormalizado)
            .ThenBy(o => o.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancelacion);
        return (elementos, total);
    }

    public void Agregar(Organizacion organizacion) => Organizaciones.Add(organizacion);

    public void EsperarVersion(Organizacion organizacion, uint version) =>
        contexto.Entry(organizacion).Property(o => o.Version).OriginalValue = version;
}
