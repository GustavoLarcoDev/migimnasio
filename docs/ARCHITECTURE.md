# Arquitectura

## Stack Tecnológico

| Capa | Tecnología |
|------|-----------|
| Backend | ASP.NET Core 9.0 (MVC) |
| ORM | Entity Framework Core 9.0 |
| Base de datos | SQL Server |
| Autenticación | Cookie Authentication + BCrypt |
| Frontend | Razor Views + Bootstrap 5 + Metronic v8 CSS |
| Tablas | DataTables 2.3.6 |
| Gráficos | ApexCharts |
| Notificaciones UI | Toastr + SweetAlert2 |
| Excel | ClosedXML |

## Diagrama de Capas

```
┌─────────────────────────────────────────┐
│              Views (Razor)              │
│  Dashboard, Login, Admin, Landing Page  │
├─────────────────────────────────────────┤
│            Controllers (6)              │
│  Auth, Admin, Clientes, Logs,           │
│  Ventas, Notificaciones                 │
├─────────────────────────────────────────┤
│           Services (6 pares)            │
│  IAuthService, IGimnasioService,        │
│  IClienteService, ILogService,          │
│  IVentasService, INotificationService   │
├─────────────────────────────────────────┤
│         EF Core DbContext               │
│  Gimnasios, Clientes, Logs,             │
│  Notificaciones                         │
├─────────────────────────────────────────┤
│           SQL Server                    │
└─────────────────────────────────────────┘
```

## Flujo de Autenticación

```
Usuario → POST /Gimnasios/Login
                │
                ▼
          AuthService.LoginAsync()
                │
                ├─ ¿Es admin? → Verificar AdminSettings (appsettings.json)
                │                 → Cookie con Claim "Admin"
                │                 → Redirect /Gimnasios/Index
                │
                └─ ¿Es gimnasio? → Buscar por email en DB
                                  → Verificar BCrypt hash
                                  │   ├─ Match → OK
                                  │   └─ No match → Intentar texto plano
                                  │       └─ Match → Auto-migrar a BCrypt
                                  → Cookie con Claims (GimnasioId, Nombre)
                                  → Redirect /Gimnasios/{id}/Dashboard
```

### Lazy Migration de Passwords (BCrypt)

Las contraseñas existentes en texto plano se migran automáticamente a BCrypt hash en el siguiente login exitoso:

1. `AuthService.LoginAsync()` recibe email + password
2. Busca el gimnasio en DB por email
3. Intenta `BCrypt.Verify(password, hash)`
4. Si falla, compara como texto plano: `password == storedPassword`
5. Si texto plano coincide → genera BCrypt hash y actualiza en DB
6. Próximo login usará BCrypt directamente

## Sistema de Notificaciones

```
Dashboard carga → POST /Gimnasios/GenerarNotificaciones
                        │
                        ▼
              NotificationService.GenerarNotificacionesAsync()
                        │
                        ├─ Buscar clientes con FechaFin en próximos 3 días
                        ├─ Verificar que no exista notificación duplicada (mismo cliente, mismo día)
                        └─ Crear Notificacion por cada cliente próximo a vencer

UI:
  ├─ Campana en topbar → GET /GetNotificacionesCount → badge con número
  ├─ Dropdown campana → GET /GetNotificaciones → lista de notificaciones
  ├─ Tab Notificaciones → misma lista, vista expandida
  └─ Marcar leída → POST /MarcarNotificacionLeida
```

## Inyección de Dependencias (DI)

Registrado en `Program.cs`:

```csharp
builder.Services.Configure<AdminSettings>(config.GetSection("AdminSettings"));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGimnasioService, GimnasioService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
```

Todos los servicios son `Scoped` (una instancia por request HTTP).

## Enrutamiento

Todos los controladores usan `[Route("Gimnasios")]` para mantener compatibilidad con las URLs originales del frontend JavaScript. Esto permite que el mismo path `/Gimnasios/...` sea manejado por diferentes controladores según la acción.

## Design System (Metronic v8)

CSS Variables principales (`wwwroot/css/metronic-style.css`):

| Variable | Valor | Uso |
|----------|-------|-----|
| `--mg-primary` | `#3E97FF` | Botones, links, sidebar activo |
| `--mg-success` | `#50CD89` | Estados activos, badges positivos |
| `--mg-warning` | `#FFC700` | Alertas, estado prueba |
| `--mg-danger` | `#F1416C` | Errores, eliminación, vencidos |
| `--mg-dark` | `#181C32` | Sidebar background |

Componentes: `mg-sidebar`, `mg-topbar`, `mg-card`, `mg-stat-card`, `mg-bell`, badges light (`badge-light-*`), botones light (`btn-light-*`).
