// ═══════════════════════════════════════════════════════════
// Program.cs — Punto de entrada de la aplicación ASP.NET Core
// Configura: servicios DI, Entity Framework, Cookie Auth,
// middleware pipeline y enrutamiento MVC
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Gimnasio.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════
// CONFIGURACIÓN DE SERVICIOS
// ═══════════════════════════════════════════════════════════

// Cargar credenciales del admin desde appsettings.json
builder.Services.Configure<AdminSettings>(
    builder.Configuration.GetSection("AdminSettings"));

// Cargar configuración de Email (Gmail SMTP)
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Cargar configuración de WhatsApp Business (Meta Cloud API)
builder.Services.Configure<WhatsAppSettings>(
    builder.Configuration.GetSection("WhatsAppSettings"));

// Registrar Entity Framework Core con SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
    .ConfigureWarnings(w => w.Log(
        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
);

// Registrar servicios de la aplicación (patrón interfaz + implementación)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INegocioService, NegocioService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISugerenciaService, SugerenciaService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<IVendedorService, VendedorService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Servicios del modelo artesanal (citas, empleados, servicios)
builder.Services.AddScoped<IServicioNegocioService, ServicioNegocioService>();
builder.Services.AddScoped<IEmpleadoService, EmpleadoService>();
builder.Services.AddScoped<ICitaService, CitaService>();

// Registrar cliente HTTP para WhatsApp
builder.Services.AddHttpClient("WhatsApp");

// Registrar servicios de fondo (recordatorios WhatsApp + resumen diario)
builder.Services.AddBackgroundServices();

// Registrar MVC con vistas + protección CSRF global en POST/PUT/DELETE
// En Development se desactiva CSRF para permitir testing con curl/agents
builder.Services.AddControllersWithViews(options =>
{
    if (!builder.Environment.IsDevelopment())
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Configurar rate limiting para endpoints sensibles (login, leads)
// En Development se aumenta el límite para testing con múltiples agentes
var isDev = builder.Environment.IsDevelopment();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isDev ? 500 : 5,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.AddPolicy("lead", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isDev ? 500 : 3,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// Configurar autenticación por cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Negocios/Login";
        options.Cookie.Name = "NegocioAuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

var app = builder.Build();

// ═══════════════════════════════════════════════════════════
// AUTO-MIGRACIÓN DE BASE DE DATOS
// ═══════════════════════════════════════════════════════════

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        db.Database.Migrate();
        logger.LogInformation("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed. App will start but DB may be out of sync.");
    }
}

// ═══════════════════════════════════════════════════════════
// PIPELINE DE MIDDLEWARE
// ═══════════════════════════════════════════════════════════

// Forwarded headers (necesario detrás de Nginx para HTTPS, HSTS y cookies seguras)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Cabeceras de seguridad
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdn.datatables.net https://cdnjs.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdn.datatables.net https://fonts.googleapis.com https://cdnjs.cloudflare.com; " +
        "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' https://cdn.datatables.net https://cdn.jsdelivr.net; " +
        "frame-ancestors 'none';";
    await next();
});

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
app.UseRateLimiter();
app.UseRouting();

// Autenticación antes de autorización (orden importa)
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Ruta por defecto: HomeController.Index
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
