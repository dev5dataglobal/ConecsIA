using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Identidad.Application;
using ConecsIA.Modules.Identidad.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Modules.Identidad.Infrastructure;

internal sealed class ConfiguracionInvitacion : IEntityTypeConfiguration<Invitacion>
{
    public void Configure(EntityTypeBuilder<Invitacion> builder)
    {
        builder.ToTable("invitacion");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property(i => i.HashToken).HasMaxLength(64).IsRequired();
        builder.Property(i => i.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.Version).IsRowVersion();
        builder.HasOne<Usuario>().WithMany().HasForeignKey(i => i.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Membresia>().WithMany().HasForeignKey(i => i.MembresiaId).OnDelete(DeleteBehavior.Restrict);
        // La FK a organizacion se crea en la migración (otro módulo).
        builder.HasIndex(i => i.HashToken).IsUnique();
        builder.HasIndex(i => new { i.OrganizacionId, i.Estado });
    }
}

internal sealed class RepositorioUsuarios(ConecsIADbContext contexto) : IRepositorioUsuarios
{
    public async Task<IReadOnlyList<(Membresia Membresia, Usuario Usuario)>> DeOrganizacionAsync(
        Guid organizacionId, CancellationToken cancelacion)
    {
        var filas = await contexto.Set<Membresia>().AsNoTracking()
            .Where(m => m.OrganizacionId == organizacionId)
            .Join(contexto.Set<Usuario>().AsNoTracking(), m => m.UsuarioId, u => u.Id, (m, u) => new { m, u })
            .ToListAsync(cancelacion);
        return filas.Select(f => (f.m, f.u)).ToList();
    }

    public async Task<(Membresia Membresia, Usuario Usuario)?> ObtenerParaActualizarAsync(
        Guid organizacionId, Guid usuarioId, CancellationToken cancelacion)
    {
        var membresia = await contexto.Set<Membresia>()
            .SingleOrDefaultAsync(m => m.OrganizacionId == organizacionId && m.UsuarioId == usuarioId, cancelacion);
        if (membresia is null) return null;
        var usuario = await contexto.Set<Usuario>().SingleAsync(u => u.Id == usuarioId, cancelacion);
        return (membresia, usuario);
    }

    public Task<Usuario?> UsuarioPorCorreoParaActualizarAsync(string correoNormalizado, CancellationToken cancelacion) =>
        contexto.Set<Usuario>().SingleOrDefaultAsync(u => u.Correo == correoNormalizado, cancelacion);

    public async Task<IReadOnlyList<Rol>> RolesDeLaOrganizacionAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelacion)
    {
        var lista = ids.Distinct().ToList();
        // Filtro global: solo roles de la organización del contexto; uno ajeno no aparece (404).
        return await contexto.Set<Rol>().AsNoTracking().Where(r => lista.Contains(r.Id)).ToListAsync(cancelacion);
    }

    public Task<Invitacion?> InvitacionPorHashAsync(string hash, CancellationToken cancelacion) =>
        // Excepción revisada al filtro global (HU-005): la aceptación es anónima y el hash identifica
        // una sola invitación; la organización sale de ella y se fija en el contexto antes de escribir.
        contexto.Set<Invitacion>().IgnoreQueryFilters().SingleOrDefaultAsync(i => i.HashToken == hash, cancelacion);

    public async Task<(Membresia Membresia, Usuario Usuario)> MembresiaDeInvitacionAsync(Invitacion invitacion, CancellationToken cancelacion)
    {
        var membresia = await contexto.Set<Membresia>().SingleAsync(m => m.Id == invitacion.MembresiaId, cancelacion);
        var usuario = await contexto.Set<Usuario>().SingleAsync(u => u.Id == invitacion.UsuarioId, cancelacion);
        return (membresia, usuario);
    }

    public void Agregar(object entidad) => contexto.Add(entidad);
}
