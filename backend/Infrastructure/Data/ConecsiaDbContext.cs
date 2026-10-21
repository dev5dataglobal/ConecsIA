using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Infrastructure.Data;

public sealed class ConecsiaDbContext(DbContextOptions<ConecsiaDbContext> options) : DbContext(options)
{
    public DbSet<Organizacion> Organizaciones => Set<Organizacion>();
    public DbSet<TipoOrganizacion> TiposOrganizacion => Set<TipoOrganizacion>();
    public DbSet<Plan> Planes => Set<Plan>();
    public DbSet<Modulo> Modulos => Set<Modulo>();
    public DbSet<PlanModulo> PlanesModulos => Set<PlanModulo>();
    public DbSet<OrganizacionModulo> OrganizacionesModulos => Set<OrganizacionModulo>();
    public DbSet<ParametroOrganizacion> ParametrosOrganizacion => Set<ParametroOrganizacion>();
    public DbSet<Ciudad> Ciudades => Set<Ciudad>();
    public DbSet<Sede> Sedes => Set<Sede>();
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConecsiaDbContext).Assembly);
    }
}
