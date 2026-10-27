using System.ComponentModel.DataAnnotations;

namespace ConecsIA.Application.DTOs;

/*
 * Contratos del módulo de Personas. Ninguna entrada lleva la organización: el servidor usa la organización activa
 * validada (IOrganizationContext). Un IdOrganizacion enviado por el cliente se ignora al deserializar.
 */

// ---------- Personas ----------

public sealed class PersonaEntrada
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de persona.")]
    public int IdTipoPersona { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de documento.")]
    public int IdTipoDocumento { get; set; }

    [Required(ErrorMessage = "El número de documento es obligatorio."), MaxLength(30)]
    public string NumeroDocumento { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los nombres son obligatorios."), MaxLength(100)]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios."), MaxLength(100)]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio."), MaxLength(320), EmailAddress(ErrorMessage = "El correo no es válido.")]
    public string Correo { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? EmpresaProcedencia { get; set; }

    [MaxLength(1000)]
    public string? Observacion { get; set; }
}

public sealed class PersonaListaDto
{
    public int IdPersona { get; set; }
    public int IdOrganizacion { get; set; }
    public int IdTipoPersona { get; set; }
    public string? TipoPersona { get; set; }
    public int IdTipoDocumento { get; set; }
    public string? TipoDocumento { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? EmpresaProcedencia { get; set; }
    public string? Observacion { get; set; }
    public string? TelefonoPrincipal { get; set; }
    public bool TieneFoto { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEn { get; set; }
}

public sealed class PersonaDetalleDto
{
    public int IdPersona { get; set; }
    public int IdOrganizacion { get; set; }
    public int IdTipoPersona { get; set; }
    public string? TipoPersona { get; set; }
    public int IdTipoDocumento { get; set; }
    public string? TipoDocumento { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? EmpresaProcedencia { get; set; }
    public string? Observacion { get; set; }
    public bool TieneFoto { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEn { get; set; }
    /// <summary>Correo del usuario que registró a la persona (tomado de la identidad autenticada al crearla; no editable).</summary>
    public string? RegistradoPor { get; set; }
    public List<TelefonoPersonaDto> Telefonos { get; set; } = [];
    public List<VehiculoPersonaDto> Vehiculos { get; set; } = [];
}

// ---------- Teléfonos ----------

public sealed class TelefonoEntrada
{
    [Required(ErrorMessage = "El indicativo es obligatorio."), RegularExpression(@"^\+[1-9][0-9]{0,3}$", ErrorMessage = "El indicativo debe tener la forma +57.")]
    public string CodigoPais { get; set; } = "+57";

    [Required(ErrorMessage = "El número es obligatorio."), MaxLength(30)]
    public string Numero { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de teléfono es obligatorio.")]
    public string Tipo { get; set; } = string.Empty;

    public bool Principal { get; set; }
}

public sealed class TelefonoPersonaDto
{
    public int IdTelefono { get; set; }
    public int IdPersona { get; set; }
    public string CodigoPais { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public bool Principal { get; set; }
    public bool Activo { get; set; }
}

// ---------- Vehículos ----------

public sealed class VehiculoPersonaEntrada
{
    [Required(ErrorMessage = "La placa es obligatoria."), MaxLength(15)]
    public string Placa { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de vehículo es obligatorio.")]
    public string Tipo { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? Marca { get; set; }

    [MaxLength(60)]
    public string? Modelo { get; set; }

    [MaxLength(40)]
    public string? Color { get; set; }

    /// <summary>Propietario | Responsable | Conductor. Por defecto Responsable.</summary>
    public string? Relacion { get; set; }
}

public sealed class VehiculoPersonaDto
{
    public int IdVehiculo { get; set; }
    public int IdPersona { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Color { get; set; }
    public string Relacion { get; set; } = string.Empty;
    /// <summary>Estado del vínculo con esta persona (inactivarlo no inactiva el vehículo).</summary>
    public bool Activo { get; set; }
    /// <summary>Otras personas activas asociadas al mismo vehículo.</summary>
    public int OtrasPersonas { get; set; }
}

// ---------- Catálogos ----------

public sealed class TipoPersonaEntrada
{
    [Required(ErrorMessage = "El nombre es obligatorio."), MaxLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descripcion { get; set; }
}

public sealed class TipoDocumentoEntrada
{
    [Required(ErrorMessage = "El código es obligatorio."), MaxLength(10), RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "El código solo admite letras, números y guion bajo.")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio."), MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? PaisEmisor { get; set; }

    [MaxLength(200)]
    public string? PatronValidacion { get; set; }
}

public sealed class TipoDocumentoAdminDto
{
    public int IdTipoDocumento { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? PaisEmisor { get; set; }
    public string? PatronValidacion { get; set; }
    public bool Activo { get; set; }
}

public sealed class CatalogoTipoDocumentoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? PatronValidacion { get; set; }
}

public sealed class CatalogosPersonaDto
{
    public List<OpcionCatalogoDto> TiposPersona { get; set; } = [];
    public List<CatalogoTipoDocumentoDto> TiposDocumento { get; set; } = [];
    public IReadOnlyList<string> TiposTelefono { get; set; } = [];
    public IReadOnlyList<string> TiposVehiculo { get; set; } = [];
    public IReadOnlyList<string> RelacionesVehiculo { get; set; } = [];
}
