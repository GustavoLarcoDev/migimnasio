#nullable enable
using Microsoft.AspNetCore.Http;

namespace Gimnasio.Helpers;

public static class ImageUploadHelper
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public static (bool valid, string? error, string? dataUri) Process(IFormFile? imagen, int maxMb = 2)
    {
        if (imagen == null || imagen.Length == 0)
            return (false, "No se ha proporcionado ninguna imagen.", null);

        if (imagen.Length > maxMb * 1024 * 1024)
            return (false, $"La imagen no debe superar los {maxMb}MB.", null);

        var ext = Path.GetExtension(imagen.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return (false, "Formato de imagen no permitido. Use JPG, PNG o WebP.", null);

        var mimeType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        using var ms = new MemoryStream();
        imagen.CopyTo(ms);
        var dataUri = $"data:{mimeType};base64,{Convert.ToBase64String(ms.ToArray())}";
        return (true, null, dataUri);
    }

    public static async Task<(bool valid, string? error, string? dataUri)> ProcessAsync(IFormFile? imagen, int maxMb = 2)
    {
        if (imagen == null || imagen.Length == 0)
            return (false, "No se ha proporcionado ninguna imagen.", null);

        if (imagen.Length > maxMb * 1024 * 1024)
            return (false, $"La imagen no debe superar los {maxMb}MB.", null);

        var ext = Path.GetExtension(imagen.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return (false, "Formato de imagen no permitido. Use JPG, PNG o WebP.", null);

        var mimeType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        using var ms = new MemoryStream();
        await imagen.CopyToAsync(ms);
        var dataUri = $"data:{mimeType};base64,{Convert.ToBase64String(ms.ToArray())}";
        return (true, null, dataUri);
    }
}
