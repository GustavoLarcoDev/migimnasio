# Project Structure

```
Gimnasio/
├── Controllers/
│   ├── AuthController.cs              # Login/logout, cookie authentication (public)
│   ├── AdminController.cs             # Admin panel: CRUD negocios, stats, impersonation, Excel export
│   ├── ClientesController.cs          # Client CRUD, membership renewal, Excel import/export, dashboard view
│   ├── LogsController.cs              # Financial log CRUD, Excel export
│   ├── VentasController.cs            # Sales statistics and chart data endpoints
│   ├── NotificacionesController.cs    # Auto-generated expiry notifications, mark as read
│   ├── InventarioController.cs        # Product inventory: CRUD, sell, return, restock, adjust, Excel export
│   ├── SugerenciasController.cs       # Feedback/suggestions from negocios to admin
│   └── HomeController.cs             # Public landing page and error handling
│
├── Data/
│   └── ApplicationDbContext.cs        # EF Core DbContext (9 DbSets)
│
├── Migrations/                        # EF Core migrations (chronological)
│   ├── 20260124081114_Initial.cs
│   ├── 20260201070042_AddLogsTable.cs
│   ├── 20260207083520_AddClienteFieldsToLogs.cs
│   ├── 20260207090955_AddNotificaciones.cs
│   ├── 20260210005604_RenameGimnasioToNegocio.cs
│   ├── 20260214091220_AddSugerencias.cs
│   ├── 20260214091241_AddSubscriptionTracking.cs
│   ├── 20260214101849_AddAdminLogs.cs
│   ├── 20260214111123_AddWhatsAppFieldsToNegocio.cs
│   ├── 20260214111623_RemoveWhatsAppFieldsFromNegocio.cs
│   ├── 20260214193230_AddInventario.cs
│   └── ApplicationDbContextModelSnapshot.cs
│
├── Models/
│   ├── Negocio.cs                     # Gym class — main business entity (subscription, state, relations)
│   ├── Cliente.cs                     # Client entity + ClienteCreateModel (legacy)
│   ├── Logs.cs                        # Financial log entity + LogCreateModel (legacy)
│   ├── Notificacion.cs                # Auto-generated expiry notification
│   ├── Sugerencia.cs                  # Feedback/suggestion from negocio to admin
│   ├── AdminLog.cs                    # Admin action audit log
│   ├── Producto.cs                    # Inventory product (soft-delete, stock tracking)
│   ├── MovimientoInventario.cs        # Immutable inventory movement audit trail
│   ├── Vendedor.cs                    # Vendedor (salesperson) entity
│   ├── AdminSettings.cs               # POCO for admin credentials (from appsettings.json)
│   ├── WhatsAppSettings.cs            # POCO for WhatsApp Cloud API config
│   ├── ErrorViewModel.cs              # Standard error view model
│   └── DTOs/
│       ├── LoginDto.cs                # Login form data
│       ├── ClienteCreateDto.cs        # Client create/edit (supports FechaFin or FechaInicio+Dias)
│       ├── RenovarClienteDto.cs       # Membership renewal
│       ├── NegocioCreateDto.cs        # Negocio creation
│       ├── NegocioEditDto.cs          # Negocio editing (password optional)
│       ├── LogCreateDto.cs            # Manual financial log creation
│       └── ProductoCreateDto.cs       # Product create/edit
│
├── Services/
│   ├── IAuthService.cs                # Interface: login, role checks, claim extraction
│   ├── AuthService.cs                 # BCrypt login with lazy plaintext migration
│   ├── INegocioService.cs             # Interface: negocio CRUD, admin stats, Excel export
│   ├── NegocioService.cs              # Business CRUD, admin dashboard stats, admin logs
│   ├── IClienteService.cs             # Interface: client CRUD, Excel import/export
│   ├── ClienteService.cs              # Client CRUD, stats, daily clients, Excel operations
│   ├── ILogService.cs                 # Interface: financial log CRUD, Excel export
│   ├── LogService.cs                  # Financial log management, period closing
│   ├── IVentasService.cs              # Interface: sales statistics, chart data
│   ├── VentasService.cs               # Sales stats aggregation, chart data formatting
│   ├── INotificationService.cs        # Interface: notification generation and management
│   ├── NotificationService.cs         # Auto-generates expiry notifications (3-day window)
│   ├── ISugerenciaService.cs          # Interface: suggestion CRUD
│   ├── SugerenciaService.cs           # Suggestion creation and admin management
│   ├── IWhatsAppService.cs            # Interface: WhatsApp message sending
│   ├── WhatsAppService.cs             # WhatsApp Cloud API integration (Meta)
│   ├── IInventarioService.cs          # Interface: product inventory operations
│   ├── InventarioService.cs           # Product CRUD, sell/return/restock/adjust, Excel export
│   ├── MembershipReminderService.cs   # Background: daily WhatsApp reminders at 8:00 AM (Ecuador)
│   ├── DailyReportService.cs          # Background: daily summary WhatsApp at 9:00 PM (Ecuador)
│   └── BackgroundServicesRegistration.cs # Extension method to register background services
│
├── Views/
│   ├── _ViewImports.cshtml            # Global Razor imports and tag helpers
│   ├── _ViewStart.cshtml              # Default layout assignment
│   ├── Home/
│   │   ├── Index.cshtml               # Public landing page
│   │   └── Privacy.cshtml             # Privacy policy page
│   ├── Negocios/
│   │   ├── Login.cshtml               # Login page (email + password)
│   │   ├── Dashboard.cshtml           # Negocio dashboard (tabs: resumen, clientes, logs, ventas, inventario, sugerencias, notificaciones)
│   │   ├── Index.cshtml               # Admin dashboard (tabs: resumen, negocios, crear, ventas, logs, sugerencias)
│   │   ├── Create.cshtml              # Public business registration page
│   │   └── Sugerencias.cshtml         # Admin suggestion management view
│   └── Shared/
│       ├── _Layout.cshtml             # Public layout (landing, auth pages)
│       ├── _AuthLayout.cshtml         # Authentication pages layout
│       ├── _DashboardLayout.cshtml    # Negocio dashboard layout (sidebar + topbar + theme switcher)
│       ├── _AdminDashboardLayout.cshtml # Admin dashboard layout
│       ├── _ValidationScriptsPartial.cshtml # Client-side validation scripts
│       └── Error.cshtml               # Error page
│
├── wwwroot/
│   ├── css/
│   │   ├── site.css                   # Base site styles
│   │   └── metronic-style.css         # Full Metronic v8 design system (4 themes, CSS vars)
│   ├── js/
│   │   └── site.js                    # Base site JavaScript
│   └── lib/                           # Third-party libraries (Bootstrap, jQuery)
│
├── Program.cs                         # Entry point: DI registration, EF Core, cookie auth, middleware
├── appsettings.json                   # Configuration (DB connection, AdminSettings, WhatsAppSettings)
├── appsettings.Development.json       # Development overrides
├── Gimnasio.csproj                    # .NET 9.0 project file with NuGet packages
│
├── docs/
│   ├── PROJECT_STRUCTURE.md           # This file — full project tree
│   ├── API_ENDPOINTS.md               # All HTTP endpoints documentation
│   ├── SETUP.md                       # Setup and installation guide
│   └── ARCHITECTURE.md                # Architecture overview and design decisions
│
└── README.md                          # Project README
```

## Application Layers

1. **Controllers** (9) — Receive HTTP requests, validate authentication/authorization, delegate to services. All business controllers use `[Route("Negocios")]`.
2. **Services** (9 interface+implementation pairs + 2 background services) — Business logic, data access, Excel operations, WhatsApp integration.
3. **Models** (9 entities + 7 DTOs + 2 settings) — Database entities, data transfer objects, and configuration POCOs.
4. **Views** (Razor) — Server-rendered HTML with Bootstrap 5, Metronic v8 design system, DataTables, ApexCharts, Toastr, and SweetAlert2.
5. **Data** — Entity Framework Core DbContext with 9 DbSets mapped to SQL Server tables.
6. **Background Services** (2) — MembershipReminderService and DailyReportService running as hosted services.

## Database Tables (DbSets)

| DbSet | Entity Class | Description |
|-------|-------------|-------------|
| `Negocios` | `Gym` | Registered businesses |
| `Clientes` | `Cliente` | Clients belonging to businesses |
| `Logs` | `Logs` | Immutable financial records |
| `Notificaciones` | `Notificacion` | Auto-generated expiry alerts |
| `Sugerencias` | `Sugerencia` | Feedback from businesses to admin |
| `AdminLogs` | `AdminLog` | Admin action audit trail |
| `Productos` | `Producto` | Inventory products |
| `MovimientosInventario` | `MovimientoInventario` | Immutable inventory movement audit |
| `Vendedores` | `Vendedor` | Salesperson accounts |
