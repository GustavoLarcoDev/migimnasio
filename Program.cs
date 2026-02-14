// ═══════════════════════════════════════════════════════════
// Program.cs — Punto de entrada de la aplicación ASP.NET Core
// Configura: servicios DI, Entity Framework, Cookie Auth,
// middleware pipeline y enrutamiento MVC
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Gimnasio.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════
// CONFIGURACIÓN DE SERVICIOS
// ═══════════════════════════════════════════════════════════

// Cargar credenciales del admin desde appsettings.json
builder.Services.Configure<AdminSettings>(
    builder.Configuration.GetSection("AdminSettings"));

// Registrar Entity Framework Core con SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Registrar servicios de la aplicación (patrón interfaz + implementación)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INegocioService, NegocioService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISugerenciaService, SugerenciaService>();

// Registrar MVC con vistas
builder.Services.AddControllersWithViews();

// Configurar autenticación por cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Negocios/Login";
        options.Cookie.Name = "NegocioAuthCookie";
    });

var app = builder.Build();

// ═══════════════════════════════════════════════════════════
// PIPELINE DE MIDDLEWARE
// ═══════════════════════════════════════════════════════════

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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
