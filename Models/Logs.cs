// ═══════════════════════════════════════════════════════════
// Logs.cs — Modelo de registro financiero y operativo del negocio
//
// Los logs son la fuente inmutable de verdad financiera del negocio.
// Se generan automáticamente cuando ocurren eventos del sistema
// (nuevo cliente, renovación, etc.) y también pueden crearse manualmente.
//
// Principios de diseño:
//   - INMUTABILIDAD: los logs nunca se editan, solo se agregan.
//   - PERSISTENCIA: eliminar un cliente NO borra sus logs históricos.
//   - DENORMALIZACIÓN: NombreCliente se guarda en texto para que el
//     historial sobreviva aunque el cliente sea eliminado.
//
// Tipos de log:
//   Automáticos: cliente_creado, cliente_editado, cliente_renovado,
//                cliente_eliminado, sesion_inicio, sesion_cierre
//   Manuales:    ingreso, gasto (creados por el dueño del negocio)
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Registro financiero y operativo de un negocio. Es la fuente inmutable de verdad.
/// Cada acción importante del sistema (y acciones manuales del dueño) genera un log.
///
/// Los logs sirven para:
///   1. Historial de movimientos financieros (ingresos y gastos).
///   2. Auditoría de operaciones (quién se registró, cuándo se renovó, etc.).
///   3. Generar reportes y gráficas en el dashboard del negocio.
///
/// IMPORTANTE: eliminar un cliente nunca debe borrar sus logs.
/// </summary>
public class Logs
{
    /// <summary>
    /// Identificador único del log (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla Logs.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// ID del negocio al que pertenece este log.
    /// [Required] = todo log debe pertenecer a un negocio.
    /// Se usa para filtrar logs por negocio (aislamiento multi-tenant).
    /// </summary>
    [Required]
    public Guid NegocioId { get; set; }

    /// <summary>
    /// Texto descriptivo del evento registrado.
    /// Ejemplos:
    ///   "Nuevo cliente registrado: Juan Pérez - 30 días - $500"
    ///   "Gasto de mantenimiento: reparación de baño"
    ///   "Sesión iniciada"
    /// [Required] = todo log debe tener una descripción.
    /// [MaxLength(300)] = suficiente para un mensaje descriptivo y conciso.
    /// </summary>
    [Required]
    [MaxLength(300)]
    public string Message { get; set; }

    /// <summary>
    /// Cantidad de dinero involucrada en este log (en moneda local).
    /// Positivo (+) = ingreso de dinero al negocio (ej: pago de membresía).
    /// Negativo (-) = gasto o salida de dinero (ej: compra de suministros).
    /// Cero (0)     = evento sin impacto financiero (ej: inicio de sesión).
    /// Valor por defecto: 0.
    /// </summary>
    public decimal Monto { get; set; } = 0;

    /// <summary>
    /// Categoría del log para filtrado y agrupación en reportes.
    /// Valores automáticos generados por el sistema:
    ///   "cliente_creado"   = se registró un nuevo cliente.
    ///   "cliente_editado"  = se modificaron datos de un cliente.
    ///   "cliente_renovado" = se renovó la membresía de un cliente.
    ///   "cliente_eliminado"= se eliminó un cliente del sistema.
    ///   "sesion_inicio"    = el negocio inició sesión en la plataforma.
    ///   "sesion_cierre"    = el negocio cerró sesión.
    /// Valores manuales creados por el dueño:
    ///   "ingreso"          = el dueño registró un ingreso manual.
    ///   "gasto"            = el dueño registró un gasto.
    /// [MaxLength(50)] = suficiente para los valores enumerados.
    /// Valor por defecto: "ingreso".
    /// </summary>
    [MaxLength(50)]
    public string Tipo { get; set; } = "ingreso";

    /// <summary>
    /// ID del cliente relacionado con este log (si aplica).
    /// Es nullable (?) porque algunos logs no están relacionados a un cliente
    /// (ej: inicio de sesión, gastos generales).
    /// NOTA: si el cliente es eliminado, este ID puede quedar como referencia huérfana,
    /// pero el campo NombreCliente conserva su nombre en texto.
    /// </summary>
    public Guid? ClienteId { get; set; }

    /// <summary>
    /// Nombre completo del cliente relacionado (denormalizado).
    /// Se guarda como texto para que el historial sea consistente incluso
    /// si el cliente es eliminado en el futuro.
    /// Ejemplo: "Juan Pérez".
    /// Es null para logs no relacionados a un cliente (ej: gastos, inicio de sesión).
    /// [MaxLength(200)] = suficiente para nombre + apellido completos.
    /// </summary>
    [MaxLength(200)]
    public string NombreCliente { get; set; }

    /// <summary>
    /// Fecha y hora exacta en que ocurrió el evento registrado.
    /// Se asigna automáticamente con TimeHelper.Now al crear el log.
    /// No se modifica después (inmutabilidad).
    /// Se usa para ordenar el historial y para filtrar por rangos de fechas en reportes.
    /// </summary>
    public DateTime Fecha { get; set; } = TimeHelper.Now;
}
