namespace ConecsIA.Application.Seguridad;

/// <summary>Códigos estables de app_modulos usados para autorizar. No dependen de etiquetas visibles.</summary>
public static class CodigosModulo
{
    public const string Dashboard = "dashboard";
    public const string ControlAccesos = "control_accesos";
    public const string Visitas = "visitas";
    public const string Autorizaciones = "autorizaciones";
    public const string Personas = "personas";
    public const string Vehiculos = "vehiculos";
    public const string Dispositivos = "dispositivos";
    public const string Alertas = "alertas";
    public const string Sedes = "sedes";
    public const string Auditoria = "auditoria";
    public const string Configuracion = "configuracion";
    /// <summary>Módulo contenedor anterior; se inactivó al dividirse en usuarios, perfiles, organizaciones y zonas empresariales.</summary>
    public const string Administracion = "administracion";
    public const string Usuarios = "usuarios";
    public const string Perfiles = "perfiles";
    public const string Organizaciones = "organizaciones";
    public const string ZonasEmpresariales = "zonas_empresariales";
}
