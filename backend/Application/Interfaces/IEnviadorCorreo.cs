namespace ConecsIA.Application.Interfaces;

/// <summary>Envío de correo transaccional. Si no hay servidor configurado, <see cref="Configurado"/> es false y no se envía nada.</summary>
public interface IEnviadorCorreo
{
    bool Configurado { get; }

    /// <summary>Envía el mensaje; lanza una excepción si el servidor lo rechaza o no está configurado.</summary>
    Task EnviarAsync(string destinatario, string asunto, string cuerpoTexto, string cuerpoHtml, CancellationToken cancellationToken = default);
}
