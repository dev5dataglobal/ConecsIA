using System.ComponentModel.DataAnnotations;

namespace ConecsIA.Application.DTOs;

/// <summary>Solicitud de información desde la página pública (anónima).</summary>
public sealed class SolicitudContactoEntrada
{
    /// <summary>Tipos ofrecidos por el formulario público; cualquier otro valor se rechaza.</summary>
    public static readonly string[] TiposOrganizacion =
    [
        "Empresa o centro empresarial", "Conjunto residencial", "Club", "Universidad o institución educativa",
        "Coworking", "Parque industrial", "Otra",
    ];

    [Required(ErrorMessage = "El nombre de la organización es obligatorio."), MinLength(2, ErrorMessage = "El nombre de la organización es obligatorio."), MaxLength(150)]
    public string Organizacion { get; set; } = string.Empty;
    [Required(ErrorMessage = "El nombre de contacto es obligatorio."), MinLength(2, ErrorMessage = "El nombre de contacto es obligatorio."), MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;
    [Required(ErrorMessage = "El correo es obligatorio."), EmailAddress(ErrorMessage = "El correo no es válido."), MaxLength(200)]
    public string Correo { get; set; } = string.Empty;
    [RegularExpression(@"^\+?[\d\s()-]{7,20}$", ErrorMessage = "El teléfono no es válido."), MaxLength(20)]
    public string? Telefono { get; set; }
    [Required(ErrorMessage = "El tipo de organización es obligatorio."), MaxLength(60)]
    public string TipoOrganizacion { get; set; } = string.Empty;
    [Range(1, 9999, ErrorMessage = "El número de sedes debe estar entre 1 y 9999.")]
    public int? NumeroSedes { get; set; }
    [Required(ErrorMessage = "El mensaje es obligatorio."), MinLength(10, ErrorMessage = "El mensaje debe tener al menos 10 caracteres."), MaxLength(2000)]
    public string Mensaje { get; set; } = string.Empty;
    /// <summary>Campo trampa, oculto para personas: si llega con valor la solicitud se descarta.</summary>
    [MaxLength(200)]
    public string? SitioWeb { get; set; }
}
