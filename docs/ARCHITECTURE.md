# Architecture

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 9.0 (MVC) |
| ORM | Entity Framework Core 9.0.10 |
| Database | SQL Server (with SQLite as alternative) |
| Authentication | Cookie Authentication + BCrypt |
| Frontend | Razor Views + Bootstrap 5 + Metronic v8 CSS |
| Tables | DataTables |
| Charts | ApexCharts |
| UI Notifications | Toastr + SweetAlert2 |
| Excel | ClosedXML 0.105.0 |
| Password Hashing | BCrypt.Net-Next 4.0.3 |
| Messaging | WhatsApp Cloud API (Meta) |

## Layer Diagram

```
┌───────────────────────────────────────────────────────────┐
│                    Views (Razor)                          │
│  Landing Page, Login, Admin Dashboard, Negocio Dashboard, │
│  Public Registration, Suggestion Management               │
├───────────────────────────────────────────────────────────┤
│                  Controllers (9)                          │
│  Home, Auth, Admin, Clientes, Logs, Ventas,              │
│  Notificaciones, Inventario, Sugerencias                 │
├───────────────────────────────────────────────────────────┤
│              Services (9 interface+impl pairs)            │
│  IAuthService, INegocioService, IClienteService,         │
│  ILogService, IVentasService, INotificationService,      │
│  ISugerenciaService, IWhatsAppService, IInventarioService │
├───────────────────────────────────────────────────────────┤
│              Background Services (2)                      │
│  MembershipReminderService (8:00 AM daily)               │
│  DailyReportService (9:00 PM daily)                      │
├───────────────────────────────────────────────────────────┤
│               EF Core DbContext (9 DbSets)               │
│  Negocios, Clientes, Logs, Notificaciones, Sugerencias,  │
│  AdminLogs, Productos, MovimientosInventario, Vendedores  │
├───────────────────────────────────────────────────────────┤
│                      SQL Server                           │
└───────────────────────────────────────────────────────────┘
```

## Multi-Tenancy

The application supports multiple businesses (negocios) on a single database. Each business has a unique `NegocioId` (Guid), and all data is scoped to this ID:

- `Clientes.NegocioId` — Clients belong to a specific business
- `Logs.NegocioId` — Financial records are per business
- `Notificaciones.NegocioId` — Notifications are per business
- `Productos.NegocioId` — Inventory is per business
- `MovimientosInventario.NegocioId` — Inventory movements are per business

Every controller endpoint validates that the authenticated user owns the `negocioId` they are requesting data for, preventing cross-tenant data access.

## Authentication Flow

```
User → POST /Negocios/Login
              │
              ▼
        AuthService.LoginAsync()
              │
              ├─ Is Admin? → Compare against AdminSettings (appsettings.json)
              │                → Cookie with Claim Role="Admin"
              │                → Redirect /Negocios/Index
              │
              └─ Is Negocio? → Look up by email in Negocios table
                              → Verify BCrypt hash
                              │   ├─ Match → OK
                              │   └─ No match → Try plaintext comparison
                              │       └─ Match → Auto-migrate to BCrypt hash
                              → Check subscription expiry
                              │   └─ Expired → Return "EXPIRED" error
                              → Cookie with Claims (NegocioId, Name, Role="Negocio")
                              → Redirect /Negocios/{id}/Dashboard
```

### Roles

| Role | Description | Access |
|------|-------------|--------|
| Admin | System administrator | Admin dashboard, CRUD all businesses, impersonation, view all suggestions |
| Negocio | Business owner | Own dashboard, CRUD own clients/logs/products, send suggestions |
| Vendedor | Salesperson (in development) | Entity exists but not yet integrated into auth flow |

### Cookie Configuration

- Cookie name: `NegocioAuthCookie`
- HttpOnly: `true`
- SameSite: `Strict`
- Secure: `SameAsRequest` in development, `Always` in production
- Persistent: `true` (survives browser close)
- Login path: `/Negocios/Login`

### BCrypt Lazy Migration

Existing plaintext passwords are automatically migrated to BCrypt hashes on successful login:

1. `AuthService.LoginAsync()` receives email + password
2. Looks up the business in the database by email
3. Tries `BCrypt.Verify(password, storedHash)`
4. If verification fails, compares as plaintext: `password == storedPassword`
5. If plaintext matches, generates a BCrypt hash and updates the database
6. Next login will use BCrypt directly

### Admin Impersonation

The admin can impersonate any business to debug issues or view their dashboard:

1. Admin calls `POST /Negocios/Impersonate/{id}`
2. A new session is created with the business's claims plus:
   - `AdminImpersonating = "true"`
   - `AdminEmail = "<admin-email>"`
3. Admin navigates the business dashboard as if they were the owner
4. To return: `POST /Negocios/StopImpersonation` restores the admin session using the stored `AdminEmail` claim

## Notification System

```
Dashboard loads → POST /Negocios/GenerarNotificaciones
                        │
                        ▼
              NotificationService.GenerarNotificacionesAsync()
                        │
                        ├─ Find clients with FechaQueTermina within next 3 days
                        ├─ Check no duplicate notification exists (same client, same day)
                        └─ Create Notificacion record for each qualifying client

UI Integration:
  ├─ Sidebar bell icon → GET /GetNotificacionesCount → badge with unread count
  ├─ Notification dropdown → GET /GetNotificaciones → list of notifications
  ├─ Notifications tab → same list, expanded view
  └─ Mark as read → POST /MarcarNotificacionLeida
```

## Inventory System

The inventory module provides full product lifecycle management with an immutable audit trail:

### Product Lifecycle
```
CrearProducto → [active product with initial stock]
                    │
                    ├─ VenderProducto    → stock decreases, MovimientoInventario(tipo="venta")
                    ├─ DevolverProducto  → stock increases, MovimientoInventario(tipo="devolucion")
                    ├─ RestockProducto   → stock increases, MovimientoInventario(tipo="restock")
                    ├─ AjustarStock     → stock corrected, MovimientoInventario(tipo="ajuste")
                    ├─ EditarProducto    → update name, prices, stock minimums
                    └─ EliminarProducto  → soft delete (IsActive=false), movements preserved
```

### Audit Trail
Every stock-changing operation creates an immutable `MovimientoInventario` record with:
- Product name at time of transaction (denormalized for historical accuracy)
- Type: `"venta"`, `"devolucion"`, `"restock"`, `"ajuste"`
- Quantity moved, unit price, total amount
- Stock before and stock after the movement
- Optional note (required for adjustments and returns)
- Timestamp

Product names are denormalized into movement records so historical data remains accurate even if the product is renamed or deleted.

## Suggestion/Feedback System

Businesses can send suggestions to the admin from their dashboard:

1. Negocio submits via `POST /Negocios/EnviarSugerencia` (max 1000 characters)
2. Suggestion stored with `NegocioId`, `NegocioNombre`, `Mensaje`, `Leida` flag
3. Admin views all suggestions in the admin dashboard Suggestions tab
4. Admin marks suggestions as read via `POST /Negocios/MarcarSugerenciaLeida`

## WhatsApp Integration

The application integrates with Meta's WhatsApp Cloud API for automated messaging:

### Background Services

1. **MembershipReminderService** (`BackgroundService`)
   - Runs at 8:00 AM Ecuador time (America/Guayaquil)
   - For each active business, finds clients whose membership expires in 3 or 1 day(s)
   - Sends a WhatsApp reminder including the business name and days remaining
   - All messages sent from the admin's WhatsApp Business number

2. **DailyReportService** (`BackgroundService`)
   - Runs at 9:00 PM Ecuador time
   - For each active business with a phone number, calculates:
     - Total income for the day (positive log amounts)
     - New clients registered today
     - Clients whose membership expires tomorrow
   - Sends a summary to the business owner's WhatsApp

### Configuration
Both services are registered via the `AddBackgroundServices()` extension method and require:
- `WhatsAppSettings.Enabled = true`
- Valid `PhoneNumberId` and `AccessToken` in appsettings.json

## Financial Logs (Immutable Source of Truth)

Logs serve as the immutable financial record. They are created:
- **Automatically** when a client is created, edited, renewed, or deleted (with the relevant amount)
- **Automatically** on login/logout (session tracking)
- **Manually** by the business owner for arbitrary income/expenses

Each log contains:
- `Tipo`: `"ingreso"`, `"gasto"`, `"cliente_creado"`, `"cliente_editado"`, `"cliente_renovado"`, `"cliente_eliminado"`, `"sesion_inicio"`, `"sesion_cierre"`
- `Monto`: positive for income, negative for expenses
- `ClienteId` and `NombreCliente`: for client-related logs (denormalized for persistence after client deletion)

## Dependency Injection

Registered in `Program.cs`:

```csharp
// Configuration
builder.Services.Configure<AdminSettings>(config.GetSection("AdminSettings"));
builder.Services.Configure<WhatsAppSettings>(config.GetSection("WhatsAppSettings"));

// Scoped services (one instance per HTTP request)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INegocioService, NegocioService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISugerenciaService, SugerenciaService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();

// HTTP client for WhatsApp API
builder.Services.AddHttpClient("WhatsApp");

// Background services (singletons, use IServiceScopeFactory internally)
builder.Services.AddHostedService<MembershipReminderService>();
builder.Services.AddHostedService<DailyReportService>();
```

All business services are `Scoped` (one instance per HTTP request). Background services are `Singleton` and create their own scopes via `IServiceScopeFactory` to access `DbContext` and other scoped services.

## Routing

All controllers (except `HomeController`) use `[Route("Negocios")]` as their base route. This means endpoints from different controllers share the same URL prefix, disambiguated by action names. For example:

- `/Negocios/Login` → `AuthController.Login()`
- `/Negocios/Index` → `AdminController.Index()`
- `/Negocios/{id}/Dashboard` → `ClientesController.Dashboard()`
- `/Negocios/GetProductos` → `InventarioController.GetProductos()`
- `/Negocios/EnviarSugerencia` → `SugerenciasController.EnviarSugerencia()`

`HomeController` uses the default MVC route: `{controller=Home}/{action=Index}/{id?}`.

## Design System (Metronic v8)

The frontend uses a custom design system based on Metronic v8, implemented in `wwwroot/css/metronic-style.css`.

### Theme System

Four color themes are available, stored in `localStorage('mg-theme')` and applied via the `data-theme` attribute on the `<html>` element:

| Theme | Description |
|-------|-------------|
| Light | Default bright theme |
| Dark | Dark mode |
| Ocean | Blue-tinted theme |
| Sunset | Warm-toned theme |

### CSS Variables

| Variable | Default Value | Usage |
|----------|--------------|-------|
| `--mg-primary` | `#3E97FF` | Buttons, links, sidebar active state |
| `--mg-success` | `#50CD89` | Active states, positive badges |
| `--mg-warning` | `#FFC700` | Alerts, trial status |
| `--mg-danger` | `#F1416C` | Errors, deletion, expired states |
| `--mg-dark` | `#181C32` | Sidebar background |
| `--mg-gradient-primary-end` | varies | Gradient card end colors |
| `--mg-gradient-danger-end` | varies | Gradient card end colors |
| `--mg-gradient-success-end` | varies | Gradient card end colors |

### Components

- `mg-sidebar` — Dashboard navigation sidebar
- `mg-topbar` — Top navigation bar
- `mg-card` — Content cards
- `mg-stat-card` — Statistics cards with gradients
- `mg-bell` — Notification bell with badge
- `badge-light-*` — Semantic light badges (success, danger, warning, primary)
- `btn-light-*` — Semantic light buttons

### Dashboard Navigation

Dashboard tabs use `data-mg-tab` attributes and fire `mg-tab-shown` custom events for sidebar navigation. Modals use `modal-fullscreen-sm-down` for responsive mobile behavior.

## Data Model Relationships

```
Gym (Negocio)
├── 1:N → Cliente (clients)
├── 1:N → Logs (financial records)
├── 1:N → Notificacion (expiry alerts)
├── 1:N → Sugerencia (feedback)
├── 1:N → Producto (inventory products)
├── 1:N → MovimientoInventario (inventory movements)
└── N:1 → Vendedor? (optional salesperson reference)

Producto
└── 1:N → MovimientoInventario (via ProductoId, denormalized NombreProducto)

AdminLog (standalone — admin action audit)

Vendedor (standalone — salesperson entity, referenced by Gym.VendedorId)
```

## Subscription Tracking

Each business (Gym/Negocio) has subscription-related fields:

- `DiasPagados` (int, default 30) — Number of days in the subscription
- `PrecioSuscripcion` (decimal) — Monthly subscription price
- `FechaPago` (DateTime?) — Date of last payment
- `FechaExpiracion` (DateTime?) — When the subscription expires
- `IsActive` (bool) — Whether the business is a paid/active subscriber
- `EsPrueba` (bool) — Whether the business is on a trial

The login flow checks `FechaExpiracion` and returns an `EXPIRED` error if the subscription has lapsed, preventing access to the dashboard.
