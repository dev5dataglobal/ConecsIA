using System.ComponentModel.DataAnnotations;

namespace ConecsIA.Application.DTOs;

public sealed class AppModuloDto
{
    public int IdAppModulo { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Ruta { get; set; }
    public string? Icono { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; }
}

public sealed class AppModuloGuardarDto
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [RegularExpression("^[a-z][a-z0-9_]{1,59}$", ErrorMessage = "El código debe usar minúsculas, números o guion bajo y empezar por una letra.")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 120 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Descripcion { get; set; }

    [StringLength(200)]
    [RegularExpression("^/[a-z0-9/_-]*$", ErrorMessage = "La ruta debe comenzar con '/'.")]
    public string? Ruta { get; set; }

    [StringLength(60)]
    public string? Icono { get; set; }

    [Range(0, 10000)]
    public int Orden { get; set; }
}

public sealed class AppModuloEstadoDto
{
    [Required]
    public bool? Activo { get; set; }
}

public sealed class ReemplazarModulosRolDto
{
    [Required(ErrorMessage = "Debe enviar la lista de módulos (puede estar vacía).")]
    public List<int>? IdsModulos { get; set; }
}
