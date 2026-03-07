// ═══════════════════════════════════════════════════════════
// Motorizado.cs — Modelo de repartidor/driver del sistema de delivery
//
// Un motorizado es un conductor independiente que recoge pedidos
// de restaurantes y los entrega a los clientes finales.
// No pertenece a un negocio específico — es una entidad global
// que puede tomar pedidos de cualquier restaurante en la plataforma.
//
// Flujo:
//   Motorizado se registra → login propio → ve pedidos disponibles →
//   toma un pedido → recoge en restaurante → entrega al cliente
//
// El motorizado tiene credenciales propias (Email + Password BCrypt)
// separadas de las cuentas de negocios.
// ═══════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

/// <summary>
/// Representa un repartidor/conductor del sistema de delivery de My-Negocio.
/// Es una entidad global (no pertenece a un negocio específico) que puede
/// tomar y entregar pedidos de cualquier restaurante registrado en la plataforma.
/// </summary>
public class Motorizado
{
    /// <summary>
    /// Identificador único del motorizado (GUID generado automáticamente).
    /// [Key] = clave primaria de la tabla Motorizados.
    /// </summary>
    [Key]
    public Guid MotorizadoId { get; set; }

    /// <summary>
    /// Nombre(s) del motorizado, por ejemplo: "Carlos" o "José Luis".
    /// [Required] = obligatorio para registro.
    /// [StringLength(100)] = límite razonable para nombres de persona.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; }

    /// <summary>
    /// Apellido(s) del motorizado, por ejemplo: "Pérez" o "García López".
    /// [Required] = obligatorio.
    /// [StringLength(100)] = límite razonable para apellidos.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Apellido { get; set; }

    /// <summary>
    /// Correo electrónico del motorizado. Sirve como usuario de login.
    /// Debe ser único en la tabla (configurado en OnModelCreating).
    /// [Required] = obligatorio para autenticación.
    /// [StringLength(200)] = límite estándar para emails.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; }

    /// <summary>
    /// Contraseña del motorizado almacenada con hash BCrypt.
    /// Se genera al registrarse y se valida en el login.
    /// [Required] = obligatorio para autenticación.
    /// [StringLength(200)] = los hashes BCrypt tienen ~60 caracteres, se deja margen.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Password { get; set; }

    /// <summary>
    /// Número de teléfono del motorizado para contacto.
    /// Se muestra al cliente durante el tracking del pedido.
    /// [Required] = obligatorio para comunicación con clientes.
    /// [StringLength(20)] = suficiente para números con código de país.
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Telefono { get; set; }

    /// <summary>
    /// Foto del motorizado como data URI Base64 (data:image/jpeg;base64,...).
    /// Se muestra al cliente durante el tracking para identificar al repartidor.
    /// Nullable — no obligatorio al registrarse.
    /// </summary>
    public string FotoUrl { get; set; }

    /// <summary>
    /// Tipo de vehículo que usa el motorizado.
    /// Ejemplos: "Moto", "Bicicleta", "Auto", "A pie".
    /// [StringLength(100)] = suficiente para cualquier tipo de vehículo.
    /// </summary>
    [StringLength(100)]
    public string Vehiculo { get; set; }

    /// <summary>
    /// Número de placa del vehículo (si aplica).
    /// Nullable — bicicletas y peatones no tienen placa.
    /// [StringLength(20)] = suficiente para placas de cualquier país.
    /// </summary>
    [StringLength(20)]
    public string Placa { get; set; }

    // ═══════════════════════════════════════════════════════════
    // IDENTIDAD (registro y verificación)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Cédula de identidad del motorizado (10-13 dígitos Ecuador).
    /// Unique filtrado en OnModelCreating.
    /// </summary>
    [StringLength(15)]
    public string? Cedula { get; set; }

    /// <summary>
    /// Tipo de vehículo normalizado: "moto", "bicicleta", "auto".
    /// Diferente de Vehiculo (que es texto libre para descripción).
    /// </summary>
    [StringLength(20)]
    public string? TipoVehiculo { get; set; }

    /// <summary>Foto de la cédula frontal en Base64 data URI.</summary>
    public string? FotoCedulaFrontal { get; set; }

    /// <summary>Foto de la cédula trasera en Base64 data URI.</summary>
    public string? FotoCedulaTrasera { get; set; }

    /// <summary>Foto de la licencia de conducir en Base64 data URI.</summary>
    public string? FotoLicencia { get; set; }

    /// <summary>Selfie del motorizado en Base64 data URI.</summary>
    public string? FotoSelfie { get; set; }

    /// <summary>Foto del vehículo en Base64 data URI.</summary>
    public string? FotoVehiculo { get; set; }

    // ═══════════════════════════════════════════════════════════
    // SUSCRIPCIÓN DELIVERY
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Tipo de plan del motorizado: "comision" ($0.20/pedido) o "mensual" ($10/mes).
    /// Default "comision" — el motorizado empieza con plan por comisión.
    /// </summary>
    [StringLength(20)]
    public string TipoPlan { get; set; } = "comision";

    /// <summary>
    /// Precio de la suscripción mensual (si aplica). Default $10.
    /// </summary>
    public decimal PrecioSuscripcion { get; set; } = 10m;

    /// <summary>
    /// Fecha del último pago de suscripción mensual.
    /// </summary>
    public DateTime? FechaPago { get; set; }

    /// <summary>
    /// Fecha de expiración de la suscripción mensual.
    /// Si TipoPlan="mensual" y esta fecha pasó, el motorizado se bloquea.
    /// </summary>
    public DateTime? FechaExpiracion { get; set; }

    // ═══════════════════════════════════════════════════════════
    // BLOQUEO POR COMISIONES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Indica si el motorizado está bloqueado por deuda de comisiones
    /// o suscripción vencida. Un motorizado bloqueado no puede tomar pedidos.
    /// </summary>
    public bool Bloqueado { get; set; } = false;

    /// <summary>
    /// Total de comisiones acumuladas sin pagar ($0.20 por cada pedido entregado).
    /// Se resetea a 0 cuando el admin confirma el pago.
    /// </summary>
    public decimal ComisionesAcumuladas { get; set; } = 0;

    /// <summary>
    /// Fecha de la última vez que se liquidaron las comisiones.
    /// </summary>
    public DateTime? UltimaLiquidacionComisiones { get; set; }

    // ═══════════════════════════════════════════════════════════
    // ESTADO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Indica si la cuenta del motorizado está activa.
    /// false = cuenta suspendida o desactivada por el admin.
    /// Un motorizado inactivo no puede iniciar sesión ni tomar pedidos.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Indica si el motorizado está disponible para tomar nuevos pedidos.
    /// true = está en línea y puede recibir pedidos.
    /// false = está ocupado, en descanso o fuera de servicio.
    /// El motorizado controla este toggle desde su app.
    /// </summary>
    public bool IsDisponible { get; set; } = false;

    // ═══════════════════════════════════════════════════════════
    // GPS — Posición en tiempo real
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Latitud actual del motorizado (GPS).
    /// Nullable — no se conoce hasta que el motorizado activa la ubicación.
    /// Se actualiza periódicamente desde el frontend del motorizado.
    /// </summary>
    public double? Latitud { get; set; }

    /// <summary>
    /// Longitud actual del motorizado (GPS).
    /// Nullable — no se conoce hasta que el motorizado activa la ubicación.
    /// Se actualiza periódicamente desde el frontend del motorizado.
    /// </summary>
    public double? Longitud { get; set; }

    // ═══════════════════════════════════════════════════════════
    // ESTADÍSTICAS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Contador de entregas completadas exitosamente.
    /// Se incrementa cada vez que un pedido llega a estado "entregado".
    /// Se muestra al cliente como indicador de confiabilidad.
    /// </summary>
    public int TotalEntregas { get; set; } = 0;

    /// <summary>
    /// Fecha y hora en que el motorizado fue registrado en la plataforma.
    /// Se asigna automáticamente con TimeHelper.Now al crear el registro.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = TimeHelper.Now;

    // ═══════════════════════════════════════════════════════════
    // RELACIONES (navigation properties)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Lista de pedidos asignados a este motorizado (histórico y activos).
    /// Relación uno-a-muchos: un motorizado puede tener muchos pedidos.
    /// </summary>
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}
