using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ConecsIA.Application.Interfaces;
using System.Linq.Expressions;

namespace ConecsIA.Infrastructure.Data;

public sealed class ConecsiaDbContext(
    DbContextOptions<ConecsiaDbContext> options,
    IOrganizationContext? organizationContext = null) : DbContext(options)
{
    // Se evalúa en cada consulta/guardado: el contexto puede crearse antes de autenticar al usuario
    // (p. ej. en OnTokenValidated), cuando la organización aún no está disponible.
    private int? _organizationId => organizationContext?.OrganizationId;
    public DbSet<Organizacion> Organizaciones => Set<Organizacion>();
    public DbSet<TipoOrganizacion> TiposOrganizacion => Set<TipoOrganizacion>();
    public DbSet<Plan> Planes => Set<Plan>();
    public DbSet<Modulo> Modulos => Set<Modulo>();
    public DbSet<PlanModulo> PlanesModulos => Set<PlanModulo>();
    public DbSet<OrganizacionModulo> OrganizacionesModulos => Set<OrganizacionModulo>();
    public DbSet<ParametroOrganizacion> ParametrosOrganizacion => Set<ParametroOrganizacion>();
    public DbSet<Ciudad> Ciudades => Set<Ciudad>();
    public DbSet<Sede> Sedes => Set<Sede>();
    public DbSet<ZonaEmpresarial> ZonasEmpresariales => Set<ZonaEmpresarial>();
    public DbSet<ZonaEmpresarialOrganizacion> ZonasEmpresarialesOrganizaciones => Set<ZonaEmpresarialOrganizacion>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<AreaResponsable> AreasResponsables => Set<AreaResponsable>();
    public DbSet<Zona> Zonas => Set<Zona>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<MembresiaOrganizacion> MembresiasOrganizacion => Set<MembresiaOrganizacion>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<PlantillaRol> PlantillasRol => Set<PlantillaRol>();
    public DbSet<PlantillaRolPermiso> PlantillasRolPermisos => Set<PlantillaRolPermiso>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<RolPermiso> RolesPermisos => Set<RolPermiso>();
    public DbSet<MembresiaRol> MembresiasRoles => Set<MembresiaRol>();
    public DbSet<MembresiaSede> MembresiasSedes => Set<MembresiaSede>();
    public DbSet<MembresiaPuntoAcceso> MembresiasPuntosAcceso => Set<MembresiaPuntoAcceso>();
    public DbSet<Archivo> Archivos => Set<Archivo>();
    public DbSet<TipoDocumento> TiposDocumento => Set<TipoDocumento>();
    public DbSet<TipoPersona> TiposPersona => Set<TipoPersona>();
    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<PersonaTelefono> PersonasTelefonos => Set<PersonaTelefono>();
    public DbSet<RestriccionAcceso> RestriccionesAcceso => Set<RestriccionAcceso>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<PersonaVehiculo> PersonasVehiculos => Set<PersonaVehiculo>();
    public DbSet<TipoDispositivo> TiposDispositivo => Set<TipoDispositivo>();
    public DbSet<Dispositivo> Dispositivos => Set<Dispositivo>();
    public DbSet<Autorizacion> Autorizaciones => Set<Autorizacion>();
    public DbSet<AutorizacionTransicion> AutorizacionesTransiciones => Set<AutorizacionTransicion>();
    public DbSet<AutorizacionHorario> AutorizacionesHorarios => Set<AutorizacionHorario>();
    public DbSet<AutorizacionZona> AutorizacionesZonas => Set<AutorizacionZona>();
    public DbSet<AutorizacionVehiculo> AutorizacionesVehiculos => Set<AutorizacionVehiculo>();
    public DbSet<AutorizacionDispositivo> AutorizacionesDispositivos => Set<AutorizacionDispositivo>();
    public DbSet<TokenQr> TokensQr => Set<TokenQr>();
    public DbSet<InvitacionEnvio> InvitacionesEnvio => Set<InvitacionEnvio>();
    public DbSet<Visita> Visitas => Set<Visita>();
    public DbSet<Permanencia> Permanencias => Set<Permanencia>();
    public DbSet<MovimientoAcceso> MovimientosAcceso => Set<MovimientoAcceso>();
    public DbSet<MovimientoVehiculo> MovimientosVehiculo => Set<MovimientoVehiculo>();
    public DbSet<MovimientoDispositivo> MovimientosDispositivo => Set<MovimientoDispositivo>();
    public DbSet<SolicitudIdempotente> SolicitudesIdempotentes => Set<SolicitudIdempotente>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<AlertaTransicion> AlertasTransiciones => Set<AlertaTransicion>();
    public DbSet<PlantillaNotificacion> PlantillasNotificacion => Set<PlantillaNotificacion>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<NotificacionDestinatario> NotificacionesDestinatarios => Set<NotificacionDestinatario>();
    public DbSet<Outbox> Outbox => Set<Outbox>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<AppModulo> AppModulos => Set<AppModulo>();
    public DbSet<ModuloPermiso> ModulosPermisos => Set<ModuloPermiso>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConecsiaDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(x => typeof(OrganizacionEntity).IsAssignableFrom(x.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var property = Expression.Call(
                typeof(EF), nameof(EF.Property), new[] { typeof(int) },
                parameter, Expression.Constant(nameof(OrganizacionEntity.IdOrganizacion)));
            var currentOrganization = Expression.Property(
                Expression.Constant(this), nameof(CurrentOrganizationId));
            var filter = Expression.Equal(property, currentOrganization);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
                Expression.Lambda(filter, parameter));
        }
    }

    public int CurrentOrganizationId => _organizationId ?? -1;

    public override int SaveChanges()
    {
        ValidateOrganizationScope();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ValidateOrganizationScope();
        return base.SaveChangesAsync(cancellationToken);
    }

    private HashSet<int>? _organizacionesPermitidas;

    /// <summary>
    /// Permite, mientras dure el ámbito, escribir filas de las organizaciones indicadas además de la activa.
    /// Solo para operaciones de plataforma ya autorizadas (p. ej. vincular empresas a una zona empresarial).
    /// </summary>
    public IDisposable PermitirEscrituraEn(IEnumerable<int> organizaciones)
    {
        var anterior = _organizacionesPermitidas;
        _organizacionesPermitidas = [.. organizaciones];
        return new Ambito(() => _organizacionesPermitidas = anterior);
    }

    private sealed class Ambito(Action alCerrar) : IDisposable
    {
        public void Dispose() => alCerrar();
    }

    private void ValidateOrganizationScope()
    {
        foreach (var entry in ChangeTracker.Entries<OrganizacionEntity>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            if (_organizacionesPermitidas?.Contains(entry.Entity.IdOrganizacion) == true) continue;
            if (_organizationId is null || entry.Entity.IdOrganizacion != _organizationId.Value)
            {
                throw new UnauthorizedAccessException(
                    "La operación no pertenece a la organización autenticada.");
            }
        }
    }
}
