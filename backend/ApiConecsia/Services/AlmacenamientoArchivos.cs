using System.Security.Cryptography;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Services;

/// <summary>
/// Almacenamiento privado de archivos en disco local (ADR-013, implementación provisional): fuera de la carpeta
/// pública, con nombre opaco, metadatos y hash en la tabla <c>archivo</c>. Ruta: <c>Almacenamiento:RutaLocal</c>
/// (por defecto <c>App_Data/archivos</c> bajo la raíz del contenido).
/// </summary>
public sealed class AlmacenamientoArchivos(ConecsiaDbContext db, IConfiguration configuration, IWebHostEnvironment environment)
{
    public const long TamanoMaximoImagen = 2 * 1024 * 1024;

    private string Raiz => Path.GetFullPath(configuration["Almacenamiento:RutaLocal"] is { Length: > 0 } ruta
        ? ruta
        : Path.Combine(environment.ContentRootPath, "App_Data", "archivos"));

    /// <summary>Detecta PNG, JPEG o WebP por su firma (no por la extensión ni el Content-Type del cliente).</summary>
    public static string? TipoImagen(ReadOnlySpan<byte> cabecera)
    {
        if (cabecera.Length >= 8 && cabecera[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return "image/png";
        if (cabecera.Length >= 3 && cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF) return "image/jpeg";
        if (cabecera.Length >= 12 && cabecera[..4].SequenceEqual("RIFF"u8) && cabecera[8..12].SequenceEqual("WEBP"u8)) return "image/webp";
        return null;
    }

    /// <summary>Guarda el contenido y agrega la fila <see cref="Archivo"/> al contexto (la persiste quien llama).</summary>
    public async Task<Archivo> GuardarAsync(byte[] contenido, string tipoContenido, int? idOrganizacion, int idUsuario, CancellationToken ct)
    {
        var archivo = new Archivo
        {
            IdArchivo = Guid.NewGuid(),
            IdOrganizacion = idOrganizacion,
            TipoContenido = tipoContenido,
            TamanoBytes = contenido.LongLength,
            HashSha256 = Convert.ToHexStringLower(SHA256.HashData(contenido)),
            IdUsuarioCarga = idUsuario,
            CreadoEn = DateTime.UtcNow,
        };
        archivo.ClaveAlmacenamiento = Path.Combine(idOrganizacion?.ToString() ?? "plataforma", archivo.IdArchivo.ToString("N"));
        var destino = RutaSegura(archivo.ClaveAlmacenamiento);
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        await File.WriteAllBytesAsync(destino, contenido, ct);
        db.Archivos.Add(archivo);
        return archivo;
    }

    public async Task<(Archivo Archivo, Stream Contenido)?> AbrirAsync(Guid idArchivo, CancellationToken ct)
    {
        var archivo = await db.Archivos.AsNoTracking().SingleOrDefaultAsync(x => x.IdArchivo == idArchivo, ct);
        if (archivo is null) return null;
        var ruta = RutaSegura(archivo.ClaveAlmacenamiento);
        return File.Exists(ruta) ? (archivo, File.OpenRead(ruta)) : null;
    }

    private string RutaSegura(string clave)
    {
        var ruta = Path.GetFullPath(Path.Combine(Raiz, clave));
        if (!ruta.StartsWith(Raiz, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Clave de almacenamiento no válida.");
        return ruta;
    }
}
