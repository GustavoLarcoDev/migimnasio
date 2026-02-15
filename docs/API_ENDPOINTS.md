# API Endpoints

All endpoints use the base route `/Negocios` (via `[Route("Negocios")]` on each controller), except for `HomeController` which uses the default MVC routing.

---

## Home (HomeController) — Public

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/` | Public landing page |
| GET | `/Home/Error` | Error page with trace ID |

---

## Authentication (AuthController) — Public

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| GET | `/Negocios/Login` | Show login form (redirects if already authenticated) | No |
| POST | `/Negocios/Login` | Process login | No |
| GET | `/Negocios/Logout` | Sign out and redirect to login | No |

### POST /Negocios/Login
**Parameters (form):**
- `email` (string, required) — Email or phone number
- `password` (string, required)

**Behavior:**
- Checks admin credentials from `AdminSettings` (appsettings.json) first
- Then checks the `Negocios` table by email
- Supports BCrypt hash verification with lazy migration from plaintext
- Returns `EXPIRED` error if subscription has expired

**Response:** Redirect to `/Negocios/Index` (admin) or `/Negocios/{id}/Dashboard` (negocio)

### GET /Negocios/Logout
**Behavior:** If the user is a negocio, logs the session close event before signing out.

---

## Admin (AdminController) — Requires Auth + Admin Role

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/Negocios` | Admin panel (list of businesses) |
| GET | `/Negocios/Index` | Alias for admin panel |
| GET | `/Negocios/GetNegocios` | JSON: all businesses with stats |
| GET | `/Negocios/GetNegocio/{id}` | JSON: single business by ID |
| GET | `/Negocios/GetAdminDashboardStats` | JSON: admin dashboard statistics |
| GET | `/Negocios/GetAdminLogs` | JSON: admin action history (last 200) |
| GET | `/Negocios/GetVentasAdmin` | JSON: admin financial/sales data |
| POST | `/Negocios/Create` | Create a new business |
| POST | `/Negocios/Editar` | Edit an existing business |
| POST | `/Negocios/Eliminar` | Delete a business (fails if it has clients) |
| POST | `/Negocios/CambiarEstado` | Toggle business state (Paid/Trial) |
| GET | `/Negocios/ExportExcel` | Download businesses as Excel (.xlsx) |
| POST | `/Negocios/Impersonate/{id}` | Start impersonating a business |
| POST | `/Negocios/StopImpersonation` | Stop impersonation, restore admin session |

### POST /Negocios/Create
**Parameters (form):**
- `NombreNegocio` (string) — Business name
- `duenoNegocio` (string) — Owner name
- `telefono` (string) — Phone number
- `EmailNegocio` (string) — Email
- `passwordNegocio` (string) — Password (will be BCrypt-hashed)
- `isActive` (bool) — Active paid status
- `esPrueba` (bool) — Trial status
- `fechaPago` (DateTime?, optional) — Last payment date
- `fechaExpiracion` (DateTime?, optional) — Subscription expiration date
- `precioSuscripcion` (decimal?, optional) — Subscription price
- `diasPagados` (int?, optional) — Days paid for

**Response:** `{ success: true, message: "..." }`

### POST /Negocios/Editar
**Parameters (form):** Full `Gym` object (NegocioId, NegocioNombre, DuenoNegocio, Email, Telefono, Password, IsActive, EsPrueba, subscription fields). If Password is empty, the existing password is kept.

### POST /Negocios/Eliminar
**Parameters (form):** `id` (Guid) — Business ID

### POST /Negocios/CambiarEstado
**Parameters (form):** `id` (Guid) — Business ID
**Response:** `{ success: true, message: "...", isActive: bool, esPrueba: bool }`

### GET /Negocios/GetAdminDashboardStats
**Response:**
```json
{
  "totalNegocios": 10,
  "negociosActivos": 7,
  "negociosPrueba": 3,
  "mrr": 500.00,
  "ingresosPorMes": [...]
}
```

### POST /Negocios/Impersonate/{id}
**Parameters:** `id` (Guid, route) — Business ID to impersonate.
Creates a new session with `AdminImpersonating=true` and `AdminEmail` claims so the admin can browse as that business and return later.

### POST /Negocios/StopImpersonation
Restores the admin session using the stored `AdminEmail` claim. Redirects to admin panel.

---

## Clients (ClientesController) — Requires Auth

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/Negocios/{id}/Dashboard` | Negocio dashboard view |
| GET | `/Negocios/GetDashboardStats` | JSON: dashboard statistics |
| GET | `/Negocios/GetClientes` | JSON: all clients for a business |
| GET | `/Negocios/GetCliente` | JSON: single client by ID |
| GET | `/Negocios/GetSuscripcionStatus` | JSON: business subscription status |
| GET | `/Negocios/GetClientesDiarios` | JSON: daily-pay clients |
| POST | `/Negocios/CrearCliente` | Create a new client |
| POST | `/Negocios/EditarCliente` | Edit an existing client |
| POST | `/Negocios/EliminarCliente` | Delete a client |
| POST | `/Negocios/RenovarCliente` | Renew a client's membership |
| POST | `/Negocios/LimpiarClientesDiarios` | Reset daily client flags |
| GET | `/Negocios/ExportClientesExcel` | Download clients as Excel (.xlsx) |
| POST | `/Negocios/ImportarClientesExcel` | Import clients from Excel (.xlsx/.xls) |

### GET /Negocios/GetDashboardStats
**Parameters (query):** `negocioId` (Guid)
**Response:**
```json
{
  "totalClientes": 50,
  "clientesActivos": 35,
  "clientesVencidos": 15,
  "ingresosDelMes": 5000.00,
  "ingresosHoy": 200.00,
  "clientesPorVencer": 5
}
```

### GET /Negocios/GetSuscripcionStatus
**Parameters (query):** `negocioId` (Guid)
**Response:**
```json
{
  "diasRestantes": 15,
  "fechaExpiracion": "2026-03-01T00:00:00",
  "fechaPago": "2026-02-01T00:00:00",
  "diasPagados": 30,
  "precioSuscripcion": 50.00,
  "porEmpezar": false
}
```

### POST /Negocios/CrearCliente
**Parameters (form):** `ClienteCreateDto`
- `NegocioId` (Guid, required)
- `Nombre` (string, required, max 100)
- `Apellido` (string, required, max 100)
- `Email` (string, optional)
- `Telefono` (string, required)
- `Direccion` (string, optional)
- `EsDiario` (bool) — Daily-pay client flag
- `FechaInicio` (DateTime?, optional)
- `FechaFin` (DateTime?, optional) — Takes priority over Dias
- `Dias` (int) — Used if FechaFin is null
- `Precio` (decimal, required, > 0)

### POST /Negocios/RenovarCliente
**Parameters (form):**
- `id` (Guid) — Client ID
- `negocioId` (Guid)
- `nuevaFechaFin` (DateTime) — New membership end date
- `precio` (decimal) — Renewal price

### POST /Negocios/ImportarClientesExcel
**Parameters (form):**
- `negocioId` (Guid)
- `file` (IFormFile) — Excel file (.xlsx or .xls)

Auto-detects columns by headers and skips duplicates.

### POST /Negocios/LimpiarClientesDiarios
**Parameters (form):** `negocioId` (Guid)
Resets all daily clients' `EsDiario` flag to false after notifications are sent.

---

## Financial Logs (LogsController) — Requires Auth

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/Negocios/CrearLog` | Create a manual log entry |
| GET | `/Negocios/GetLogs` | JSON: all logs for a business |
| GET | `/Negocios/GetLog` | JSON: single log by ID |
| GET | `/Negocios/GetOldestLogDate` | JSON: oldest log date (for date filters) |
| POST | `/Negocios/EliminarTodosLogs` | Delete all logs (irreversible) |
| GET | `/Negocios/ExportLogsExcel` | Download logs as Excel (.xlsx) |

### POST /Negocios/CrearLog
**Parameters (form):** `LogCreateDto`
- `NegocioId` (Guid, required)
- `Message` (string, required, max 300)
- `Monto` (decimal, required) — Positive = income, negative = expense

The log type is automatically assigned based on the sign of the amount.

### GET /Negocios/GetLogs
**Parameters (query):** `negocioId` (Guid)
Returns all logs ordered by date descending.

### GET /Negocios/GetOldestLogDate
**Parameters (query):** `negocioId` (Guid)
**Response:** `{ "fecha": "2026-01-15T00:00:00" }`

### GET /Negocios/ExportLogsExcel
**Parameters (query):** `negocioId` (Guid)
Exports logs with a summary of income, expenses, and balance.

---

## Sales & Statistics (VentasController) — Requires Auth

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/Negocios/GetVentasStats` | JSON: comprehensive sales statistics |
| GET | `/Negocios/GetChartData` | JSON: income vs expenses chart data |
| GET | `/Negocios/GetClientesChartData` | JSON: new clients chart data |

### GET /Negocios/GetVentasStats
**Parameters (query):** `negocioId` (Guid)
**Response:**
```json
{
  "ventasHoy": 500.00,
  "ventasMes": 12000.00,
  "ventasAnio": 144000.00,
  "gastosHoy": 100.00,
  "gastosMes": 3000.00,
  "gastosAnio": 36000.00,
  "gananciaHoy": 400.00,
  "gananciaMes": 9000.00,
  "gananciaAnio": 108000.00
}
```

### GET /Negocios/GetChartData
**Parameters (query):**
- `negocioId` (Guid)
- `periodo` (string: `"dia"` | `"semana"` | `"mes"` | `"anio"`, default: `"semana"`)

**Response:**
```json
{
  "labels": ["Mon", "Tue", "Wed", ...],
  "ingresos": [500, 300, 800, ...],
  "gastos": [100, 50, 200, ...]
}
```

### GET /Negocios/GetClientesChartData
**Parameters (query):**
- `negocioId` (Guid)
- `periodo` (string: `"dia"` | `"semana"` | `"mes"` | `"anio"`, default: `"semana"`)

Returns chart data for new client registrations by period.

---

## Notifications (NotificacionesController) — Requires Auth

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/Negocios/GetNotificaciones` | JSON: last 50 notifications |
| GET | `/Negocios/GetNotificacionesCount` | JSON: unread notification count |
| POST | `/Negocios/MarcarNotificacionLeida` | Mark one notification as read |
| POST | `/Negocios/MarcarTodasNotificacionesLeidas` | Mark all notifications as read |
| POST | `/Negocios/GenerarNotificaciones` | Generate expiry notifications |

### GET /Negocios/GetNotificaciones
**Parameters (query):** `negocioId` (Guid)
Returns the last 50 notifications for the business.

### GET /Negocios/GetNotificacionesCount
**Parameters (query):** `negocioId` (Guid)
**Response:** `{ "count": 5 }`

### POST /Negocios/MarcarNotificacionLeida
**Parameters (form):**
- `id` (Guid) — Notification ID
- `negocioId` (Guid)

### POST /Negocios/MarcarTodasNotificacionesLeidas
**Parameters (form):** `negocioId` (Guid)

### POST /Negocios/GenerarNotificaciones
**Parameters (form):** `negocioId` (Guid)
Auto-generates notifications for clients whose membership expires within the next 3 days. Prevents duplicates (one per client per day). Called automatically when the dashboard loads.
**Response:** `{ "success": true, "message": "...", "count": 3 }`

---

## Inventory (InventarioController) — Requires Auth

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/Negocios/GetProductos` | JSON: all products for a business |
| GET | `/Negocios/GetProducto` | JSON: single product by ID |
| POST | `/Negocios/CrearProducto` | Create a new product |
| POST | `/Negocios/EditarProducto` | Edit an existing product |
| POST | `/Negocios/EliminarProducto` | Soft-delete a product |
| POST | `/Negocios/VenderProducto` | Record a product sale |
| POST | `/Negocios/DevolverProducto` | Record a product return |
| POST | `/Negocios/RestockProducto` | Record a product restock |
| POST | `/Negocios/AjustarStock` | Record a stock adjustment |
| GET | `/Negocios/GetMovimientos` | JSON: all inventory movements |
| GET | `/Negocios/GetInventarioStats` | JSON: inventory statistics |
| GET | `/Negocios/ExportInventarioExcel` | Download inventory as Excel (.xlsx) |

### POST /Negocios/CrearProducto
**Parameters (form):** `ProductoCreateDto`
- `NegocioId` (Guid, required)
- `Nombre` (string, required, max 200)
- `PrecioVenta` (decimal, required, > 0) — Sale price
- `CostoCompra` (decimal, required, > 0) — Purchase cost
- `Stock` (int, required, >= 0) — Initial stock
- `StockMinimo` (int, default: 5) — Minimum stock alert threshold

### POST /Negocios/EditarProducto
**Parameters (form):** Same as `ProductoCreateDto` with `ProductoId` set.

### POST /Negocios/EliminarProducto
**Parameters (form):**
- `id` (Guid) — Product ID
- `negocioId` (Guid)

Performs a soft delete (sets `IsActive = false`).

### POST /Negocios/VenderProducto
**Parameters (form):**
- `productoId` (Guid)
- `negocioId` (Guid)
- `cantidad` (int) — Quantity to sell

Decrements stock and creates an immutable `MovimientoInventario` record of type `"venta"`.

### POST /Negocios/DevolverProducto
**Parameters (form):**
- `productoId` (Guid)
- `negocioId` (Guid)
- `cantidad` (int) — Quantity returned
- `nota` (string) — Reason for return (required)

Increments stock and creates a `MovimientoInventario` record of type `"devolucion"`.

### POST /Negocios/RestockProducto
**Parameters (form):**
- `productoId` (Guid)
- `negocioId` (Guid)
- `cantidad` (int) — Quantity added
- `costoTotal` (decimal) — Total purchase cost

Increments stock and creates a `MovimientoInventario` record of type `"restock"`.

### POST /Negocios/AjustarStock
**Parameters (form):**
- `productoId` (Guid)
- `negocioId` (Guid)
- `stockReal` (int) — Actual physical stock count
- `nota` (string) — Reason for adjustment (required)

Sets stock to the provided value and creates a `MovimientoInventario` record of type `"ajuste"`.

### GET /Negocios/GetMovimientos
**Parameters (query):** `negocioId` (Guid)
Returns all inventory movements for the business with product name, type, quantity, prices, and stock before/after.

### GET /Negocios/GetInventarioStats
**Parameters (query):** `negocioId` (Guid)
Returns aggregate inventory statistics (total products, total stock value, low stock alerts, etc.).

---

## Suggestions (SugerenciasController) — Requires Auth

| Method | Route | Description | Access |
|--------|-------|-------------|--------|
| POST | `/Negocios/EnviarSugerencia` | Submit a suggestion | Negocio |
| GET | `/Negocios/Sugerencias` | Suggestions management view | Admin |
| GET | `/Negocios/GetSugerencias` | JSON: all suggestions | Admin |
| POST | `/Negocios/MarcarSugerenciaLeida` | Mark a suggestion as read | Admin |

### POST /Negocios/EnviarSugerencia
**Parameters (form):**
- `negocioId` (Guid)
- `mensaje` (string, max 1000 characters)

Automatically includes the business name from the authenticated user's claims.

### GET /Negocios/GetSugerencias
Returns all suggestions ordered by date, with read/unread status.

### POST /Negocios/MarcarSugerenciaLeida
**Parameters (form):** `id` (Guid) — Suggestion ID

---

## Common Response Patterns

### Success
```json
{ "success": true, "message": "Operation completed successfully" }
```

### Error (400 Bad Request)
```json
{ "success": false, "message": "Validation error description" }
```

### Error (404 Not Found)
```json
{ "success": false, "message": "Resource not found" }
```

### Error (500 Internal Server Error)
```json
{ "success": false, "message": "Exception message" }
```

### Authorization
All protected endpoints return HTTP 403 Forbidden if the authenticated user does not own the requested `negocioId` or lacks admin privileges. Unauthenticated requests are redirected to `/Negocios/Login`.
