// ═══════════════════════════════════════════════════════════
// DeliveryCommissionBlockingService.cs — Bloqueo automático por deuda
//
// Servicio en segundo plano que se ejecuta a medianoche (hora Ecuador)
// y bloquea a motorizados y restaurantes que tienen deuda pendiente:
//
//   1. Motorizados plan "comision" con ComisionesAcumuladas > 0 → Bloqueado=true
//   2. Motorizados plan "mensual" con FechaExpiracion vencida → Bloqueado=true
//   3. Restaurantes plan "comision" con ComisionesDeliveryAcumuladas > 0 → BloqueadoDelivery=true
//
// Un motorizado/restaurante bloqueado no puede tomar/recibir nuevos pedidos
// hasta que envíe un comprobante de pago y el admin lo confirme.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

/// <summary>
/// Servicio en segundo plano que bloquea automáticamente a motorizados y
/// restaurantes con deuda de comisiones o suscripción vencida.
/// Se ejecuta diariamente a medianoche (hora Ecuador, UTC-5).
/// </summary>
public class DeliveryCommissionBlockingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeliveryCommissionBlockingService> _logger;

    public DeliveryCommissionBlockingService(
        IServiceScopeFactory scopeFactory,
        ILogger<DeliveryCommissionBlockingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DeliveryCommissionBlockingService iniciado");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Calcular próxima medianoche hora Ecuador (UTC-5)
                var ecuadorZone = TimeHelper.EcuadorTz;
                var nowEcuador = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ecuadorZone);

                var nextMidnight = nowEcuador.Date.AddDays(1); // medianoche del día siguiente
                var nextMidnightUtc = TimeZoneInfo.ConvertTimeToUtc(nextMidnight, ecuadorZone);
                var delay = nextMidnightUtc - DateTime.UtcNow;

                _logger.LogInformation(
                    "DeliveryCommissionBlocking: próxima ejecución en {Delay} ({NextRun} hora Ecuador)",
                    delay, nextMidnight);

                await Task.Delay(delay, stoppingToken);

                await EjecutarBloqueoAsync(stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            // Apagado normal del servicio
        }

        _logger.LogInformation("DeliveryCommissionBlockingService detenido");
    }

    /// <summary>
    /// Ejecuta el proceso de bloqueo automático por deuda.
    /// </summary>
    private async Task EjecutarBloqueoAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Bloquear motorizados con plan "comision" que tengan comisiones acumuladas
            var motosComision = await context.Motorizados
                .Where(m => m.IsActive
                             && m.TipoPlan == "comision"
                             && m.ComisionesAcumuladas > 0
                             && !m.Bloqueado)
                .ToListAsync(stoppingToken);

            foreach (var m in motosComision)
                m.Bloqueado = true;

            // 2. Bloquear motorizados con plan "mensual" y suscripción vencida
            var ahora = TimeHelper.Now;
            var motosMensual = await context.Motorizados
                .Where(m => m.IsActive
                             && m.TipoPlan == "mensual"
                             && m.FechaExpiracion < ahora
                             && !m.Bloqueado)
                .ToListAsync(stoppingToken);

            foreach (var m in motosMensual)
                m.Bloqueado = true;

            // 3. Bloquear restaurantes con plan "comision" que tengan comisiones acumuladas
            var restosComision = await context.Negocios
                .Where(n => n.IsActive
                             && n.TipoNegocio == "restaurante"
                             && n.TipoPlanDelivery == "comision"
                             && n.ComisionesDeliveryAcumuladas > 0
                             && !n.BloqueadoDelivery)
                .ToListAsync(stoppingToken);

            foreach (var r in restosComision)
                r.BloqueadoDelivery = true;

            await context.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "DeliveryCommissionBlocking: Bloqueados {MotoComision} motos (comisión), " +
                "{MotoMensual} motos (mensual vencida), {Restos} restaurantes (comisión)",
                motosComision.Count, motosMensual.Count, restosComision.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en DeliveryCommissionBlockingService");
        }
    }
}
