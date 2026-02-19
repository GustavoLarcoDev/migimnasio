// ═══════════════════════════════════════════════════════════
// ErrorViewModel.cs — Modelo de vista para la página de error
//
// ASP.NET Core genera este archivo automáticamente al crear un proyecto MVC.
// Se usa en Views/Shared/Error.cshtml para mostrar información de diagnóstico
// cuando ocurre un error no controlado en la aplicación.
//
// En producción, RequestId es útil para correlacionar el error con los logs
// del servidor y encontrar exactamente qué falló.
// ═══════════════════════════════════════════════════════════

namespace Gimnasio.Models;

/// <summary>
/// Modelo de vista para la página de error genérica de la aplicación.
/// ASP.NET Core lo usa automáticamente cuando ocurre un error no manejado.
///
/// En desarrollo, la página de error muestra detalles completos del error.
/// En producción, muestra solo esta vista amigable con el RequestId para diagnóstico.
/// </summary>
public class ErrorViewModel
{
    /// <summary>
    /// ID único de la solicitud HTTP que causó el error.
    /// Es generado automáticamente por ASP.NET Core para cada petición.
    /// Ejemplo: "0HMVFE0O2HDOR:00000001".
    ///
    /// Este ID permite al desarrollador buscar en los logs del servidor
    /// exactamente qué petición causó el error y ver el stack trace completo.
    /// Se muestra en la página de error solo cuando ShowRequestId es true.
    /// </summary>
    public string RequestId { get; set; }

    /// <summary>
    /// Propiedad calculada que indica si se debe mostrar el RequestId en la vista.
    /// Devuelve true solo si RequestId tiene un valor (no es nulo ni cadena vacía).
    /// Se usa en Views/Shared/Error.cshtml para mostrar u ocultar el ID condicionalmente.
    /// Ejemplo de uso en la vista: @if (Model.ShowRequestId) { mostrar RequestId }
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
