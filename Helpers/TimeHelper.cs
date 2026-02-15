namespace Gimnasio.Helpers;

/// <summary>
/// Proveedor de tiempo consistente para la zona horaria de Ecuador (America/Guayaquil, UTC-5).
/// Garantiza que las fechas sean correctas independientemente de la zona horaria del servidor.
/// </summary>
internal static class TimeHelper
{
    private static readonly TimeZoneInfo EcuadorTz = TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");

    /// <summary>Fecha y hora actual en zona horaria de Ecuador</summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EcuadorTz);
}
