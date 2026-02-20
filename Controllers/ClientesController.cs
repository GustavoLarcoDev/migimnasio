// ═══════════════════════════════════════════════════════════════════════════════
// ClientesController.cs — Controlador de gestión de clientes
//
// Este controlador es el CORAZÓN del sistema para la gestión de membresías.
// Maneja todo lo relacionado con clientes: consultas, creación, edición,
// eliminación, renovación de membresías, clientes diarios,
// e importación/exportación de datos en formato Excel.
//
// IMPORTANTE PARA EL JUNIOR:
//   - Todos los endpoints requieren autenticación (cookie de sesión activa).
//   - CADA endpoint valida que el usuario autenticado sea dueño del negocio
//     que está modificando. Esto evita que un negocio acceda a datos de otro.
//   - La lógica de negocio está en ClienteService, NO aquí. El controlador
//     solo recibe peticiones, valida permisos y delega al servicio.
//   - Se usa el patrón (success, message) en las respuestas del servicio
//     para manejar errores sin lanzar excepciones innecesarias.
// ═══════════════════════════════════════════════════════════════════════════════

using Gimnasio.Models.DTOs;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gimnasio.Data;
using Microsoft.EntityFrameworkCore;
using Gimnasio.Models;

namespace Gimnasio.Controllers;

// Todas las rutas de este controlador empiezan con "/Negocios/..."
// El atributo [Authorize] hace que CUALQUIER petición sin sesión activa
// sea redirigida automáticamente al login. Sin este atributo, cualquiera
// podría ver/modificar datos sin estar autenticado.
[Route("Negocios")]
[Authorize]
public class ClientesController : Controller
{
    // ── Dependencias inyectadas por el constructor ────────────────────────────
    // ASP.NET Core se encarga de crear estas instancias automáticamente
    // (Dependency Injection). Nunca se instancian con "new" aquí.
    private readonly IClienteService _clienteService;
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IReciboService _reciboService;

    public ClientesController(IClienteService clienteService, IAuthService authService, ApplicationDbContext context, IEmailService emailService, IReciboService reciboService)
    {
        _clienteService = clienteService;
        _authService = authService;
        _context = context;
        _emailService = emailService;
        _reciboService = reciboService;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SECCIÓN 1 — VISTA PRINCIPAL DEL DASHBOARD
    //
    // Este es el único endpoint que devuelve una VISTA HTML (no JSON).
    // Todos los demás endpoints son AJAX y devuelven JSON.
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Muestra el dashboard principal del negocio con todas las pestañas:
    /// clientes, logs, ventas y notificaciones.
    ///
    /// Valida que el usuario autenticado sea DUEÑO de este negocio específico.
    /// Redirige a una vista diferente si el negocio es de tipo "artesanal".
    /// </summary>
    /// <param name="id">ID del negocio cuyo dashboard se quiere visualizar (viene en la URL)</param>
    [HttpGet("{id}/Dashboard")]
    public async Task<IActionResult> Dashboard(Guid id)
    {
        try
        {
            // Extraer el NegocioId del claim de la cookie de sesión.
            // Si el usuario no tiene sesión o su negocio no coincide con
            // el ID de la URL, devolvemos 403 Forbidden (no autorizado).
            // Esto impide que un dueño de negocio A vea el dashboard de negocio B.
            var negocioId = _authService.GetNegocioId(User);
            if (!negocioId.HasValue || negocioId.Value != id)
                return Forbid();

            // Cargar el negocio junto con sus clientes en una sola consulta SQL
            // usando .Include() (carga ansiosa / eager loading).
            // Sin el .Include(), la propiedad Clientes llegaría como null.
            var negocio = await _context.Negocios
                .Include(g => g.Clientes)
                .FirstOrDefaultAsync(g => g.NegocioId == id);

            // Si el negocio no existe en la base de datos, devolver 404
            if (negocio == null)
                return NotFound();

            // El sistema soporta dos tipos de dashboard:
            //   - "artesanal": vista especializada para negocios de servicios (citas, etc.)
            //   - (cualquier otro): vista estándar de gimnasio/membresías
            if (negocio.TipoNegocio == "artesanal")
                return View("~/Views/Negocios/DashboardArtesanal.cshtml", negocio);
            if (negocio.TipoNegocio == "tienda")
                return View("~/Views/Negocios/DashboardTienda.cshtml", negocio);
            if (negocio.TipoNegocio == "restaurante")
                return View("~/Views/Negocios/DashboardRestaurante.cshtml", negocio);

            return View("~/Views/Negocios/Dashboard.cshtml", negocio);
        }
        catch
        {
            // En caso de error inesperado, devolvemos 500 genérico.
            // Nota: aquí no se expone el mensaje del error para no filtrar
            // información sensible al cliente en producción.
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SECCIÓN 2 — ENDPOINTS DE CONSULTA (GET / LECTURA)
    //
    // Estos endpoints son llamados por el frontend vía AJAX (fetch/axios).
    // Todos devuelven JSON. Ninguno modifica datos.
    //
    // PATRÓN DE SEGURIDAD REPETIDO EN TODOS:
    //   1. Leer el NegocioId del claim de sesión del usuario autenticado.
    //   2. Comparar con el NegocioId del parámetro de la petición.
    //   3. Si no coinciden → 403 Forbidden.
    // Esto garantiza que cada negocio solo pueda ver SUS propios datos.
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene las estadísticas del dashboard para mostrar en las tarjetas superiores:
    /// - Total de clientes activos (membresía vigente)
    /// - Total de clientes vencidos (membresía expirada)
    /// - Ingresos del mes actual
    /// - Ingresos de hoy
    /// - Lista de clientes próximos a vencer en los próximos 5 días
    /// </summary>
    /// <param name="negocioId">ID del negocio cuyos datos se quieren obtener</param>
    [HttpGet("GetDashboardStats")]
    public async Task<IActionResult> GetDashboardStats(Guid negocioId)
    {
        try
        {
            // Verificar que el usuario sea dueño del negocio solicitado
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            // Delegar el cálculo al servicio. El controlador NO calcula nada,
            // solo orquesta y responde.
            var stats = await _clienteService.GetDashboardStatsAsync(negocioId);
            return Ok(stats);
        }
        catch (Exception)
        {
            // En endpoints AJAX sí devolvemos el mensaje de excepción para
            // facilitar el debugging. En producción podrías querer ocultarlo.
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene la lista completa de clientes del negocio.
    /// Cada cliente incluye campos calculados como días restantes
    /// de membresía y su estado (activo, vencido, próximo a vencer).
    /// Esta lista es la que popula la tabla de clientes en el dashboard.
    /// </summary>
    /// <param name="negocioId">ID del negocio cuyos clientes se quieren listar</param>
    [HttpGet("GetClientes")]
    public async Task<IActionResult> GetClientes(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var clientes = await _clienteService.GetClientesAsync(negocioId);
            return Ok(clientes);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene los datos de UN cliente específico para pre-cargar el formulario
    /// de edición en el modal del dashboard.
    /// Si el cliente no existe o no pertenece al negocio, devuelve 404.
    /// </summary>
    /// <param name="id">ID único del cliente a buscar</param>
    /// <param name="negocioId">ID del negocio al que debe pertenecer el cliente (seguridad)</param>
    [HttpGet("GetCliente")]
    public async Task<IActionResult> GetCliente(Guid id, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var cliente = await _clienteService.GetClienteAsync(id, negocioId);

            // El servicio devuelve null si el cliente no existe o no pertenece
            // al negocio. En ese caso se informa al frontend con un 404.
            if (cliente == null)
                return NotFound(new { success = false, message = "Cliente no encontrado" });

            return Ok(cliente);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene el estado actual de la suscripción del NEGOCIO al sistema MiNegocio.
    /// Esto es diferente a las membresías de los clientes: aquí se consulta
    /// si el dueño del negocio tiene su plan de MiNegocio activo o vencido.
    ///
    /// Devuelve:
    ///   - diasRestantes: cuántos días faltan para que expire el plan
    ///   - fechaExpiracion: fecha exacta de vencimiento
    ///   - fechaPago: fecha en que se realizó el último pago
    ///   - diasPagados: cantidad de días que cubrió el pago
    ///   - precioSuscripcion: monto pagado
    ///   - porEmpezar: true si el plan fue pagado pero aún no comenzó a correr
    /// </summary>
    /// <param name="negocioId">ID del negocio cuya suscripción se quiere consultar</param>
    [HttpGet("GetSuscripcionStatus")]
    public async Task<IActionResult> GetSuscripcionStatus(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            // Buscar el negocio directamente en el contexto de EF.
            // Este endpoint no necesita el servicio porque solo lee datos
            // simples del modelo Negocio, sin lógica de clientes.
            var negocio = await _context.Negocios.FindAsync(negocioId);
            if (negocio == null)
                return NotFound();

            // TimeHelper.Now es un helper centralizado que devuelve la fecha/hora
            // actual (permite ser mockeado en tests fácilmente).
            var now = TimeHelper.Now;

            // Calcular días restantes: si no tiene FechaExpiracion configurada
            // (plan sin vencimiento), diasRestantes será null.
            int? diasRestantes = negocio.FechaExpiracion.HasValue
                ? (int)(negocio.FechaExpiracion.Value.Date - now.Date).TotalDays
                : null;

            // "Por empezar" indica que el dueño ya pagó, pero la fecha de pago
            // es en el futuro (ej: pagó hoy para un plan que empieza mañana).
            // Útil para mostrar un badge especial en la UI.
            bool porEmpezar = negocio.FechaPago.HasValue && negocio.FechaPago.Value.Date > now.Date;

            return Ok(new
            {
                diasRestantes,
                fechaExpiracion = negocio.FechaExpiracion,
                fechaPago = negocio.FechaPago,
                diasPagados = negocio.DiasPagados,
                precioSuscripcion = negocio.PrecioSuscripcion,
                porEmpezar
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene la lista de clientes marcados como "diario" (EsDiario = true).
    /// Un cliente diario es alguien que pagó solo por un día, sin membresía mensual.
    /// Esta lista se usa para que el dueño envíe mensajes de cobro vía WhatsApp.
    /// Después de contactarlos, se llama a LimpiarClientesDiarios para resetear la lista.
    /// </summary>
    /// <param name="negocioId">ID del negocio cuyos clientes diarios se quieren obtener</param>
    [HttpGet("GetClientesDiarios")]
    public async Task<IActionResult> GetClientesDiarios(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var clientes = await _clienteService.GetClientesDiariosAsync(negocioId);
            return Ok(clientes);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SECCIÓN 3 — OPERACIONES CRUD (POST / ESCRITURA)
    //
    // Estos endpoints MODIFICAN datos en la base de datos.
    // Todos usan [HttpPost] porque cambian el estado del sistema.
    //
    // PATRÓN DE RESPUESTA:
    //   - La capa de servicio devuelve una tupla (bool success, string message).
    //   - El controlador interpreta esa tupla y responde con el código HTTP adecuado:
    //       success = true  → 200 OK
    //       success = false → 400 BadRequest o 404 NotFound según el mensaje
    //   - Esto evita usar excepciones para control de flujo normal (ej: "no encontrado").
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo cliente en el sistema y registra automáticamente un log
    /// de pago con el monto abonado.
    ///
    /// El formulario del frontend envía los datos como multipart/form-data
    /// (por eso se usa [FromForm] en vez de [FromBody]).
    /// </summary>
    /// <param name="model">DTO con todos los campos del formulario de nuevo cliente</param>
    [HttpPost("CrearCliente")]
    public async Task<IActionResult> CrearCliente([FromForm] ClienteCreateDto model)
    {
        try
        {
            var negocioId = _authService.GetNegocioId(User);
            if (!negocioId.HasValue || model.NegocioId != negocioId.Value)
                return Forbid();

            // El servicio devuelve una tupla con el resultado de la operación.
            // La destructuración (var (success, message) = ...) es azúcar sintáctica
            // de C# para desempaquetar la tupla directamente.
            var (success, message) = await _clienteService.CrearClienteAsync(model);

            if (!success)
                return BadRequest(new { success = false, message });

            // Enviar recibo al cliente si tiene email registrado y guardar en BD
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var negocio = await _context.Negocios.FindAsync(model.NegocioId);
                if (negocio != null)
                {
                    var concepto = $"Membresía x{model.Dias} días";
                    var nombreCompleto = $"{model.Nombre} {model.Apellido}";
                    try
                    {
                        var numRecibo = await _reciboService.ObtenerSiguienteNumeroAsync(model.NegocioId);
                        var (enviado, html) = await _emailService.EnviarReciboPagoClienteAsync(
                            model.Email, nombreCompleto, negocio.NegocioNombre,
                            concepto, model.Precio, model.Dias,
                            negocio.Email, negocio.Telefono, numRecibo);
                        await _reciboService.CrearReciboAsync(model.NegocioId, numRecibo, "pago_cliente",
                            model.Email, nombreCompleto, negocio.NegocioNombre, concepto, model.Precio, html);
                    }
                    catch { }
                }
            }

            return Ok(new { success = true, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Edita los datos de un cliente existente (nombre, teléfono, fechas, precio, etc.).
    /// El servicio registra en los logs qué campos cambiaron y cuáles eran sus valores
    /// anteriores, generando una auditoría de cambios.
    ///
    /// Reutiliza el mismo DTO que CrearCliente porque los campos del formulario
    /// son los mismos, pero el servicio detecta si tiene un ID existente para editar.
    /// </summary>
    /// <param name="model">DTO con los nuevos datos del cliente (incluye el ClienteId para identificarlo)</param>
    [HttpPost("EditarCliente")]
    public async Task<IActionResult> EditarCliente([FromForm] ClienteCreateDto model)
    {
        try
        {
            var negocioId = _authService.GetNegocioId(User);
            if (!negocioId.HasValue || model.NegocioId != negocioId.Value)
                return Forbid();

            var (success, message) = await _clienteService.EditarClienteAsync(model);

            if (!success)
            {
                // Diferenciar entre "cliente no encontrado" (404) y cualquier
                // otro tipo de error de validación (400), para que el frontend
                // pueda mostrar el mensaje adecuado al usuario.
                if (message.Contains("no encontrado"))
                    return NotFound(new { success = false, message });
                return BadRequest(new { success = false, message });
            }

            return Ok(new { success = true, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Elimina un cliente del sistema de forma permanente.
    /// La operación también registra un log indicando qué cliente fue eliminado
    /// y cuándo, para mantener trazabilidad de acciones.
    ///
    /// Los parámetros llegan en la query string (ej: ?id=xxx&negocioId=yyy)
    /// porque el frontend los envía así desde el botón de eliminar.
    /// </summary>
    /// <param name="id">ID del cliente a eliminar</param>
    /// <param name="negocioId">ID del negocio (para verificar que el cliente pertenece al negocio del usuario)</param>
    [HttpPost("EliminarCliente")]
    public async Task<IActionResult> EliminarCliente(Guid id, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var (success, message) = await _clienteService.EliminarClienteAsync(id, negocioId);

            // Si el servicio no encontró el cliente, devolvemos 404.
            // Usualmente significa que el cliente ya fue eliminado antes (doble click).
            if (!success)
                return NotFound(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SECCIÓN 4 — RENOVACIÓN DE MEMBRESÍAS
    //
    // La renovación extiende la fecha de fin de membresía de un cliente y
    // genera automáticamente un registro de pago en los logs.
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renueva la membresía de un cliente extendiendo su FechaFin a la nueva fecha indicada.
    /// También registra en los logs el pago realizado con el precio correspondiente.
    ///
    /// El dueño del negocio puede definir manualmente la nueva fecha de vencimiento,
    /// lo que permite manejar casos especiales (ej: abonos parciales, quincenas, etc.).
    /// </summary>
    /// <param name="id">ID del cliente cuya membresía se va a renovar</param>
    /// <param name="negocioId">ID del negocio (verificación de pertenencia)</param>
    /// <param name="nuevaFechaFin">Nueva fecha hasta la que estará activa la membresía</param>
    /// <param name="precio">Monto cobrado por la renovación (se guarda en los logs)</param>
    [HttpPost("RenovarCliente")]
    public async Task<IActionResult> RenovarCliente(Guid id, Guid negocioId, DateTime nuevaFechaFin, decimal precio)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            var (success, message) = await _clienteService.RenovarClienteAsync(id, negocioId, nuevaFechaFin, precio);

            if (!success)
            {
                if (message.Contains("no encontrado"))
                    return NotFound(new { success = false, message });
                return BadRequest(new { success = false, message });
            }

            // Enviar recibo de renovación y guardarlo en BD
            try
            {
                var cliente = await _context.Clientes.FindAsync(id);
                var negocio = await _context.Negocios.FindAsync(negocioId);
                if (cliente != null && negocio != null && !string.IsNullOrWhiteSpace(cliente.Email))
                {
                    var dias = (int)(nuevaFechaFin.Date - TimeHelper.Now.Date).TotalDays;
                    if (dias < 1) dias = 1;
                    var concepto = $"Renovación membresía x{dias} días";
                    var nombreCompleto = $"{cliente.Nombre} {cliente.Apellido}";
                    var numRecibo = await _reciboService.ObtenerSiguienteNumeroAsync(negocioId);
                    var (enviado, html) = await _emailService.EnviarReciboPagoClienteAsync(
                        cliente.Email, nombreCompleto, negocio.NegocioNombre,
                        concepto, precio, dias, negocio.Email, negocio.Telefono, numRecibo);
                    await _reciboService.CrearReciboAsync(negocioId, numRecibo, "pago_cliente",
                        cliente.Email, nombreCompleto, negocio.NegocioNombre, concepto, precio, html);
                }
            }
            catch { }

            return Ok(new { success = true, message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SECCIÓN 5 — GESTIÓN DE CLIENTES DIARIOS
    //
    // "Clientes diarios" son aquellos que pagaron solo por un día de acceso,
    // sin tomar una membresía mensual. El flujo típico es:
    //   1. Se registra al cliente como "diario" (EsDiario = true).
    //   2. El dueño ve la lista de diarios y envía mensajes de WhatsApp
    //      usando los links wa.me generados automáticamente.
    //   3. Una vez contactados, se limpia la lista llamando a este endpoint.
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Limpia la lista de clientes diarios marcando EsDiario = false en todos ellos.
    /// Se llama DESPUÉS de que el dueño ya envió los mensajes de cobro/recordatorio
    /// vía WhatsApp, para que la lista quede vacía para el día siguiente.
    ///
    /// Devuelve cuántos clientes fueron actualizados (limpiados).
    /// </summary>
    /// <param name="negocioId">ID del negocio cuyos clientes diarios se van a limpiar</param>
    [HttpPost("LimpiarClientesDiarios")]
    public async Task<IActionResult> LimpiarClientesDiarios(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            // El servicio devuelve el número de registros actualizados
            // para que el frontend pueda mostrar confirmación ("X clientes limpiados")
            var limpiados = await _clienteService.LimpiarClientesDiariosAsync(negocioId);
            return Ok(new { success = true, limpiados });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SECCIÓN 6 — IMPORTACIÓN Y EXPORTACIÓN EXCEL
    //
    // Permite al dueño del negocio migrar datos desde/hacia hojas de cálculo.
    // Usa la librería ClosedXML para manipular archivos .xlsx sin necesidad de
    // tener Microsoft Office instalado en el servidor.
    //
    // EXPORTAR: genera un archivo Excel con todos los clientes actuales.
    // IMPORTAR: lee un archivo Excel y crea los clientes que no existan aún.
    //           Omite duplicados automáticamente para evitar registros repetidos.
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Exporta todos los clientes del negocio a un archivo Excel descargable (.xlsx).
    /// El nombre del archivo incluye la fecha actual (ej: Clientes_20260218.xlsx)
    /// para facilitar el archivado histórico.
    ///
    /// El browser del usuario recibirá el archivo y lo descargará automáticamente
    /// gracias al Content-Type correcto de la respuesta.
    /// </summary>
    /// <param name="negocioId">ID del negocio cuyos clientes se van a exportar</param>
    [HttpGet("ExportClientesExcel")]
    public async Task<IActionResult> ExportClientesExcel(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            // El servicio genera el archivo en memoria y devuelve el array de bytes.
            // No se guarda en disco para evitar acumular archivos temporales en el servidor.
            var content = await _clienteService.ExportClientesExcelAsync(negocioId);

            // File() le indica al browser que descargue el contenido como un archivo.
            // El Content-Type "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            // es el MIME type oficial de los archivos .xlsx (Excel moderno).
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Clientes_{TimeHelper.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Importa clientes desde un archivo Excel (.xlsx o .xls) subido por el usuario.
    /// El servicio detecta las columnas automáticamente por sus encabezados (headers),
    /// por lo que el orden de las columnas no importa.
    /// Los registros duplicados (mismo nombre o teléfono) son omitidos silenciosamente.
    ///
    /// El archivo llega como multipart/form-data mediante un input[type=file] en el HTML.
    /// La validación de extensión se hace aquí (en el controlador) antes de
    /// siquiera enviar el stream al servicio, para fallar rápido sin procesar archivos inválidos.
    /// </summary>
    /// <param name="negocioId">ID del negocio donde se importarán los clientes</param>
    /// <param name="file">Archivo Excel subido por el usuario desde el formulario</param>
    [HttpPost("ImportarClientesExcel")]
    public async Task<IActionResult> ImportarClientesExcel(Guid negocioId, IFormFile file)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();

            // Validar que el usuario realmente adjuntó un archivo y que no está vacío.
            // IFormFile puede ser null si el campo del formulario se envió vacío.
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No se ha proporcionado ningún archivo" });

            // SEGURIDAD: Limitar tamaño del archivo a 10 MB para prevenir DoS.
            // Un archivo Excel con miles de clientes típicamente pesa <1 MB,
            // así que 10 MB es un límite generoso pero seguro.
            const long maxBytes = 10 * 1024 * 1024; // 10 MB
            if (file.Length > maxBytes)
                return BadRequest(new { success = false, message = "El archivo no puede superar 10 MB" });

            // Validar que la extensión sea Excel. ToLowerInvariant() normaliza
            // mayúsculas/minúsculas (.XLSX, .Xlsx, .xlsx → todos pasan la validación).
            // Esto previene que se intenten parsear archivos PDF, imágenes, etc.
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
                return BadRequest(new { success = false, message = "El archivo debe ser un Excel (.xlsx o .xls)" });

            // Abrir el stream del archivo y pasarlo al servicio.
            // Se usa "using" para garantizar que el stream se cierre y libere
            // memoria aunque el procesamiento falle con una excepción.
            using var stream = file.OpenReadStream();
            var result = await _clienteService.ImportarClientesExcelAsync(negocioId, stream);

            // El resultado incluye cuántos clientes se importaron, cuántos se omitieron
            // por ser duplicados, y posibles mensajes de error por fila.
            return Ok(result);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SECCION 7 — RECIBOS
    //
    // Los recibos se generan automaticamente al crear un cliente, renovar
    // membresia o procesar una venta POS. Esta seccion permite consultarlos,
    // buscarlos por numero, exportarlos a Excel y eliminar los antiguos.
    // Cada recibo contiene el HTML completo que fue enviado por email.
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todos los recibos del negocio, ordenados del mas reciente al mas antiguo.
    /// </summary>
    [HttpGet("GetRecibos")]
    public async Task<IActionResult> GetRecibos(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();
            var recibos = await _reciboService.GetRecibosAsync(negocioId);
            return Ok(recibos);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene un recibo especifico por su ID. Incluye el contenido HTML completo.
    /// </summary>
    [HttpGet("GetRecibo")]
    public async Task<IActionResult> GetRecibo(Guid reciboId, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();
            var recibo = await _reciboService.GetReciboAsync(reciboId, negocioId);
            if (recibo == null)
                return NotFound(new { success = false, message = "Recibo no encontrado" });
            return Ok(recibo);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Busca un recibo por su numero secuencial (ej: 000042).
    /// Util para busqueda rapida desde el dashboard.
    /// </summary>
    [HttpGet("BuscarRecibo")]
    public async Task<IActionResult> BuscarRecibo(int numero, Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();
            var recibo = await _reciboService.BuscarPorNumeroAsync(numero, negocioId);
            return Ok(recibo);
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Exporta los recibos de un mes especifico a Excel (.xlsx).
    /// Util para contabilidad mensual y declaracion de impuestos.
    /// </summary>
    [HttpGet("ExportRecibosExcel")]
    public async Task<IActionResult> ExportRecibosExcel(Guid negocioId, int anio, int mes)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();
            var content = await _reciboService.ExportRecibosExcelAsync(negocioId, anio, mes);
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Recibos_{anio}_{mes:D2}.xlsx");
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene la fecha del recibo mas antiguo del negocio.
    /// Se usa en el frontend para definir el rango del selector de fechas
    /// en la funcion de limpieza de recibos antiguos.
    /// </summary>
    [HttpGet("GetFechaReciboMasAntiguo")]
    public async Task<IActionResult> GetFechaReciboMasAntiguo(Guid negocioId)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();
            var fecha = await _reciboService.GetFechaReciboMasAntiguoAsync(negocioId);
            return Ok(new { fecha });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Elimina permanentemente todos los recibos anteriores a la fecha indicada.
    /// Util para liberar espacio en BD cuando los recibos HTML se acumulan.
    /// Devuelve la cantidad de recibos eliminados.
    /// </summary>
    [HttpPost("EliminarRecibosAntiguos")]
    public async Task<IActionResult> EliminarRecibosAntiguos(Guid negocioId, DateTime anteriorA)
    {
        try
        {
            var nId = _authService.GetNegocioId(User);
            if (!nId.HasValue || negocioId != nId.Value)
                return Forbid();
            var eliminados = await _reciboService.EliminarRecibosAntiguosAsync(negocioId, anteriorA);
            return Ok(new { success = true, eliminados });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }
}
