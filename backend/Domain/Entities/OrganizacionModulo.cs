namespace ConecsIA.Domain.Entities;

public class OrganizacionModulo : OrganizacionEntity { public int IdModulo { get; set; } public bool Habilitado { get; set; } public DateTime CambiadoEn { get; set; } public int IdUsuarioCambia { get; set; } }

