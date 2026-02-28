namespace Gimnasio.Services;

/// <summary>
/// Background service que reprocesa facturas pendientes ante el SRI.
/// Ejecuta cada 10 minutos. Busca facturas con estado "Pendiente" o "Recibida"
/// que tengan menos intentos que el máximo configurado.
/// </summary>
public class FacturaReintentoService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FacturaReintentoService> _logger;

    public FacturaReintentoService(IServiceScopeFactory scopeFactory, ILogger<FacturaReintentoService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FacturaReintentoService iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var facturacionService = scope.ServiceProvider.GetRequiredService<IFacturacionElectronicaService>();

                var procesadas = await facturacionService.ReprocesarPendientesAsync();
                if (procesadas > 0)
                    _logger.LogInformation("FacturaReintentoService: {Count} facturas reprocesadas", procesadas);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en FacturaReintentoService");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("FacturaReintentoService detenido");
    }
}
