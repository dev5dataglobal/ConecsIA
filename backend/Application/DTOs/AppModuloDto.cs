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
    /// <summary>Acciones concedidas (en /mios: las del usuario; en /roles/{id}/modulos: las del rol). En el catálogo, false.</summary>
    public bool Consultar { get; set; }
    public bool Anadir { get; set; }
    public bool Editar { get; set; }
}

/// <summary>Acciones de un rol sobre un módulo. Ninguna implica a otra.</summary>
public sealed class AsignacionModuloDto
{
    [Range(1, int.MaxValue)] public int IdAppModulo { get; set; }
    public bool Consultar { get; set; }
    public bool Anadir { get; set; }
    public bool Editar { get; set; }
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

public sealed class ReemplazarModulosRolDto : IValidatableObject
{
    /// <summary>Matriz de acciones por módulo. Un módulo sin acciones marcadas se retira del rol.</summary>
    public List<AsignacionModuloDto>? Asignaciones { get; set; }

    /// <summary>Compatibilidad: lista de módulos con las tres acciones. Se ignora si llega <see cref="Asignaciones"/>.</summary>
    public List<int>? IdsModulos { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Asignaciones is null && IdsModulos is null)
            yield return new ValidationResult("Debe enviar la lista de módulos (puede estar vacía).", [nameof(Asignaciones)]);
    }
}
