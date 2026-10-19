namespace ConecsIA.SharedKernel.Seguridad;

/// <summary>
/// Endpoint autenticado que no necesita una organización resuelta (p. ej. listar las membresías del
/// usuario para elegir una). El middleware solo verifica que el usuario exista y esté activo.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public sealed class SinOrganizacionResueltaAttribute : Attribute;
