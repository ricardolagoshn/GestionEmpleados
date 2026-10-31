using GestionEmpleados.Application.Common.Exceptions;

namespace GestionEmpleados.Application.Utilities;

public sealed record ImagenDecodificada(byte[] Contenido, string TipoContenido);

/// <summary>
/// Decodifica una fotografía en Base64 o Data URL y valida tamaño, MIME y firma binaria real.
/// </summary>
public static class Base64ImageConverter
{
    public const int MaxBytes = 5 * 1024 * 1024;

    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";
    public const string WebP = "image/webp";

    public static IReadOnlyList<string> TiposPermitidos { get; } = [Jpeg, Png, WebP];

    private const string Campo = "fotografiaBase64";

    public static ImagenDecodificada Decode(string value, string? tipoContenidoDeclarado, int maxBytes = MaxBytes)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidacionException(Campo, "La fotografía está vacía.");
        }

        var base64 = value.Trim();
        string? tipoDataUrl = null;

        if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = base64.IndexOf(',', StringComparison.Ordinal);
            if (commaIndex < 0)
            {
                throw new ValidacionException(Campo, "La fotografía Data URL no es válida.");
            }

            var partes = base64[5..commaIndex].Split(';');
            if (!partes.Skip(1).Any(p => p.Trim().Equals("base64", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ValidacionException(Campo, "La Data URL debe estar codificada en Base64.");
            }

            tipoDataUrl = NormalizarTipo(partes[0]);
            base64 = base64[(commaIndex + 1)..];
        }

        if (base64.AsSpan().IndexOfAny(" \t\r\n") >= 0)
        {
            base64 = string.Concat(base64.Where(c => !char.IsWhiteSpace(c)));
        }

        if (base64.Length == 0)
        {
            throw new ValidacionException(Campo, "La fotografía está vacía.");
        }

        // Evita decodificar cadenas evidentemente mayores al límite.
        var tamañoEstimado = (long)base64.Length / 4 * 3 - 2;
        if (tamañoEstimado > maxBytes)
        {
            throw new ArchivoDemasiadoGrandeException(MensajeTamaño(maxBytes));
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            throw new ValidacionException(Campo, "La fotografía no contiene Base64 válido.");
        }

        if (bytes.Length > maxBytes)
        {
            throw new ArchivoDemasiadoGrandeException(MensajeTamaño(maxBytes));
        }

        var tipoDeclarado = NormalizarTipo(tipoContenidoDeclarado);
        if (tipoDataUrl is not null && tipoDeclarado is not null && tipoDataUrl != tipoDeclarado)
        {
            throw new ValidacionException(Campo, "El MIME de la Data URL no coincide con fotografiaTipoContenido.");
        }

        var tipoIndicado = tipoDataUrl ?? tipoDeclarado;
        if (tipoIndicado is not null && !TiposPermitidos.Contains(tipoIndicado))
        {
            throw new TipoArchivoNoPermitidoException("Solo se permiten imágenes JPEG, PNG o WebP.");
        }

        var tipoDetectado = DetectarTipo(bytes)
            ?? throw new TipoArchivoNoPermitidoException("El contenido no es una imagen JPEG, PNG o WebP.");

        if (tipoIndicado is not null && tipoIndicado != tipoDetectado)
        {
            throw new ValidacionException(Campo, "El contenido de la fotografía no coincide con el tipo MIME indicado.");
        }

        return new ImagenDecodificada(bytes, tipoDetectado);
    }

    /// <summary>Conserva solo el nombre del archivo, sin rutas, con un máximo de 255 caracteres.</summary>
    public static string? NormalizarNombreArchivo(string? nombreArchivo)
    {
        if (string.IsNullOrWhiteSpace(nombreArchivo))
        {
            return null;
        }

        var nombre = Path.GetFileName(nombreArchivo.Trim().Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return null;
        }

        return nombre.Length <= 255 ? nombre : nombre[..255];
    }

    private static string? DetectarTipo(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> jpeg = [0xFF, 0xD8, 0xFF];
        ReadOnlySpan<byte> png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        if (bytes.StartsWith(jpeg))
        {
            return Jpeg;
        }

        if (bytes.StartsWith(png))
        {
            return Png;
        }

        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8))
        {
            return WebP;
        }

        return null;
    }

    private static string? NormalizarTipo(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
        {
            return null;
        }

        var normalizado = tipo.Trim().ToUpperInvariant() switch
        {
            "IMAGE/JPEG" or "IMAGE/JPG" or "IMAGE/PJPEG" => Jpeg,
            "IMAGE/PNG" => Png,
            "IMAGE/WEBP" => WebP,
            _ => tipo.Trim()
        };

        return normalizado;
    }

    private static string MensajeTamaño(int maxBytes) =>
        $"La fotografía no puede superar {maxBytes / (1024 * 1024.0):0.##} MB.";
}
