using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace ConecsIA.Api.Observabilidad;

/// <summary>
/// Logs JSON estructurados (ADR-018) con traceId/spanId para correlación. Mensajes, valores de
/// estado y ámbitos pasan por <see cref="RedactorDatosSensibles"/>. De las excepciones solo se
/// registra el tipo y la traza de pila, nunca el mensaje (puede contener datos personales o SQL).
/// </summary>
public sealed class FormateadorJsonRedactado() : ConsoleFormatter(Nombre)
{
    public const string Nombre = "json-redactado";

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var entrada = logEntry;
        var ambitos = scopeProvider;
        var escritor = textWriter;
        var mensaje = entrada.Formatter(entrada.State, entrada.Exception);
        if (string.IsNullOrEmpty(mensaje) && entrada.Exception is null) return;

        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteString("timestamp", DateTimeOffset.UtcNow.ToString("O"));
            json.WriteString("level", entrada.LogLevel.ToString());
            json.WriteString("category", entrada.Category);
            json.WriteNumber("eventId", entrada.EventId.Id);
            json.WriteString("message", RedactorDatosSensibles.Redactar(mensaje));

            var actividad = Activity.Current;
            if (actividad is not null)
            {
                json.WriteString("traceId", actividad.TraceId.ToString());
                json.WriteString("spanId", actividad.SpanId.ToString());
            }

            if (entrada.Exception is not null)
            {
                json.WriteString("exceptionType", entrada.Exception.GetType().FullName);
                json.WriteString("stackTrace", RedactorDatosSensibles.Redactar(entrada.Exception.StackTrace));
            }

            if (entrada.State is IReadOnlyList<KeyValuePair<string, object?>> propiedades)
            {
                json.WriteStartObject("state");
                foreach (var (clave, valor) in propiedades)
                {
                    if (clave == "{OriginalFormat}") continue;
                    json.WriteString(clave, RedactorDatosSensibles.Redactar(Convert.ToString(valor, System.Globalization.CultureInfo.InvariantCulture)));
                }

                json.WriteEndObject();
            }

            if (ambitos is not null)
            {
                json.WriteStartArray("scopes");
                ambitos.ForEachScope(
                    static (ambito, j) => j.WriteStringValue(RedactorDatosSensibles.Redactar(Convert.ToString(ambito, System.Globalization.CultureInfo.InvariantCulture))),
                    json);
                json.WriteEndArray();
            }

            json.WriteEndObject();
        }

        escritor.WriteLine(System.Text.Encoding.UTF8.GetString(buffer.ToArray()));
    }

    /// <summary>Formatea una entrada fuera del pipeline de logging (pruebas).</summary>
    public string Formatear(LogLevel nivel, string categoria, string mensaje, Exception? excepcion = null)
    {
        using var escritor = new StringWriter();
        Write(new LogEntry<string>(nivel, categoria, new EventId(0), mensaje, excepcion, (s, _) => s), scopeProvider: null, escritor);
        return escritor.ToString();
    }
}
