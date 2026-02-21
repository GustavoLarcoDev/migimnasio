// ═══════════════════════════════════════════════════════════
// Program.cs — Punto de entrada de la aplicación ASP.NET Core
//
// Este archivo es el corazón de la aplicación. ASP.NET Core 9
// usa el patrón "minimal hosting" donde todo se configura aquí
// en dos fases bien definidas:
//
//   FASE 1 — Registro de servicios en el contenedor de DI
//             (todo lo que va antes de builder.Build())
//
//   FASE 2 — Construcción del pipeline de middleware
//             (todo lo que va después de app = builder.Build())
//
// Piénsalo así: la FASE 1 define QUÉ servicios existen,
// la FASE 2 define EN QUÉ ORDEN se procesan los requests HTTP.
// ═══════════════════════════════════════════════════════════

using Gimnasio.Data;
using Gimnasio.Models;
using Gimnasio.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;

// WebApplication.CreateBuilder prepara el contenedor de DI y la
// configuración (appsettings.json, variables de entorno, etc.)
var builder = WebApplication.CreateBuilder(args);

// SEGURIDAD: Ocultar la cabecera "Server: Kestrel" de las respuestas HTTP.
// Esto evita revelar la tecnología del servidor durante un reconocimiento.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// ═══════════════════════════════════════════════════════════
// SECCIÓN 1 — CONFIGURACIÓN TIPADA (Options Pattern)
//
// En lugar de leer cadenas de texto con Configuration["clave"],
// usamos clases POCO fuertemente tipadas. El contenedor de DI
// inyecta IOptions<T> donde se necesite, con validación en
// tiempo de compilación y soporte para recarga en caliente.
// ═══════════════════════════════════════════════════════════

// Credenciales del súper-admin (email + contraseña) definidas
// en appsettings.json bajo la sección "AdminSettings".
// Nunca se escriben en el código fuente (secreto en config).
builder.Services.AddOptions<AdminSettings>()
    .BindConfiguration("AdminSettings")
    .Validate(s => !string.IsNullOrWhiteSpace(s.Email), "AdminSettings:Email is required")
    .Validate(s => !string.IsNullOrWhiteSpace(s.PasswordHash), "AdminSettings:PasswordHash is required")
    .ValidateOnStart();

// Configuración del servidor SMTP de Gmail para envío de emails
// (host, puerto, credenciales). Separado del admin para poder
// cambiar el proveedor de email sin tocar la lógica de negocio.
builder.Services.AddOptions<EmailSettings>()
    .BindConfiguration("EmailSettings")
    .Validate(s => !string.IsNullOrWhiteSpace(s.SmtpHost), "EmailSettings:SmtpHost is required")
    .ValidateOnStart();

// Credenciales de la Meta Cloud API (WhatsApp Business):
// token de acceso, número de teléfono, ID de plantillas, etc.
builder.Services.Configure<WhatsAppSettings>(
    builder.Configuration.GetSection("WhatsAppSettings"));

// ═══════════════════════════════════════════════════════════
// SECCIÓN 2 — BASE DE DATOS (Entity Framework Core)
//
// AddDbContext registra ApplicationDbContext como servicio
// "Scoped", es decir, una instancia por request HTTP. EF Core
// mapea nuestras clases C# a tablas SQL Server a través del
// ORM, evitando escribir SQL a mano.
// ═══════════════════════════════════════════════════════════

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        // La cadena de conexión vive en appsettings.json →
        // "ConnectionStrings": { "DefaultConnection": "..." }
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
    // PendingModelChangesWarning detecta cuando el modelo C# difiere
    // de las migraciones. En Development lanzamos excepción para que
    // el desarrollador no pueda ignorar el problema (crash inmediato).
    // En producción solo logueamos para no bloquear un deploy de emergencia.
    .ConfigureWarnings(w =>
    {
        if (builder.Environment.IsDevelopment())
            w.Throw(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
        else
            w.Log(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
    })
);

// ═══════════════════════════════════════════════════════════
// SECCIÓN 3 — SERVICIOS DE NEGOCIO (patrón interfaz + impl)
//
// Registramos cada servicio con su interfaz como "Scoped":
//   - Una instancia nueva POR REQUEST HTTP
//   - El código depende de la interfaz (IXService), no de la
//     implementación concreta → facilita testing con mocks
//   - Evitar "Singleton" aquí porque los servicios usan
//     ApplicationDbContext que también es Scoped
// ═══════════════════════════════════════════════════════════

// Autenticación: login, logout, verificación de contraseña BCrypt
builder.Services.AddScoped<IAuthService, AuthService>();

// CRUD y lógica de negocios (Gym): crear, editar, activar, etc.
builder.Services.AddScoped<INegocioService, NegocioService>();

// Registro inmutable de transacciones financieras (logs contables)
builder.Services.AddScoped<ILogService, LogService>();

// Gestión de clientes de cada negocio: membresías, estados, búsqueda
builder.Services.AddScoped<IClienteService, ClienteService>();

// Módulo de ventas: registrar cobros, historial, totales
builder.Services.AddScoped<IVentasService, VentasService>();

// Notificaciones automáticas: detecta vencimientos próximos
// y genera alertas para que el negocio contacte al cliente
builder.Services.AddScoped<INotificationService, NotificationService>();

// Sugerencias y feedback de los negocios hacia la plataforma
builder.Services.AddScoped<ISugerenciaService, SugerenciaService>();

// Envío de mensajes de WhatsApp vía Meta Cloud API
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();

// Control de stock de productos: alta, baja, movimientos
builder.Services.AddScoped<IInventarioService, InventarioService>();

// Gestión de vendedores y sus comisiones
builder.Services.AddScoped<IVendedorService, VendedorService>();

// Comisiones generadas para vendedores al crear negocios calificados
builder.Services.AddScoped<IComisionService, ComisionService>();

// Recibos de pago almacenados en BD con HTML del email
builder.Services.AddScoped<IReciboService, ReciboService>();

// Envío de emails transaccionales (bienvenida, recordatorios, etc.)
builder.Services.AddScoped<IEmailService, EmailService>();

// ── Modelo Artesanal ────────────────────────────────────────
// El "modelo artesanal" es la modalidad de negocios basados en
// citas (peluquerías, spas, estudios, etc.) a diferencia del
// modelo de membresías (gimnasios). Tiene su propio conjunto
// de servicios para gestionar empleados, horarios y citas.

// Catálogo de servicios que ofrece el negocio (corte, masaje, etc.)
builder.Services.AddScoped<IServicioNegocioService, ServicioNegocioService>();

// Alta y gestión de empleados: horarios, disponibilidad, excepciones
builder.Services.AddScoped<IEmpleadoService, EmpleadoService>();

// Agenda de citas: crear, mover, cancelar, confirmar, cobrar
builder.Services.AddScoped<ICitaService, CitaService>();

// ── Modelo Tienda (POS / Catálogo) ──────────────────────────
// Punto de venta con órdenes, detalles, recibos y catálogo público.
builder.Services.AddScoped<IVentaProductoService, VentaProductoService>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();

// ── Modelo Restaurante (Mesas / Menús) ──────────────────────
// Gestión de mesas y menús digitales con 5 estilos visuales.
builder.Services.AddScoped<IMesaService, MesaService>();
builder.Services.AddScoped<IMenuRestauranteService, MenuRestauranteService>();

// ═══════════════════════════════════════════════════════════
// SECCIÓN 4 — CLIENTE HTTP (HttpClientFactory)
//
// AddHttpClient registra un HttpClient nombrado "WhatsApp".
// Usar IHttpClientFactory en lugar de "new HttpClient()" evita
// el agotamiento de sockets (socket exhaustion) al reutilizar
// conexiones HTTP de forma eficiente entre requests.
// ═══════════════════════════════════════════════════════════

builder.Services.AddHttpClient("WhatsApp");

// ═══════════════════════════════════════════════════════════
// SECCIÓN 5 — SERVICIOS DE FONDO (IHostedService)
//
// Los IHostedService corren en background threads separados del
// ciclo request/response. Se registran como Singleton internamente
// por el runtime de .NET. Son perfectos para tareas programadas
// (schedulers) que no dependen de un request HTTP entrante.
//
// Los tres servicios registrados en AddBackgroundServices():
//   - MembershipReminderService : WhatsApp a las 8:00 AM con
//     recordatorios de membresías próximas a vencer
//   - DailyReportService        : WhatsApp a las 9:00 PM con
//     resumen diario de cobros y nuevos clientes
//   - AppointmentReminderService: WhatsApp/SMS de recordatorio
//     de citas próximas (modelo artesanal)
// ═══════════════════════════════════════════════════════════

builder.Services.AddBackgroundServices();

// ═══════════════════════════════════════════════════════════
// SECCIÓN 6 — MVC + PROTECCIÓN CSRF
//
// AddControllersWithViews habilita el patrón MVC completo:
// Controllers que retornan Views Razor (.cshtml).
//
// AutoValidateAntiforgeryTokenAttribute agrega protección CSRF
// global: cualquier POST/PUT/DELETE que no traiga el token
// antiforgery válido es rechazado con 400 Bad Request.
// En Development se desactiva para facilitar pruebas con
// herramientas como curl, Postman o agentes automáticos.
// ═══════════════════════════════════════════════════════════

builder.Services.AddControllersWithViews(options =>
{
    // CSRF global: protege todos los POST/PUT/DELETE contra ataques CSRF.
    // Habilitado en TODOS los entornos (incluyendo desarrollo) para evitar
    // que un deploy accidental con ASPNETCORE_ENVIRONMENT=Development
    // deje los endpoints sin protección. Usar [IgnoreAntiforgeryToken]
    // en endpoints específicos si es necesario para testing.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
}).AddJsonOptions(jsonOpts =>
{
    // Evitar errores 500 por referencias circulares en navigation properties
    // (ej: CategoriaProducto → Productos → Producto.Categoria → ciclo infinito)
    jsonOpts.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// ═══════════════════════════════════════════════════════════
// SECCIÓN 7 — RATE LIMITING (límite de velocidad)
//
// Protege endpoints críticos de ataques de fuerza bruta y
// abuso automatizado. Usamos la ventana fija (Fixed Window):
// se permite un máximo de N requests por minuto por IP.
//
// Si el límite se supera → responde 429 Too Many Requests.
//
// En Development el límite es 500 req/min para no bloquear
// a los desarrolladores durante pruebas intensivas.
// En Production el límite es mucho más restrictivo.
//
// Políticas definidas:
//   "login" → aplicada al endpoint de inicio de sesión
//   "lead"  → aplicada al formulario de captura de leads
// ═══════════════════════════════════════════════════════════

// Guardamos el flag de entorno para reutilizarlo dentro del lambda
var isDev = builder.Environment.IsDevelopment();

builder.Services.AddRateLimiter(options =>
{
    // Código HTTP de respuesta cuando se supera el límite
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Política para el endpoint de login (ataque de credenciales)
    // Dev: 500/min → permite pruebas; Prod: 5/min → bloquea brute-force
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            // La clave de partición es la IP del cliente.
            // Cada IP tiene su propio contador independiente.
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isDev ? 500 : 5,      // requests permitidos
                Window = TimeSpan.FromMinutes(1)     // por ventana de 1 minuto
            }));

    // Política para el formulario de leads (spam de formularios)
    // Dev: 500/min → sin restricción; Prod: 3/min → anti-spam
    options.AddPolicy("lead", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isDev ? 500 : 3,      // más restrictivo que login
                Window = TimeSpan.FromMinutes(1)
            }));
});

// ═══════════════════════════════════════════════════════════
// SECCIÓN 8 — AUTENTICACIÓN POR COOKIES
//
// HTTP es un protocolo sin estado (stateless). Para que el
// usuario no tenga que loguearse en cada request, usamos una
// cookie de sesión firmada criptográficamente. ASP.NET Core
// verifica automáticamente la firma en cada request entrante.
//
// Flujo:
//   1. Usuario hace POST /Negocios/Login con credenciales
//   2. AuthService valida y crea un ClaimsPrincipal
//   3. ASP.NET Core serializa los claims, los cifra y los
//      devuelve como cookie "NegocioAuthCookie" al browser
//   4. El browser envía esa cookie en cada request futuro
//   5. ASP.NET Core la descifra y llena HttpContext.User
// ═══════════════════════════════════════════════════════════

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Si un usuario no autenticado accede a una ruta protegida
        // [Authorize], se redirige aquí en lugar de mostrar 401
        options.LoginPath = "/Negocios/Login";

        // Nombre de la cookie en el browser del usuario
        options.Cookie.Name = "NegocioAuthCookie";

        // HttpOnly = true: la cookie NO es accesible desde JavaScript.
        // Esto previene robo de sesión vía ataques XSS, ya que el
        // código malicioso en la página no puede leer la cookie.
        options.Cookie.HttpOnly = true;

        // En desarrollo aceptamos HTTP (para localhost sin TLS).
        // En producción la cookie SOLO viaja por HTTPS, nunca
        // por HTTP plano (donde podría ser interceptada).
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest   // dev: HTTP o HTTPS
            : CookieSecurePolicy.Always;         // prod: HTTPS obligatorio

        // SameSite.Strict: la cookie NUNCA se envía en requests
        // que originen de otro dominio. Protege contra CSRF
        // (Cross-Site Request Forgery) a nivel de browser.
        options.Cookie.SameSite = SameSiteMode.Strict;

        // SEGURIDAD: Expiración de sesión — la cookie expira después de 12 horas.
        // SlidingExpiration renueva el timer en cada request activo del usuario,
        // por lo que solo expira tras 12h de inactividad completa.
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
    });

// ═══════════════════════════════════════════════════════════
// FIN DE LA FASE 1 — Build y arranque del pipeline
//
// builder.Build() congela el contenedor de DI: a partir de
// aquí NO se pueden registrar más servicios. Todo lo que
// sigue configura el pipeline de middleware (FASE 2).
// ═══════════════════════════════════════════════════════════

var app = builder.Build();

// ═══════════════════════════════════════════════════════════
// SECCIÓN 9 — AUTO-MIGRACIÓN DE BASE DE DATOS
//
// Al arrancar la aplicación, aplicamos automáticamente
// cualquier migración de EF Core pendiente. Esto garantiza
// que el esquema de la BD siempre esté sincronizado con el
// modelo C# sin intervención manual.
//
// Por qué un scope temporal: ApplicationDbContext es Scoped
// (una instancia por request). Fuera del pipeline de requests
// no existe un scope activo, así que creamos uno manualmente
// solo para esta operación de inicio.
//
// FAIL-FAST: Si la migración falla, la app NO arranca.
// Esto es INTENCIONAL. El problema del 2026-02-17 (login roto
// en producción) ocurrió porque la app arrancó con un schema
// de BD desincronizado. Es preferible que la app no arranque
// (systemd lo detecta como crash) a que arranque rota.
//
// El script deploy.sh tiene rollback automático: si la app
// no arranca en 5 segundos, vuelve al release anterior.
// ═══════════════════════════════════════════════════════════

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        // Verificar y loguear migraciones pendientes antes de aplicarlas
        var pendientes = db.Database.GetPendingMigrations().ToList();
        if (pendientes.Any())
        {
            logger.LogWarning("Aplicando {Count} migraciones pendientes: {Migrations}",
                pendientes.Count, string.Join(", ", pendientes));
        }

        // Aplica todas las migraciones pendientes a la BD.
        // Si no hay migraciones pendientes, es un no-op (sin costo).
        db.Database.Migrate();
        logger.LogInformation("Migraciones aplicadas correctamente.");
    }
    catch (Exception ex)
    {
        // FAIL-FAST: NO arrancar con una BD desincronizada.
        // systemd detectará el crash y el deploy.sh hará rollback automático.
        logger.LogCritical(ex, "FALLO CRÍTICO: La migración de BD falló. DETENIENDO LA APP.");
        Environment.Exit(1);
    }
}

// ═══════════════════════════════════════════════════════════
// SECCIÓN 10 — PIPELINE DE MIDDLEWARE (FASE 2)
//
// Los middlewares se ejecutan en ORDEN ESTRICTO para cada
// request entrante y en ORDEN INVERSO para la respuesta.
// El orden aquí importa muchísimo: por ejemplo, la
// autenticación DEBE ir antes de la autorización.
//
// Flujo simplificado de un request:
//
//   Request →  [ForwardedHeaders]
//           →  [ExceptionHandler / HSTS]
//           →  [SecurityHeaders]
//           →  [HttpsRedirection]
//           →  [RateLimiter]
//           →  [Routing]
//           →  [Authentication]     ← identifica al usuario
//           →  [Authorization]      ← verifica permisos
//           →  [StaticFiles]
//           →  [Controller Action]
//   Response ← (orden inverso)
// ═══════════════════════════════════════════════════════════

// ── Forwarded Headers ──────────────────────────────────────
// La app corre detrás de Nginx (reverse proxy en producción).
// Sin esto, HttpContext.Connection.RemoteIpAddress sería siempre
// la IP de Nginx (127.0.0.1) en lugar de la IP real del cliente.
// XForwardedFor  → IP real del cliente
// XForwardedProto→ protocolo real (https), necesario para
//                  que UseHttpsRedirection y las cookies
//                  seguras funcionen correctamente
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // Solo confiar en proxies de loopback (Nginx corriendo en el mismo servidor).
    // Sin esta restricción, un atacante podría falsificar X-Forwarded-For para evadir rate limiting.
    KnownProxies = { System.Net.IPAddress.Loopback, System.Net.IPAddress.IPv6Loopback }
});

// ── Manejo de errores + HSTS ───────────────────────────────
if (!app.Environment.IsDevelopment())
{
    // En producción, los errores no controlados redirigen a
    // /Home/Error en lugar de mostrar el stack trace al usuario
    app.UseExceptionHandler("/Home/Error");

    // HSTS (HTTP Strict Transport Security): le dice al browser
    // que NUNCA use HTTP para este dominio, solo HTTPS.
    // Protege contra ataques de downgrade de protocolo.
    // Solo en producción porque en dev usamos HTTP localmente.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

// ── Cabeceras de Seguridad HTTP ────────────────────────────
// Añadimos cabeceras de seguridad estándar a CADA respuesta.
// No son manejadas por ningún middleware built-in de ASP.NET Core,
// así que usamos un middleware inline (lambda).
app.Use(async (context, next) =>
{
    // Evita que el browser "adivine" el tipo de contenido
    // de un archivo (MIME sniffing), que puede ejecutar
    // archivos maliciosos como si fueran HTML/JavaScript
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";

    // Impide que la app se incruste en un <iframe> de otro
    // sitio, previniendo ataques de clickjacking
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";

    // Activa el filtro XSS del browser (modo bloqueo).
    // Principalmente para browsers antiguos; los modernos
    // usan Content-Security-Policy en su lugar
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

    // Controla cuánta información de la URL actual se envía
    // en la cabecera Referer al navegar a otro sitio
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    // Deshabilita explícitamente el acceso a hardware sensible
    // del dispositivo que la app no necesita usar
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

    // Content-Security-Policy (CSP): la política más importante.
    // Define exactamente de qué orígenes puede cargar recursos
    // el browser (scripts, estilos, fuentes, imágenes, etc.).
    // Cualquier recurso de un origen no listado es bloqueado,
    // previniendo ataques XSS que inyecten scripts externos.
    context.Response.Headers["Content-Security-Policy"] =
        // Por defecto: solo recursos del propio dominio ('self')
        "default-src 'self'; " +

        // Scripts: self + inline/eval (necesario para Razor y libs)
        // + CDNs de las librerías JS que usamos (Bootstrap, DataTables,
        // ApexCharts, Toastr, SweetAlert2, etc.)
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdn.datatables.net https://cdnjs.cloudflare.com; " +

        // Estilos: self + inline (necesario para Metronic) + CDNs de CSS
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdn.datatables.net https://fonts.googleapis.com https://cdnjs.cloudflare.com; " +

        // Fuentes tipográficas: self + CDN de Metronic + Google Fonts
        "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com; " +

        // Imágenes: self + data: URIs (para íconos base64) + cualquier HTTPS
        "img-src 'self' data: https:; " +

        // Fetch/XHR: self + CDNs de DataTables (para carga lazy de datos)
        "connect-src 'self' https://cdn.datatables.net https://cdn.jsdelivr.net; " +

        // Solo el propio dominio puede incrustar la app en iframes (catálogo, recibos)
        "frame-ancestors 'self';";

    // Pasar el control al siguiente middleware en el pipeline
    await next();
});

// ── HTTPS Redirection ──────────────────────────────────────
// Redirige automáticamente HTTP → HTTPS (301 permanente).
// Se salta en producción porque Nginx ya maneja la redirección
// HTTP→HTTPS antes de que el request llegue a la app .NET.
// Aquí solo se activa en entornos no-producción (staging, dev).
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

// ── Rate Limiter ───────────────────────────────────────────
// Activa las políticas de rate limiting definidas en la SECCIÓN 7.
// Los controllers usan [EnableRateLimiting("login")] o
// [EnableRateLimiting("lead")] para aplicar la política correcta.
app.UseRateLimiter();

// ── Routing ────────────────────────────────────────────────
// Analiza la URL del request y selecciona el endpoint
// (controller + action) que debe manejarlo. Debe ir ANTES
// de Authentication/Authorization para que los middlewares
// de auth puedan inspeccionar el endpoint seleccionado y
// sus atributos [Authorize], [AllowAnonymous], etc.
app.UseRouting();

// ── Autenticación y Autorización ──────────────────────────
// ORDEN CRÍTICO: Autenticación SIEMPRE antes de Autorización.
//
// UseAuthentication: lee la cookie "NegocioAuthCookie", la
// descifra y puebla HttpContext.User con los claims del usuario.
// Si la cookie no existe o es inválida, User.Identity.IsAuthenticated = false.
//
// UseAuthorization: verifica si el usuario autenticado tiene
// permiso para acceder al endpoint seleccionado. Si un
// endpoint tiene [Authorize] y el usuario no está autenticado,
// redirige a LoginPath definida en la SECCIÓN 8.
app.UseAuthentication();
app.UseAuthorization();

// ── Archivos Estáticos ─────────────────────────────────────
// Sirve archivos de wwwroot/ (CSS, JS, imágenes, fuentes).
// MapStaticAssets es la versión optimizada de ASP.NET Core 9
// que incluye fingerprinting (cache-busting) y compresión.
app.MapStaticAssets();

// ── Enrutamiento MVC ───────────────────────────────────────
// Define la ruta por defecto para todos los controllers.
// Patrón: /Controller/Action/Id (los tres son opcionales).
// Sin ruta específica → HomeController.Index().
// WithStaticAssets vincula la resolución de rutas con los
// assets estáticos versionados (para links en Razor views).
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ═══════════════════════════════════════════════════════════
// HEALTH CHECK — Endpoint de verificación de salud
//
// deploy.sh usa este endpoint para verificar que la app está
// funcionando correctamente después de un deploy. Si devuelve
// HTTP 503, el script de deploy hace rollback automáticamente
// a la versión anterior.
//
// También es útil para monitoreo externo (UptimeRobot, etc.).
// ═══════════════════════════════════════════════════════════

app.MapGet("/health", async (ApplicationDbContext db) =>
{
    try
    {
        // Verificar que la base de datos responde
        var canConnect = await db.Database.CanConnectAsync();
        if (!canConnect)
            return Results.Problem("Base de datos inaccesible", statusCode: 503);

        // Verificar que no hay migraciones pendientes (schema sincronizado)
        var pendientes = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pendientes.Any())
            return Results.Problem(
                "Base de datos con schema desincronizado",
                statusCode: 503);

        return Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            database = "connected",
            pendingMigrations = 0
        });
    }
    catch (Exception)
    {
        return Results.Problem("Error interno del servidor", statusCode: 503);
    }
}).AllowAnonymous();

// ═══════════════════════════════════════════════════════════
// ARRANQUE DEL SERVIDOR
// app.Run() bloquea el hilo principal, pone el servidor en
// escucha y procesa requests hasta que la app se apague.
// ═══════════════════════════════════════════════════════════

app.Run();
