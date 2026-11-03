using System.Net;
using System.Net.Mail;
using ConecsIA.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace ConecsIA.Infrastructure.Services;

/// <summary>Sección de configuración «Correo». Sin Host ni Remitente el envío se considera no configurado.</summary>
public sealed class CorreoOptions
{
    public string Host { get; set; } = string.Empty;
    public int Puerto { get; set; } = 587;
    public bool UsarSsl { get; set; } = true;
    public string? Usuario { get; set; }
    public string? Contrasena { get; set; }
    public string Remitente { get; set; } = string.Empty;
    public string NombreRemitente { get; set; } = "ConecsIA";
}

public sealed class SmtpEnviadorCorreo(IOptions<CorreoOptions> opciones) : IEnviadorCorreo
{
    private readonly CorreoOptions _opciones = opciones.Value;

    public bool Configurado => !string.IsNullOrWhiteSpace(_opciones.Host) && !string.IsNullOrWhiteSpace(_opciones.Remitente);

    public async Task EnviarAsync(string destinatario, string asunto, string cuerpoTexto, string cuerpoHtml, CancellationToken cancellationToken = default)
    {
        if (!Configurado) throw new InvalidOperationException("El envío de correo no está configurado (sección «Correo»).");

        using var mensaje = new MailMessage
        {
            From = new MailAddress(_opciones.Remitente, _opciones.NombreRemitente),
            Subject = asunto,
            Body = cuerpoTexto,
            IsBodyHtml = false,
        };
        mensaje.To.Add(destinatario);
        mensaje.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(cuerpoHtml, null, "text/html"));

        using var cliente = new SmtpClient(_opciones.Host, _opciones.Puerto) { EnableSsl = _opciones.UsarSsl };
        if (!string.IsNullOrWhiteSpace(_opciones.Usuario))
            cliente.Credentials = new NetworkCredential(_opciones.Usuario, _opciones.Contrasena);
        await cliente.SendMailAsync(mensaje, cancellationToken);
    }
}
