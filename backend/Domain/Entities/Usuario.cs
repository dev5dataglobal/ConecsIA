namespace ConecsIA.Domain.Entities;

public class Usuario { public int IdUsuario { get; set; } public string Correo { get; set; } = ""; public string CorreoNormalizado { get; set; } = ""; public string PasswordHash { get; set; } = ""; public string SelloSeguridad { get; set; } = ""; public int IntentosFallidos { get; set; } public DateTime? BloqueadoHasta { get; set; } public bool Activo { get; set; } = true; public DateTime? UltimoAcceso { get; set; } public DateTime CreadoEn { get; set; } }

