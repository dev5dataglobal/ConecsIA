namespace ConecsIA.Domain.Entities;

/// <summary>
/// Solicitud de información enviada desde la página pública. No pertenece a ninguna organización cliente
/// (no hereda de OrganizacionEntity): su consulta queda reservada a la plataforma.
/// </summary>
public class SolicitudContacto
{
    public long IdSolicitudContacto { get; set; }
    public string Organizacion { get; set; } = "";
    public string NombreContacto { get; set; } = "";
    public string Correo { get; set; } = "";
    public string? Telefono { get; set; }
    public string TipoOrganizacion { get; set; } = "";
    public int? NumeroSedes { get; set; }
    public string Mensaje { get; set; } = "";
    public string Estado { get; set; } = "Nueva";
    public DateTime FechaCreacion { get; set; }
}
