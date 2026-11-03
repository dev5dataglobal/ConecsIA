using System.ComponentModel.DataAnnotations;

namespace ConecsIA.Application.DTOs;

// ---------- Usuarios ----------

public sealed class UsuarioAdministracionDto
{
    public int IdUsuario { get; set; }
    public int IdMembresia { get; set; }
    public int IdOrganizacion { get; set; }
    public string Organizacion { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int? IdPersona { get; set; }
    public string? Nombre { get; set; }
    public string? Documento { get; set; }
    public IReadOnlyList<int> IdRoles { get; set; } = [];
    public IReadOnlyList<string> Perfiles { get; set; } = [];
    /// <summary>Activo = cuenta activa y membresía activa en la organización.</summary>
    public bool Activo { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public sealed class UsuarioAdministracionEntrada
{
    [Required(ErrorMessage = "El correo es obligatorio."), EmailAddress(ErrorMessage = "El correo no es válido."), MaxLength(320)]
    public string Correo { get; set; } = string.Empty;
    [Required(ErrorMessage = "La contraseña es obligatoria."), MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres."), MaxLength(128)]
    public string Contrasena { get; set; } = string.Empty;
    public int? IdPersona { get; set; }
    public IReadOnlyList<int> IdRoles { get; set; } = [];
    public bool Activo { get; set; } = true;
}

public sealed class UsuarioAdministracionEdicion
{
    [Required(ErrorMessage = "El correo es obligatorio."), EmailAddress(ErrorMessage = "El correo no es válido."), MaxLength(320)]
    public string Correo { get; set; } = string.Empty;
    public int? IdPersona { get; set; }
    public IReadOnlyList<int> IdRoles { get; set; } = [];
    public bool Activo { get; set; } = true;
}

public sealed class EstadoAdministracionEntrada
{
    [Required(ErrorMessage = "Indique si el registro queda activo.")]
    public bool? Activo { get; set; }
}

// ---------- Organizaciones ----------

public sealed class OrganizacionAdministracionDto
{
    public int IdOrganizacion { get; set; }
    public int IdTipoOrganizacion { get; set; }
    public string? TipoOrganizacion { get; set; }
    public int IdPlan { get; set; }
    public string? Plan { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Identificacion { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime CreadoEn { get; set; }
    public bool TieneLogo { get; set; }
    /// <summary>Zonas empresariales en las que la organización tiene sedes (derivado de las sedes, ADR-028).</summary>
    public IReadOnlyList<string> ZonasEmpresariales { get; set; } = [];
}

public sealed class OrganizacionAdministracionEntrada
{
    [Required(ErrorMessage = "El nombre es obligatorio."), MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de organización.")]
    public int IdTipoOrganizacion { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el plan.")]
    public int IdPlan { get; set; }
    [Required(ErrorMessage = "La identificación es obligatoria."), MaxLength(50)]
    public string Identificacion { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Direccion { get; set; }
    [Required(ErrorMessage = "El teléfono es obligatorio."), MaxLength(30), RegularExpression(@"^\+?[0-9 ()-]{7,30}$", ErrorMessage = "El teléfono solo admite números, espacios, paréntesis, guiones y un + inicial.")]
    public string Telefono { get; set; } = string.Empty;
    [Required(ErrorMessage = "El correo es obligatorio."), MaxLength(320), EmailAddress(ErrorMessage = "El correo no es válido.")]
    public string Correo { get; set; } = string.Empty;
    [RegularExpression("^(Activa|Inactiva)$", ErrorMessage = "El estado debe ser Activa o Inactiva.")]
    public string Estado { get; set; } = "Activa";
}

public sealed class OpcionCatalogoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

// ---------- Perfiles ----------

public sealed class PerfilAdministracionEntrada
{
    [Required(ErrorMessage = "El nombre es obligatorio."), MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Descripcion { get; set; }
    /// <summary>Permisos por código (rol_permiso). null = no se modifican.</summary>
    public IReadOnlyList<int>? IdPermisos { get; set; }
    public bool Activo { get; set; } = true;
}

public sealed class PerfilAdministracionDto
{
    public int IdRol { get; set; }
    public int IdOrganizacion { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public int TotalModulos { get; set; }
    public int TotalUsuarios { get; set; }
    public IReadOnlyList<PermisoDto> Permisos { get; set; } = [];
}

// ---------- Zonas empresariales (catálogo de plataforma; las sedes pertenecen a la zona, ADR-028) ----------

public sealed class SedeZonaDto
{
    public int IdSede { get; set; }
    public string Sede { get; set; } = string.Empty;
    public string Organizacion { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public sealed class ZonaEmpresarialAdministracionDto
{
    public int IdZonaEmpresarial { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activa { get; set; }
    public DateTime CreadaEn { get; set; }
    public int TotalSedes { get; set; }
    /// <summary>Sedes de la zona: todas para la plataforma; solo las propias para las demás organizaciones.</summary>
    public IReadOnlyList<SedeZonaDto> Sedes { get; set; } = [];
}

public sealed class ZonaEmpresarialAdministracionEntrada
{
    [Required(ErrorMessage = "El nombre de la zona es obligatorio."), MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;
    [MaxLength(2000)]
    public string? Descripcion { get; set; }
    public bool Activa { get; set; } = true;
}
