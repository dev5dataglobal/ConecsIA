using System.ComponentModel.DataAnnotations;

namespace ConecsIA.Application.DTOs;

public sealed class UsuarioAdministracionDto
{
    public int IdUsuario { get; set; }
    public int IdMembresia { get; set; }
    public int IdOrganizacion { get; set; }
    public string Organizacion { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public string? Documento { get; set; }
    public IReadOnlyList<string> Perfiles { get; set; } = [];
    public string Estado { get; set; } = string.Empty;
}

public sealed class UsuarioAdministracionEntrada
{
    [Required, EmailAddress, MaxLength(320)] public string Correo { get; set; } = string.Empty;
    [Required, MinLength(8), MaxLength(128)] public string Contrasena { get; set; } = string.Empty;
    public int? IdPersona { get; set; }
    public int? IdOrganizacion { get; set; }
    public IReadOnlyList<int> IdRoles { get; set; } = [];
    public bool Activo { get; set; } = true;
}

public sealed class UsuarioAdministracionEdicion
{
    [Required, EmailAddress, MaxLength(320)] public string Correo { get; set; } = string.Empty;
    public int? IdPersona { get; set; }
    public IReadOnlyList<int> IdRoles { get; set; } = [];
    public bool Activo { get; set; } = true;
}

public sealed class OrganizacionAdministracionEntrada
{
    [Required, MaxLength(200)] public string Nombre { get; set; } = string.Empty;
    [Required] public int IdTipoOrganizacion { get; set; }
    [Required] public int IdPlan { get; set; }
    [MaxLength(200)] public string? Identificacion { get; set; }
    [MaxLength(320), EmailAddress] public string? Correo { get; set; }
    [MaxLength(500)] public string? Direccion { get; set; }
    public string Estado { get; set; } = "Activa";
}

public sealed class PerfilAdministracionEntrada
{
    [Required, MaxLength(120)] public string Nombre { get; set; } = string.Empty;
    [MaxLength(500)] public string? Descripcion { get; set; }
    public IReadOnlyList<int> IdPermisos { get; set; } = [];
    public bool Activo { get; set; } = true;
}

public sealed class PerfilAdministracionDto
{
    public int IdRol { get; set; }
    public int IdOrganizacion { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public IReadOnlyList<PermisoDto> Permisos { get; set; } = [];
}
