using System.Linq.Expressions;
using System.Reflection;
using ConecsIA.Infrastructure.Seguridad;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Infrastructure.Persistencia;

/// <summary>
/// Contexto único compartido por los módulos (ADR-021, propuesto). Cada módulo aporta sus
/// configuraciones de entidades; así las operaciones que cruzan módulos (p. ej. entrada + consumo
/// de autorización + permanencia) pueden ser una sola transacción local (ADR-007).
/// Aplica el aislamiento por organización de ADR-004: filtro global en lectura y verificación al guardar.
/// </summary>
public sealed class ConecsIADbContext(
    DbContextOptions<ConecsIADbContext> opciones,
    EnsambladosDelModelo ensamblados,
    IContextoSolicitud? contextoSolicitud = null) : DbContext(opciones)
{
    private readonly IContextoSolicitud contexto = contextoSolicitud ?? new ContextoSolicitud();

    // EF Core evalúa estas propiedades por consulta: el filtro usa el contexto de cada petición.
    private bool SinFiltro => contexto.EsSistema;
    private Guid? OrganizacionActual => contexto.OrganizacionId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (Assembly ensamblado in ensamblados.Todos)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(ensamblado);
        }

        foreach (var tipo in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(IEntidadDeOrganizacion).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(tipo.ClrType).HasQueryFilter(FiltroOrganizacion(tipo.ClrType));
        }
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        VerificarOrganizacion();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        VerificarOrganizacion();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    private void VerificarOrganizacion()
    {
        if (contexto.EsSistema) return;

        foreach (var entrada in ChangeTracker.Entries<IEntidadDeOrganizacion>())
        {
            if (entrada.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                && entrada.Entity.OrganizacionId != contexto.OrganizacionId)
            {
                throw new EscrituraEntreOrganizacionesException(entrada.Metadata.ClrType.Name);
            }
        }
    }

    private LambdaExpression FiltroOrganizacion(Type tipo)
    {
        // e => this.SinFiltro || (Guid?)e.OrganizacionId == this.OrganizacionActual
        var entidad = Expression.Parameter(tipo, "e");
        var esteContexto = Expression.Constant(this);
        var organizacion = Expression.Convert(
            Expression.Property(entidad, nameof(IEntidadDeOrganizacion.OrganizacionId)),
            typeof(Guid?));
        var cuerpo = Expression.OrElse(
            Expression.Property(esteContexto, nameof(SinFiltro)),
            Expression.Equal(organizacion, Expression.Property(esteContexto, nameof(OrganizacionActual))));
        return Expression.Lambda(cuerpo, entidad);
    }
}
