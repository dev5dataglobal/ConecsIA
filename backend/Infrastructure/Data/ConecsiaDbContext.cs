using System.Reflection;
using System.Text.RegularExpressions;
using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Infrastructure.Data;

public sealed class ConecsiaDbContext(DbContextOptions<ConecsiaDbContext> options) : DbContext(options)
{
    public DbSet<Organizacion> Organizaciones => Set<Organizacion>();
    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<Sede> Sedes => Set<Sede>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Zona> Zonas => Set<Zona>();
    public DbSet<Autorizacion> Autorizaciones => Set<Autorizacion>();
    public DbSet<Visita> Visitas => Set<Visita>();
    public DbSet<MovimientoAcceso> MovimientosAcceso => Set<MovimientoAcceso>();
    public DbSet<Permanencia> Permanencias => Set<Permanencia>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("public");
        foreach (var clr in typeof(Organizacion).Assembly.GetTypes().Where(x => x is { IsClass: true, IsAbstract: false } && x.Namespace == typeof(Organizacion).Namespace))
            modelBuilder.Entity(clr);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToArray())
        {
            var clr = entityType.ClrType;
            entityType.SetTableName(ToSnakeCase(clr.Name));
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
                if (property.ClrType == typeof(string)) property.SetMaxLength(2000);
            }

            var key = clr.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name.StartsWith("Id", StringComparison.Ordinal) && p.Name != "IdOrganizacion")
                .OrderBy(p => p.Name == "IdOrganizacion" ? 1 : 0).FirstOrDefault();
            if (key is not null) entityType.SetPrimaryKey(new[] { entityType.FindProperty(key.Name)! });
        }

        ConfigureCompositeKeys(modelBuilder);
        modelBuilder.Entity<Persona>().HasIndex(x => new { x.IdOrganizacion, x.IdTipoDocumento, x.NumeroDocumentoNormalizado }).IsUnique();
        modelBuilder.Entity<Usuario>().HasIndex(x => x.CorreoNormalizado).IsUnique();
        modelBuilder.Entity<Permiso>().HasIndex(x => x.Codigo).IsUnique();
        modelBuilder.Entity<Vehiculo>().HasIndex(x => new { x.IdOrganizacion, x.PlacaNormalizada }).IsUnique();
        modelBuilder.Entity<TokenQr>().HasIndex(x => x.HashSha256).IsUnique();
        modelBuilder.Entity<Alerta>().HasIndex(x => new { x.IdOrganizacion, x.ClaveDeduplicacion }).IsUnique();
        modelBuilder.Entity<SolicitudIdempotente>().HasKey(x => new { x.IdOrganizacion, x.ClaveIdempotencia });
        modelBuilder.Entity<Zona>().ToTable("zona", t => t.HasCheckConstraint("ck_zona_modo_acceso", "es_punto_acceso OR modo_acceso IS NULL OR modo_acceso IN ('Entrada','Salida','Ambos')"));
        modelBuilder.Entity<AutorizacionHorario>().ToTable("autorizacion_horario", t => t.HasCheckConstraint("ck_autorizacion_horario_dia", "dia_semana BETWEEN 1 AND 7"));
        modelBuilder.Entity<Autorizacion>().Property(x => x.FechaInicio).HasColumnType("date");
        modelBuilder.Entity<Autorizacion>().Property(x => x.FechaFin).HasColumnType("date");
        modelBuilder.Entity<Autorizacion>().Property(x => x.Version).IsConcurrencyToken();
        modelBuilder.Entity<Visita>().Property(x => x.Version).IsConcurrencyToken();
        ConfigureRelationships(modelBuilder);
    }

    private static void ConfigureCompositeKeys(ModelBuilder b)
    {
        b.Entity<Organizacion>().HasKey(x => x.IdOrganizacion);
        b.Entity<ParametroOrganizacion>().HasKey(x => new { x.IdOrganizacion, x.Clave });
        b.Entity<Persona>().HasKey(x => x.IdPersona);
        b.Entity<Sede>().HasKey(x => x.IdSede);
        b.Entity<Area>().HasKey(x => x.IdArea);
        b.Entity<Zona>().HasKey(x => x.IdZona);
        b.Entity<Autorizacion>().HasKey(x => x.IdAutorizacion);
        b.Entity<Visita>().HasKey(x => x.IdVisita);
        b.Entity<Permanencia>().HasKey(x => x.IdPermanencia);
        b.Entity<MovimientoAcceso>().HasKey(x => x.IdMovimiento);
        b.Entity<Usuario>().HasKey(x => x.IdUsuario);
        b.Entity<Alerta>().HasKey(x => x.IdAlerta);
        b.Entity<Vehiculo>().HasKey(x => x.IdVehiculo);
        b.Entity<TokenQr>().HasKey(x => x.IdToken);
        b.Entity<SolicitudIdempotente>().HasKey(x => new { x.IdOrganizacion, x.ClaveIdempotencia });
        b.Entity<PlanModulo>().HasKey(x => new { x.IdPlan, x.IdModulo });
        b.Entity<PlantillaRolPermiso>().HasKey(x => new { x.IdPlantillaRol, x.IdPermiso });
        b.Entity<OrganizacionModulo>().HasKey(x => new { x.IdOrganizacion, x.IdModulo });
        b.Entity<AreaResponsable>().HasKey(x => new { x.IdOrganizacion, x.IdArea, x.IdMembresia });
        b.Entity<RolPermiso>().HasKey(x => new { x.IdOrganizacion, x.IdRol, x.IdPermiso });
        b.Entity<MembresiaRol>().HasKey(x => new { x.IdOrganizacion, x.IdMembresia, x.IdRol });
        b.Entity<MembresiaSede>().HasKey(x => new { x.IdOrganizacion, x.IdMembresia, x.IdSede });
        b.Entity<MembresiaPuntoAcceso>().HasKey(x => new { x.IdOrganizacion, x.IdMembresia, x.IdZona });
        b.Entity<PersonaVehiculo>().HasKey(x => new { x.IdOrganizacion, x.IdPersona, x.IdVehiculo });
        b.Entity<AutorizacionZona>().HasKey(x => new { x.IdOrganizacion, x.IdAutorizacion, x.IdZona });
        b.Entity<AutorizacionVehiculo>().HasKey(x => new { x.IdOrganizacion, x.IdAutorizacion, x.IdVehiculo });
        b.Entity<AutorizacionDispositivo>().HasKey(x => new { x.IdOrganizacion, x.IdAutorizacion, x.IdDispositivo });
    }

    private static void ConfigureRelationships(ModelBuilder b)
    {
        b.Entity<Sede>().HasIndex(x => new { x.IdOrganizacion, x.IdSede }).IsUnique();
        b.Entity<Area>().HasIndex(x => new { x.IdOrganizacion, x.IdArea }).IsUnique();
        b.Entity<Zona>().HasIndex(x => new { x.IdOrganizacion, x.IdZona }).IsUnique();
        b.Entity<Persona>().HasIndex(x => new { x.IdOrganizacion, x.IdPersona }).IsUnique();
        b.Entity<TipoPersona>().HasIndex(x => new { x.IdOrganizacion, x.IdTipoPersona }).IsUnique();
        b.Entity<Autorizacion>().HasIndex(x => new { x.IdOrganizacion, x.IdAutorizacion }).IsUnique();
        b.Entity<Visita>().HasIndex(x => new { x.IdOrganizacion, x.IdVisita }).IsUnique();

        b.Entity<Sede>().HasOne<Ciudad>().WithMany().HasForeignKey(x => x.IdCiudad).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Area>().HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Zona>().HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Persona>().HasOne<TipoPersona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdTipoPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdTipoPersona }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Persona>().HasOne<TipoDocumento>().WithMany().HasForeignKey(x => x.IdTipoDocumento).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Autorizacion>().HasOne<Persona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdPersona }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Autorizacion>().HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Autorizacion>().HasOne<Area>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdArea }).HasForeignKey(x => new { x.IdOrganizacion, x.IdArea }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Visita>().HasOne<Persona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdPersona }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Visita>().HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Visita>().HasOne<Area>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdArea }).HasForeignKey(x => new { x.IdOrganizacion, x.IdArea }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MovimientoAcceso>().HasOne<Persona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdPersona }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MovimientoAcceso>().HasOne<Zona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdZona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdZonaPuntoAcceso }).OnDelete(DeleteBehavior.Restrict);
    }

    private static string ToSnakeCase(string value) => Regex.Replace(value, "(?<!^)([A-Z])", "_$1").ToLowerInvariant();
}
