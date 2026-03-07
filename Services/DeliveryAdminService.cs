// ═══════════════════════════════════════════════════════════════════════════
// DeliveryAdminService.cs — Servicio de gestión del sistema de delivery
//
// RESPONSABILIDADES:
//   - Procesar solicitudes de registro de motorizados y restaurantes
//   - Aprobar/rechazar solicitudes (creando las entidades correspondientes)
//   - Gestionar pagos de suscripción y comisiones
//   - Registrar comisiones por pedido entregado
//   - Verificar bloqueos por deuda
//   - Proporcionar estadísticas del sistema de delivery
//
// REGLAS DE NEGOCIO:
//   - Cédula y email deben ser únicos (verificación dual: solicitudes + entidades)
//   - Password se hashea con BCrypt al registrar la solicitud
//   - Al aprobar motorizado: se crea Motorizado con TipoPlan="comision"
//   - Al aprobar restaurante: se crea Gym con TipoNegocio="restaurante"
//   - Comisión: $0.20 por pedido entregado (solo plan "comision")
//   - Confirmar pago: desbloquea y resetea comisiones acumuladas
// ═══════════════════════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Implementación del servicio de gestión del sistema de delivery.
/// </summary>
public class DeliveryAdminService : IDeliveryAdminService
{
    private readonly ApplicationDbContext _context;

    public DeliveryAdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // REGISTRO
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<(bool success, string message, Guid? solicitudId)> RegistrarMotorizadoAsync(
        string nombre, string apellido, string email, string password,
        string telefono, string cedula, string tipoVehiculo, string placa,
        string fotoCedulaFrontal, string fotoCedulaTrasera,
        string fotoLicencia, string fotoSelfie, string fotoVehiculo)
    {
        // Validaciones básicas
        if (string.IsNullOrWhiteSpace(nombre))
            return (false, "El nombre es obligatorio", null);
        if (string.IsNullOrWhiteSpace(apellido))
            return (false, "El apellido es obligatorio", null);
        if (string.IsNullOrWhiteSpace(email))
            return (false, "El email es obligatorio", null);
        if (string.IsNullOrWhiteSpace(password))
            return (false, "La contraseña es obligatoria", null);
        if (string.IsNullOrWhiteSpace(telefono))
            return (false, "El teléfono es obligatorio", null);
        if (string.IsNullOrWhiteSpace(cedula))
            return (false, "La cédula es obligatoria", null);
        if (string.IsNullOrWhiteSpace(tipoVehiculo))
            return (false, "El tipo de vehículo es obligatorio", null);

        // Validar 5 fotos requeridas
        if (string.IsNullOrWhiteSpace(fotoCedulaFrontal))
            return (false, "La foto de la cédula frontal es obligatoria", null);
        if (string.IsNullOrWhiteSpace(fotoCedulaTrasera))
            return (false, "La foto de la cédula trasera es obligatoria", null);
        if (string.IsNullOrWhiteSpace(fotoLicencia))
            return (false, "La foto de la licencia es obligatoria", null);
        if (string.IsNullOrWhiteSpace(fotoSelfie))
            return (false, "La selfie es obligatoria", null);
        if (string.IsNullOrWhiteSpace(fotoVehiculo))
            return (false, "La foto del vehículo es obligatoria", null);

        var emailNorm = email.Trim().ToLowerInvariant();

        // Verificar cédula única en solicitudes no rechazadas
        var cedulaExisteSolicitud = await _context.SolicitudesRegistro.AsNoTracking()
            .AnyAsync(s => s.TipoSolicitud == "motorizado"
                           && s.Cedula == cedula.Trim()
                           && s.Estado != "rechazada");
        if (cedulaExisteSolicitud)
            return (false, "Ya existe una solicitud pendiente o aprobada con esta cédula", null);

        // Verificar cédula única en motorizados existentes
        var cedulaExisteMotorizado = await _context.Motorizados.AsNoTracking()
            .AnyAsync(m => m.Cedula == cedula.Trim());
        if (cedulaExisteMotorizado)
            return (false, "Ya existe un motorizado registrado con esta cédula", null);

        // Verificar email único en solicitudes no rechazadas
        var emailExisteSolicitud = await _context.SolicitudesRegistro.AsNoTracking()
            .AnyAsync(s => s.Email.ToLower() == emailNorm && s.Estado != "rechazada");
        if (emailExisteSolicitud)
            return (false, "Ya existe una solicitud pendiente o aprobada con este email", null);

        // Verificar email único en motorizados existentes
        var emailExisteMotorizado = await _context.Motorizados.AsNoTracking()
            .AnyAsync(m => m.Email.ToLower() == emailNorm);
        if (emailExisteMotorizado)
            return (false, "Ya existe un motorizado registrado con este email", null);

        // Verificar email único en negocios existentes
        var emailExisteNegocio = await _context.Negocios.AsNoTracking()
            .AnyAsync(n => n.Email.ToLower() == emailNorm);
        if (emailExisteNegocio)
            return (false, "Este email ya está registrado en la plataforma", null);

        // Hash password con BCrypt (work factor 12)
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        var solicitud = new SolicitudRegistro
        {
            SolicitudId = Guid.NewGuid(),
            TipoSolicitud = "motorizado",
            Estado = "pendiente",
            Nombre = nombre.Trim(),
            Apellido = apellido.Trim(),
            Email = emailNorm,
            Password = hashedPassword,
            Telefono = telefono.Trim(),
            Cedula = cedula.Trim(),
            TipoVehiculo = tipoVehiculo.Trim(),
            Placa = placa?.Trim(),
            FotoCedulaFrontal = fotoCedulaFrontal,
            FotoCedulaTrasera = fotoCedulaTrasera,
            FotoLicencia = fotoLicencia,
            FotoSelfie = fotoSelfie,
            FotoVehiculo = fotoVehiculo,
            FechaCreacion = TimeHelper.Now
        };

        _context.SolicitudesRegistro.Add(solicitud);
        await _context.SaveChangesAsync();

        return (true, "Solicitud de registro enviada exitosamente. Será revisada por un administrador.", solicitud.SolicitudId);
    }

    /// <inheritdoc/>
    public async Task<(bool success, string message, Guid? solicitudId)> RegistrarRestauranteAsync(
        string nombre, string apellido, string email, string password,
        string telefono, string nombreNegocio, string duenoNegocio,
        string direccion, string ciudad, double? latitud, double? longitud,
        string tiposComida, string logoUrl)
    {
        // Validaciones básicas
        if (string.IsNullOrWhiteSpace(nombre))
            return (false, "El nombre es obligatorio", null);
        if (string.IsNullOrWhiteSpace(apellido))
            return (false, "El apellido es obligatorio", null);
        if (string.IsNullOrWhiteSpace(email))
            return (false, "El email es obligatorio", null);
        if (string.IsNullOrWhiteSpace(password))
            return (false, "La contraseña es obligatoria", null);
        if (string.IsNullOrWhiteSpace(telefono))
            return (false, "El teléfono es obligatorio", null);
        if (string.IsNullOrWhiteSpace(nombreNegocio))
            return (false, "El nombre del negocio es obligatorio", null);

        var emailNorm = email.Trim().ToLowerInvariant();

        // Verificar email único en solicitudes no rechazadas
        var emailExisteSolicitud = await _context.SolicitudesRegistro.AsNoTracking()
            .AnyAsync(s => s.Email.ToLower() == emailNorm && s.Estado != "rechazada");
        if (emailExisteSolicitud)
            return (false, "Ya existe una solicitud pendiente o aprobada con este email", null);

        // Verificar email único en negocios existentes
        var emailExisteNegocio = await _context.Negocios.AsNoTracking()
            .AnyAsync(n => n.Email.ToLower() == emailNorm);
        if (emailExisteNegocio)
            return (false, "Ya existe un negocio registrado con este email", null);

        // Verificar email único en motorizados existentes
        var emailExisteMotorizado = await _context.Motorizados.AsNoTracking()
            .AnyAsync(m => m.Email.ToLower() == emailNorm);
        if (emailExisteMotorizado)
            return (false, "Este email ya está registrado en la plataforma", null);

        // Hash password con BCrypt (work factor 12)
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        var solicitud = new SolicitudRegistro
        {
            SolicitudId = Guid.NewGuid(),
            TipoSolicitud = "restaurante",
            Estado = "pendiente",
            Nombre = nombre.Trim(),
            Apellido = apellido.Trim(),
            Email = emailNorm,
            Password = hashedPassword,
            Telefono = telefono.Trim(),
            NombreNegocio = nombreNegocio.Trim(),
            DuenoNegocio = duenoNegocio?.Trim(),
            Direccion = direccion?.Trim(),
            Ciudad = ciudad?.Trim(),
            Latitud = latitud,
            Longitud = longitud,
            TiposComida = tiposComida?.Trim(),
            LogoUrl = logoUrl,
            FechaCreacion = TimeHelper.Now
        };

        _context.SolicitudesRegistro.Add(solicitud);
        await _context.SaveChangesAsync();

        return (true, "Solicitud de registro enviada exitosamente. Será revisada por un administrador.", solicitud.SolicitudId);
    }

    /// <inheritdoc/>
    public async Task<object?> GetEstadoSolicitudAsync(Guid solicitudId)
    {
        return await _context.SolicitudesRegistro.AsNoTracking()
            .Where(s => s.SolicitudId == solicitudId)
            .Select(s => new
            {
                s.SolicitudId,
                s.TipoSolicitud,
                s.Estado,
                s.Nombre,
                s.Apellido,
                s.Email,
                s.MotivoRechazo,
                s.FechaCreacion,
                s.FechaRevision
            })
            .FirstOrDefaultAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ADMIN — SOLICITUDES
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<object> GetSolicitudesPendientesAsync()
    {
        return await _context.SolicitudesRegistro.AsNoTracking()
            .Where(s => s.Estado == "pendiente")
            .OrderBy(s => s.FechaCreacion)
            .Select(s => new
            {
                s.SolicitudId,
                s.TipoSolicitud,
                s.Nombre,
                s.Apellido,
                s.Email,
                s.Telefono,
                s.Cedula,
                s.NombreNegocio,
                s.Ciudad,
                s.FechaCreacion
            })
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<object?> GetSolicitudAsync(Guid solicitudId)
    {
        return await _context.SolicitudesRegistro.AsNoTracking()
            .Where(s => s.SolicitudId == solicitudId)
            .Select(s => new
            {
                s.SolicitudId,
                s.TipoSolicitud,
                s.Estado,
                s.Nombre,
                s.Apellido,
                s.Email,
                s.Telefono,
                // Motorizado
                s.Cedula,
                s.TipoVehiculo,
                s.Placa,
                s.FotoCedulaFrontal,
                s.FotoCedulaTrasera,
                s.FotoLicencia,
                s.FotoSelfie,
                s.FotoVehiculo,
                // Restaurante
                s.NombreNegocio,
                s.DuenoNegocio,
                s.Direccion,
                s.Ciudad,
                s.Latitud,
                s.Longitud,
                s.TiposComida,
                s.LogoUrl,
                // Review
                s.MotivoRechazo,
                s.FechaCreacion,
                s.FechaRevision
            })
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<(bool success, string message)> AprobarSolicitudAsync(Guid solicitudId)
    {
        var solicitud = await _context.SolicitudesRegistro
            .FirstOrDefaultAsync(s => s.SolicitudId == solicitudId);

        if (solicitud == null)
            return (false, "Solicitud no encontrada");

        if (solicitud.Estado != "pendiente")
            return (false, "Solo se pueden aprobar solicitudes pendientes");

        if (solicitud.TipoSolicitud == "motorizado")
        {
            // Verificar que la cédula no haya sido registrada mientras la solicitud estaba pendiente
            var cedulaExiste = await _context.Motorizados.AsNoTracking()
                .AnyAsync(m => m.Cedula == solicitud.Cedula);
            if (cedulaExiste)
                return (false, "Ya existe un motorizado con esta cédula. La solicitud ya no es válida.");

            var emailExiste = await _context.Motorizados.AsNoTracking()
                .AnyAsync(m => m.Email.ToLower() == solicitud.Email.ToLower());
            if (emailExiste)
                return (false, "Ya existe un motorizado con este email. La solicitud ya no es válida.");

            // Crear motorizado
            var motorizado = new Motorizado
            {
                MotorizadoId = Guid.NewGuid(),
                Nombre = solicitud.Nombre,
                Apellido = solicitud.Apellido,
                Email = solicitud.Email,
                Password = solicitud.Password, // Ya está hasheado con BCrypt
                Telefono = solicitud.Telefono,
                Cedula = solicitud.Cedula,
                TipoVehiculo = solicitud.TipoVehiculo,
                Vehiculo = solicitud.TipoVehiculo, // Copiar también al campo descriptivo
                Placa = solicitud.Placa,
                FotoCedulaFrontal = solicitud.FotoCedulaFrontal,
                FotoCedulaTrasera = solicitud.FotoCedulaTrasera,
                FotoLicencia = solicitud.FotoLicencia,
                FotoSelfie = solicitud.FotoSelfie,
                FotoVehiculo = solicitud.FotoVehiculo,
                FotoUrl = solicitud.FotoSelfie, // Selfie como foto de perfil
                IsActive = true,
                IsDisponible = false,
                TipoPlan = "comision",
                PrecioSuscripcion = 10m,
                Bloqueado = false,
                ComisionesAcumuladas = 0,
                FechaCreacion = TimeHelper.Now
            };

            _context.Motorizados.Add(motorizado);
        }
        else if (solicitud.TipoSolicitud == "restaurante")
        {
            var emailExiste = await _context.Negocios.AsNoTracking()
                .AnyAsync(n => n.Email.ToLower() == solicitud.Email.ToLower());
            if (emailExiste)
                return (false, "Ya existe un negocio con este email. La solicitud ya no es válida.");

            // Crear negocio tipo restaurante
            var negocio = new Gym
            {
                NegocioId = Guid.NewGuid(),
                NegocioNombre = solicitud.NombreNegocio ?? solicitud.Nombre + " " + solicitud.Apellido,
                DuenoNegocio = solicitud.DuenoNegocio ?? solicitud.Nombre + " " + solicitud.Apellido,
                Email = solicitud.Email,
                Password = solicitud.Password, // Ya está hasheado con BCrypt
                Telefono = solicitud.Telefono,
                TipoNegocio = "restaurante",
                Direccion = solicitud.Direccion,
                Ciudad = solicitud.Ciudad,
                Latitud = solicitud.Latitud,
                Longitud = solicitud.Longitud,
                TiposComida = solicitud.TiposComida,
                LogoUrl = solicitud.LogoUrl,
                IsActive = true,
                EsPrueba = false,
                NegocioBloqueado = false,
                TipoPlanDelivery = "comision",
                ComisionesDeliveryAcumuladas = 0,
                BloqueadoDelivery = false,
                FechaCreacion = TimeHelper.Now,
                FechaDeActualizacion = TimeHelper.Now
            };

            _context.Negocios.Add(negocio);
        }
        else
        {
            return (false, $"Tipo de solicitud desconocido: {solicitud.TipoSolicitud}");
        }

        // Marcar solicitud como aprobada
        solicitud.Estado = "aprobada";
        solicitud.FechaRevision = TimeHelper.Now;

        await _context.SaveChangesAsync();

        var tipoTexto = solicitud.TipoSolicitud == "motorizado" ? "Motorizado" : "Restaurante";
        return (true, $"{tipoTexto} aprobado y creado exitosamente");
    }

    /// <inheritdoc/>
    public async Task<(bool success, string message)> RechazarSolicitudAsync(Guid solicitudId, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            return (false, "Debe proporcionar un motivo de rechazo");

        var solicitud = await _context.SolicitudesRegistro
            .FirstOrDefaultAsync(s => s.SolicitudId == solicitudId);

        if (solicitud == null)
            return (false, "Solicitud no encontrada");

        if (solicitud.Estado != "pendiente")
            return (false, "Solo se pueden rechazar solicitudes pendientes");

        solicitud.Estado = "rechazada";
        solicitud.MotivoRechazo = motivo.Trim();
        solicitud.FechaRevision = TimeHelper.Now;

        await _context.SaveChangesAsync();

        return (true, "Solicitud rechazada");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ADMIN — PAGOS
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<object> GetPagosDeliveryPendientesAsync()
    {
        return await _context.PagosDelivery.AsNoTracking()
            .Where(p => p.Estado == "pendiente")
            .OrderBy(p => p.FechaCreacion)
            .Select(p => new
            {
                p.PagoDeliveryId,
                p.TipoPagador,
                p.TipoPago,
                p.Monto,
                p.NumeroConfirmacion,
                p.FechaCreacion,
                // Datos del pagador
                MotorizadoNombre = p.Motorizado != null
                    ? p.Motorizado.Nombre + " " + p.Motorizado.Apellido
                    : null,
                MotorizadoEmail = p.Motorizado != null ? p.Motorizado.Email : null,
                NegocioNombre = p.Negocio != null ? p.Negocio.NegocioNombre : null,
                NegocioEmail = p.Negocio != null ? p.Negocio.Email : null
            })
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<(bool success, string message)> ConfirmarPagoDeliveryAsync(Guid pagoId)
    {
        var pago = await _context.PagosDelivery
            .FirstOrDefaultAsync(p => p.PagoDeliveryId == pagoId);

        if (pago == null)
            return (false, "Pago no encontrado");

        if (pago.Estado != "pendiente")
            return (false, "Solo se pueden confirmar pagos pendientes");

        pago.Estado = "confirmado";
        pago.FechaRevision = TimeHelper.Now;

        if (pago.TipoPagador == "motorizado" && pago.MotorizadoId.HasValue)
        {
            var motorizado = await _context.Motorizados.FindAsync(pago.MotorizadoId.Value);
            if (motorizado != null)
            {
                motorizado.Bloqueado = false;
                motorizado.UltimaLiquidacionComisiones = TimeHelper.Now;

                if (pago.TipoPago == "mensual")
                {
                    motorizado.FechaPago = TimeHelper.Now;
                    motorizado.FechaExpiracion = TimeHelper.Now.AddDays(30);
                }
                else if (pago.TipoPago == "comision")
                {
                    // Solo resetear comisiones acumuladas para pagos de comisión
                    motorizado.ComisionesAcumuladas = 0;

                    // Marcar comisiones como pagadas
                    var comisionesPendientes = await _context.ComisionesDelivery
                        .Where(c => c.MotorizadoId == pago.MotorizadoId.Value && !c.Pagada)
                        .ToListAsync();
                    foreach (var c in comisionesPendientes)
                        c.Pagada = true;
                }
            }
        }
        else if (pago.TipoPagador == "restaurante" && pago.NegocioId.HasValue)
        {
            var negocio = await _context.Negocios.FindAsync(pago.NegocioId.Value);
            if (negocio != null)
            {
                negocio.BloqueadoDelivery = false;
                negocio.UltimaLiquidacionComisionesDelivery = TimeHelper.Now;

                if (pago.TipoPago == "comision")
                {
                    negocio.ComisionesDeliveryAcumuladas = 0;

                    // Marcar comisiones como pagadas
                    var comisionesPendientes = await _context.ComisionesDelivery
                        .Where(c => c.NegocioId == pago.NegocioId.Value && !c.Pagada)
                        .ToListAsync();
                    foreach (var c in comisionesPendientes)
                        c.Pagada = true;
                }
            }
        }

        await _context.SaveChangesAsync();

        return (true, "Pago confirmado exitosamente");
    }

    /// <inheritdoc/>
    public async Task<(bool success, string message)> RechazarPagoDeliveryAsync(Guid pagoId, string notas)
    {
        var pago = await _context.PagosDelivery
            .FirstOrDefaultAsync(p => p.PagoDeliveryId == pagoId);

        if (pago == null)
            return (false, "Pago no encontrado");

        if (pago.Estado != "pendiente")
            return (false, "Solo se pueden rechazar pagos pendientes");

        pago.Estado = "rechazado";
        pago.Notas = notas?.Trim();
        pago.FechaRevision = TimeHelper.Now;

        await _context.SaveChangesAsync();

        return (true, "Pago rechazado");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMISIONES
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task RegistrarComisionDeliveryAsync(Guid pedidoId)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.Motorizado)
            .FirstOrDefaultAsync(p => p.PedidoId == pedidoId);

        if (pedido == null) return;

        // Comisión para el motorizado (si tiene plan "comision")
        if (pedido.MotorizadoId.HasValue && pedido.Motorizado?.TipoPlan == "comision")
        {
            _context.ComisionesDelivery.Add(new ComisionDelivery
            {
                ComisionDeliveryId = Guid.NewGuid(),
                TipoPagador = "motorizado",
                MotorizadoId = pedido.MotorizadoId,
                PedidoId = pedidoId,
                Monto = 0.20m,
                Fecha = TimeHelper.Now
            });
            pedido.Motorizado.ComisionesAcumuladas += 0.20m;
        }

        // Comisión para el restaurante (si tiene plan "comision")
        var negocio = await _context.Negocios.FindAsync(pedido.NegocioId);
        if (negocio?.TipoPlanDelivery == "comision")
        {
            _context.ComisionesDelivery.Add(new ComisionDelivery
            {
                ComisionDeliveryId = Guid.NewGuid(),
                TipoPagador = "restaurante",
                NegocioId = pedido.NegocioId,
                PedidoId = pedidoId,
                Monto = 0.20m,
                Fecha = TimeHelper.Now
            });
            negocio.ComisionesDeliveryAcumuladas += 0.20m;
        }

        await _context.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BLOQUEO Y PAGOS
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<bool> IsMotorizadoBloqueadoAsync(Guid motorizadoId)
    {
        var motorizado = await _context.Motorizados.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MotorizadoId == motorizadoId);

        return motorizado?.Bloqueado ?? true;
    }

    /// <inheritdoc/>
    public async Task<(bool success, string message)> EnviarConfirmacionPagoAsync(
        string tipoPagador, Guid pagadorId, string tipoPago, decimal monto, string numConfirmacion)
    {
        if (string.IsNullOrWhiteSpace(tipoPagador))
            return (false, "El tipo de pagador es obligatorio");
        if (string.IsNullOrWhiteSpace(tipoPago))
            return (false, "El tipo de pago es obligatorio");
        if (monto <= 0)
            return (false, "El monto debe ser mayor a 0");
        if (string.IsNullOrWhiteSpace(numConfirmacion))
            return (false, "El número de confirmación es obligatorio");

        var pago = new PagoDelivery
        {
            PagoDeliveryId = Guid.NewGuid(),
            TipoPagador = tipoPagador,
            TipoPago = tipoPago,
            Monto = monto,
            NumeroConfirmacion = numConfirmacion.Trim(),
            Estado = "pendiente",
            FechaCreacion = TimeHelper.Now
        };

        if (tipoPagador == "motorizado")
        {
            var motorizado = await _context.Motorizados.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MotorizadoId == pagadorId);
            if (motorizado == null)
                return (false, "Motorizado no encontrado");
            pago.MotorizadoId = pagadorId;
        }
        else if (tipoPagador == "restaurante")
        {
            var negocio = await _context.Negocios.AsNoTracking()
                .FirstOrDefaultAsync(n => n.NegocioId == pagadorId);
            if (negocio == null)
                return (false, "Negocio no encontrado");
            pago.NegocioId = pagadorId;
        }
        else
        {
            return (false, "Tipo de pagador no válido. Use 'motorizado' o 'restaurante'");
        }

        _context.PagosDelivery.Add(pago);
        await _context.SaveChangesAsync();

        return (true, "Comprobante de pago enviado exitosamente. Será revisado por un administrador.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ESTADÍSTICAS
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<object> GetDeliveryAdminStatsAsync()
    {
        var solicitudesPendientesMotorizado = await _context.SolicitudesRegistro.AsNoTracking()
            .CountAsync(s => s.TipoSolicitud == "motorizado" && s.Estado == "pendiente");

        var solicitudesPendientesRestaurante = await _context.SolicitudesRegistro.AsNoTracking()
            .CountAsync(s => s.TipoSolicitud == "restaurante" && s.Estado == "pendiente");

        var pagosPendientes = await _context.PagosDelivery.AsNoTracking()
            .CountAsync(p => p.Estado == "pendiente");

        var totalMotorizadosActivos = await _context.Motorizados.AsNoTracking()
            .CountAsync(m => m.IsActive);

        var totalRestaurantesDelivery = await _context.Negocios.AsNoTracking()
            .CountAsync(n => n.TipoNegocio == "restaurante" && n.IsActive && n.TipoPlanDelivery != null);

        return new
        {
            solicitudesPendientesMotorizado,
            solicitudesPendientesRestaurante,
            solicitudesPendientesTotal = solicitudesPendientesMotorizado + solicitudesPendientesRestaurante,
            pagosPendientes,
            totalMotorizadosActivos,
            totalRestaurantesDelivery
        };
    }

    /// <inheritdoc/>
    public async Task<object?> GetEstadoSuscripcionMotorizadoAsync(Guid motorizadoId)
    {
        var motorizado = await _context.Motorizados.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MotorizadoId == motorizadoId);

        if (motorizado == null) return null;

        return new
        {
            motorizado.TipoPlan,
            motorizado.PrecioSuscripcion,
            motorizado.ComisionesAcumuladas,
            motorizado.Bloqueado,
            motorizado.FechaPago,
            motorizado.FechaExpiracion,
            motorizado.UltimaLiquidacionComisiones,
            montoAdeudado = motorizado.TipoPlan == "comision"
                ? motorizado.ComisionesAcumuladas
                : (motorizado.FechaExpiracion.HasValue && motorizado.FechaExpiracion.Value >= TimeHelper.Now)
                    ? 0m
                    : motorizado.PrecioSuscripcion
        };
    }

    /// <inheritdoc/>
    public async Task<object> GetHistorialPagosMotorizadoAsync(Guid motorizadoId)
    {
        return await _context.PagosDelivery.AsNoTracking()
            .Where(p => p.MotorizadoId == motorizadoId)
            .OrderByDescending(p => p.FechaCreacion)
            .Select(p => new
            {
                p.PagoDeliveryId,
                p.TipoPago,
                p.Monto,
                p.NumeroConfirmacion,
                p.Estado,
                p.Notas,
                p.FechaCreacion,
                p.FechaRevision
            })
            .ToListAsync();
    }
}
