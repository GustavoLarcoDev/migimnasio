# PROTOCOLO 002 — Intensive System Testing with Fake Data

## Overview

This protocol launches a **3-phase automated testing pipeline** that exercises the ENTIRE My-Negocio system by creating fake data, testing every endpoint, attempting to break the system, and automatically fixing any errors found.

## How to Run

```
run protocolo 002
```

## Prerequisites

- SQL Server running on localhost:1433
- App must be in Development mode (for TestAgentController + SeedController access)
- Port 5170 available (or whichever the app uses)

## Architecture

```
┌──────────────────────────────────────────────────────┐
│              PHASE 0 — SETUP (1 agent)               │
│  Build → Start app → Seed DB → Verify health         │
│  Output: Base URL + Cookie jar setup                  │
└──────────────────────┬───────────────────────────────┘
                       │ app running
┌──────────────────────▼───────────────────────────────┐
│          PHASE 1 — TESTING (6 agents parallel)       │
│                                                       │
│  Agent 1: Auth & Security Testing                     │
│  Agent 2: Admin & Vendedor Testing                    │
│  Agent 3: Membresias (Clients/Memberships) Testing    │
│  Agent 4: Artesanal (Appointments/Employees) Testing  │
│  Agent 5: Inventario & Ventas Testing                 │
│  Agent 6: Edge Cases, Stress & Boundary Testing       │
│                                                       │
│  Each agent: curl → verify → report errors            │
└──────────────────────┬───────────────────────────────┘
                       │ error reports
┌──────────────────────▼───────────────────────────────┐
│          PHASE 2 — FIX (2 Opus agents)               │
│                                                       │
│  Agent A: Backend fixes (Controllers + Services)      │
│  Agent B: Frontend fixes (Views + JavaScript)         │
│                                                       │
│  Each receives full error context → reads code → fix  │
└──────────────────────────────────────────────────────┘
```

## Testing Approach

All test agents use:
- **`curl -b cookies.txt -c cookies.txt`** for session persistence
- **`TestAgentController`** for JSON login/impersonation (no CSRF required)
- **`SeedController`** to populate the database with demo data
- **HTTP status code validation** (expect 200/302 for success, specific codes for errors)
- **JSON response parsing** via `jq` for structured validation
- **Cross-tenant isolation tests** (try to access other business's data)

## Phase 0 — Setup Agent

**Purpose:** Prepare the environment for testing.

**Steps:**
1. `dotnet build` — Verify no compilation errors
2. Start app: `dotnet run` in background (Development environment)
3. Wait for health check: `curl http://localhost:5170/health`
4. Seed database: `curl http://localhost:5170/Seed/Generate`
5. Verify seed success by checking response
6. Create cookie jar files for each test agent
7. Report: base URL, seed data summary, ready status

## Phase 1 — Test Agents (6 parallel)

### Agent 1: AUTH & SECURITY TESTING

**Login as each role and verify correct behavior:**
```
# Admin login
POST /TestAgent/LoginAsAdmin → verify { success: true, role: "Admin" }
GET /TestAgent/WhoAmI → verify claims contain role=Admin

# Business login (membresias)
POST /TestAgent/Login { email: "iron@fitness.com", password: "demo123" }
→ verify { success: true, role: "Negocio", tipoNegocio: "membresias" }

# Business login (artesanal)
POST /TestAgent/Login { email: "barberia@patron.com", password: "demo123" }
→ verify { success: true, role: "Negocio", tipoNegocio: "artesanal" }

# Wrong password
POST /TestAgent/Login { email: "iron@fitness.com", password: "WRONG" }
→ verify { success: false }

# Non-existent user
POST /TestAgent/Login { email: "nobody@fake.com", password: "test" }
→ verify { success: false }
```

**Multi-tenant isolation (CRITICAL):**
```
# Login as Business A, try to access Business B's data
POST /TestAgent/Login as Business A
GET /Negocios/GetClientes?negocioId={B_ID} → must return 0 or 403
GET /Negocios/GetProductos?negocioId={B_ID} → must return 0 or 403
GET /Negocios/GetLogs?negocioId={B_ID} → must return 0 or 403
```

**Impersonation flow:**
```
POST /TestAgent/LoginAsAdmin
POST /TestAgent/Admin/Impersonate/{id} → verify success
GET /TestAgent/WhoAmI → verify AdminImpersonating=true claim
POST /TestAgent/Admin/StopImpersonation → verify back to Admin
```

**Unauthorized access:**
```
# Without login, access protected endpoints
GET /Negocios/GetClientes → expect 401/302 to login
GET /Negocios/GetProductos → expect 401/302 to login

# As Negocio, access admin endpoints
POST /TestAgent/Login as negocio
GET /Negocios/GetVendedores → expect 403/empty
GET /Negocios/GetNegocios → expect 403/empty
```

### Agent 2: ADMIN & VENDEDOR TESTING

**Admin dashboard:**
```
POST /TestAgent/LoginAsAdmin
GET /Negocios/GetNegocios → verify returns list of businesses
GET /Negocios/GetAdminDashboardStats → verify { totalNegocios, activos, mrr }
GET /Negocios/GetAdminLogs → verify returns array
GET /Negocios/GetVentasAdmin → verify financial data
```

**Business CRUD:**
```
# Create business
POST /Negocios/Create {
  NegocioNombre: "Test Gym Protocol",
  DuenoNegocio: "Juan Prueba",
  Email: "test@protocolo002.com",
  Password: "Test1234!",
  Telefono: "0999999999",
  TipoNegocio: "membresias",
  PrecioMensual: 25.00,
  DiasPagados: 30
}
→ verify success, capture negocioId

# Edit business
POST /Negocios/Editar { NegocioId: {id}, NegocioNombre: "Test Gym Updated" }
→ verify success

# Get business
GET /Negocios/GetNegocio/{id} → verify updated name

# Change state (paid → trial → paid)
POST /Negocios/CambiarEstado { NegocioId: {id} }
→ verify toggled

# Delete business
POST /Negocios/Eliminar { NegocioId: {id} }
→ verify deleted
```

**Vendedor CRUD:**
```
POST /Negocios/CrearVendedor {
  Nombre: "Vendedor Test",
  Email: "vendedor@test.com",
  Password: "Vend1234!",
  Telefono: "0988888888",
  NombreBanco: "Banco Pichincha",
  NumeroCuenta: "2200012345",
  NumeroCedula: "1712345678"
}
→ verify success, capture vendedorId

# Edit vendor
POST /Negocios/EditarVendedor { ... updated fields ... }

# Vendor login
POST /TestAgent/Login { email: "vendedor@test.com", password: "Vend1234!" }
→ verify role=Vendedor

# Vendor creates business → verify commission auto-generated
POST /Negocios/VendedorCrearNegocio { ... PrecioMensual: 20, DiasPagados: 30 ... }
→ verify commission appears in GetComisionesPendientes

# Vendor creates trial business → verify NO commission
POST /Negocios/VendedorCrearNegocio { ... PrecioMensual: 10, DiasPagados: 7 ... }
→ verify NO new commission

# Commission payment
POST /TestAgent/LoginAsAdmin
GET /Negocios/GetComisionesPendientes → verify pending commissions with bank details
POST /Negocios/PagarComisionesVendedor { VendedorId: {id} }
→ verify commissions marked paid

# Delete vendor
POST /Negocios/EliminarVendedor { VendedorId: {id} }
```

**Impersonation as vendor:**
```
POST /TestAgent/LoginAsAdmin
POST /TestAgent/Vendedor/Impersonate/{id}
GET /TestAgent/WhoAmI → verify VendedorImpersonating=true
POST /TestAgent/Vendedor/StopImpersonation
```

### Agent 3: MEMBRESIAS TESTING (Clients & Memberships)

**Login as membresias business, then test:**

**Client CRUD:**
```
# Create client
POST /Negocios/CrearCliente {
  Nombre: "Cliente Prueba",
  Email: "cliente@test.com",
  Telefono: "0977777777",
  FechaInicio: "2026-02-18",
  FechaFin: "2026-03-18",
  PrecioMensual: 30.00,
  PaseDiario: false
}
→ verify success, capture clienteId

# Get client
GET /Negocios/GetCliente?clienteId={id} → verify all fields match

# Edit client
POST /Negocios/EditarCliente { ClienteId: {id}, Nombre: "Cliente Editado" }
→ verify success

# Create daily pass client
POST /Negocios/CrearCliente { ..., PaseDiario: true, FechaInicio: today, FechaFin: today }
→ verify success

# List clients
GET /Negocios/GetClientes?negocioId={id} → verify array contains created clients

# List daily clients
GET /Negocios/GetClientesDiarios?negocioId={id} → verify daily client appears

# Clean daily clients
POST /Negocios/LimpiarClientesDiarios { negocioId: {id} }
→ verify daily clients reset
```

**Membership renewal:**
```
# Renew client
POST /Negocios/RenovarCliente {
  ClienteId: {id},
  FechaFin: "2026-04-18",
  PrecioMensual: 35.00
}
→ verify FechaFin updated, price updated
```

**Dashboard stats:**
```
GET /Negocios/GetDashboardStats?negocioId={id}
→ verify { totalClientes, clientesActivos, ingresosMes, porVencer }

GET /Negocios/GetSuscripcionStatus?negocioId={id}
→ verify subscription status fields
```

**Notifications:**
```
POST /Negocios/GenerarNotificaciones { negocioId: {id} }
→ verify notifications created for expiring memberships

GET /Negocios/GetNotificaciones?negocioId={id}
→ verify returns array

GET /Negocios/GetNotificacionesCount?negocioId={id}
→ verify count matches

POST /Negocios/MarcarNotificacionLeida { notificacionId: {id} }
→ verify marked read

POST /Negocios/MarcarTodasNotificacionesLeidas { negocioId: {id} }
→ verify all marked
```

**Financial logs:**
```
POST /Negocios/CrearLog {
  NegocioId: {id},
  Descripcion: "Test income",
  Monto: 50.00,
  Tipo: "ingreso"
}
→ verify success

POST /Negocios/CrearLog {
  NegocioId: {id},
  Descripcion: "Test expense",
  Monto: 20.00,
  Tipo: "gasto"
}
→ verify success

GET /Negocios/GetLogs?negocioId={id} → verify both logs

GET /Negocios/GetOldestLogDate?negocioId={id} → verify date

# Delete all logs
POST /Negocios/EliminarTodosLogs { negocioId: {id} }
→ verify 0 logs after
```

**Sales stats:**
```
GET /Negocios/GetVentasStats?negocioId={id}
→ verify { hoy, esteMes, esteAno }

GET /Negocios/GetChartData?negocioId={id}
→ verify chart data structure

GET /Negocios/GetClientesChartData?negocioId={id}
→ verify chart data structure
```

**Excel export/import:**
```
GET /Negocios/ExportClientesExcel?negocioId={id}
→ verify returns .xlsx file (content-type check)

# Import test (with valid Excel file structure)
POST /Negocios/ImportarClientesExcel (multipart form)
→ verify clients imported
```

**Delete client:**
```
POST /Negocios/EliminarCliente { ClienteId: {id} }
→ verify deleted, GET returns 404/empty
```

### Agent 4: ARTESANAL TESTING (Appointments, Employees, Services)

**Login as artesanal business, then test:**

**Services CRUD:**
```
POST /Negocios/CrearServicio {
  NegocioId: {id},
  Nombre: "Corte Test",
  Precio: 15.00,
  DuracionMinutos: 30,
  Descripcion: "Test service"
}
→ capture servicioId

GET /Negocios/GetServicios?negocioId={id} → verify service exists
GET /Negocios/GetServicio?servicioId={id} → verify fields
POST /Negocios/EditarServicio { ..., Nombre: "Corte Editado" }
POST /Negocios/EliminarServicio { servicioId: {id} }
```

**Employees CRUD:**
```
POST /Negocios/CrearEmpleado {
  NegocioId: {id},
  Nombre: "Empleado Test",
  Telefono: "0966666666",
  Email: "empleado@test.com",
  Especialidad: "Barbería"
}
→ capture empleadoId

GET /Negocios/GetEmpleados?negocioId={id} → verify
GET /Negocios/GetEmpleado?empleadoId={id} → verify
POST /Negocios/EditarEmpleado { ..., Nombre: "Empleado Editado" }
```

**Employee schedules:**
```
# Save weekly schedule (Mon-Fri 09:00-17:00, Sat 10:00-14:00, Sun off)
POST /Negocios/GuardarHorarios {
  EmpleadoId: {id},
  Horarios: [
    { DiaSemana: 1, HoraInicio: "09:00", HoraFin: "17:00", Disponible: true },
    { DiaSemana: 2, HoraInicio: "09:00", HoraFin: "17:00", Disponible: true },
    ...
    { DiaSemana: 0, Disponible: false }
  ]
}

GET /Negocios/GetHorarios?empleadoId={id} → verify schedule saved

# Create exception (day off)
POST /Negocios/CrearExcepcionHorario {
  EmpleadoId: {id},
  Fecha: "2026-02-25",
  Disponible: false,
  Motivo: "Vacation"
}

# Delete exception
POST /Negocios/EliminarExcepcionHorario { excepcionId: {id} }
```

**Artesanal clients:**
```
POST /Negocios/CrearClienteArtesanal {
  NegocioId: {id},
  Nombre: "Cliente Artesanal Test",
  Telefono: "0955555555",
  Email: "artesanal@test.com"
}

GET /Negocios/GetClientesArtesanal?negocioId={id}
POST /Negocios/EditarClienteArtesanal { ... }
```

**Appointment lifecycle (full flow):**
```
# Check available slots
GET /Negocios/GetSlotsDisponibles?negocioId={id}&empleadoId={empId}&fecha=2026-02-20&servicioId={svcId}
→ verify returns time slots

# Quick appointment
POST /Negocios/CrearCitaRapida {
  NegocioId: {id},
  NombreCliente: "Walk-in Test",
  Telefono: "0944444444",
  EmpleadoId: {empId},
  ServicioId: {svcId},
  FechaHoraInicio: "2026-02-20T10:00:00"
}
→ capture citaId

# Full appointment
POST /Negocios/CrearCita {
  NegocioId: {id},
  ClienteId: {clienteId},
  EmpleadoId: {empId},
  ServicioId: {svcId},
  FechaHoraInicio: "2026-02-20T11:00:00",
  Notas: "Test appointment"
}
→ capture citaId2

# Get appointment
GET /Negocios/GetCita?citaId={id} → verify all fields

# Get calendar
GET /Negocios/GetCitasCalendario?negocioId={id}&start=2026-02-01&end=2026-02-28
→ verify both appointments appear

# Confirm appointment
POST /Negocios/CambiarEstadoCita { CitaId: {id}, Estado: "confirmada" }
→ verify state changed

# Move appointment (reschedule)
POST /Negocios/MoverCita { CitaId: {id}, FechaHoraInicio: "2026-02-20T14:00:00" }
→ verify time updated

# Start appointment
POST /Negocios/CambiarEstadoCita { CitaId: {id}, Estado: "en_progreso" }

# Complete and pay
POST /Negocios/RegistrarPagoCita {
  CitaId: {id},
  Monto: 15.00,
  MetodoPago: "efectivo"
}
→ verify payment recorded

GET /Negocios/GetPagoCita?citaId={id} → verify payment details

# Cancel second appointment
POST /Negocios/CambiarEstadoCita { CitaId: {citaId2}, Estado: "cancelada" }
→ verify cancelled
```

**Double-booking prevention:**
```
# Try to book same employee, same time as existing appointment
POST /Negocios/CrearCita {
  EmpleadoId: {empId},
  FechaHoraInicio: "2026-02-20T14:00:00",  # Same time as moved appointment
  ...
}
→ must FAIL or warn about conflict
```

**Dashboard stats:**
```
GET /Negocios/GetCitasDashboardStats?negocioId={id}
→ verify stats reflect created appointments

GET /Negocios/GetHistorialCliente?clienteId={id}
→ verify history
```

**Employee search/availability:**
```
GET /Negocios/GetEmpleadosConDisponibilidad?negocioId={id}&fecha=2026-02-20
→ verify employee availability for date

GET /Negocios/BuscarClientes?negocioId={id}&q=Test
→ verify search returns created clients
```

**Delete employee:**
```
POST /Negocios/EliminarEmpleado { EmpleadoId: {id} }
→ verify deleted
```

### Agent 5: INVENTARIO & VENTAS TESTING

**Login as membresias business, then test:**

**Product CRUD:**
```
POST /Negocios/CrearProducto {
  NegocioId: {id},
  Nombre: "Proteína Whey Test",
  Precio: 45.00,
  Costo: 25.00,
  Stock: 100,
  StockMinimo: 10,
  Categoria: "suplementos"
}
→ capture productoId

GET /Negocios/GetProductos?negocioId={id} → verify product
GET /Negocios/GetProducto?productoId={id} → verify fields
POST /Negocios/EditarProducto { ..., Precio: 50.00 }
→ verify price updated
```

**Stock operations:**
```
# Sell product
POST /Negocios/VenderProducto {
  ProductoId: {id},
  Cantidad: 5,
  NegocioId: {negId}
}
→ verify stock reduced to 95

# Return product
POST /Negocios/DevolverProducto {
  ProductoId: {id},
  Cantidad: 2,
  NegocioId: {negId}
}
→ verify stock increased to 97

# Restock
POST /Negocios/RestockProducto {
  ProductoId: {id},
  Cantidad: 50,
  NegocioId: {negId}
}
→ verify stock = 147

# Stock adjustment
POST /Negocios/AjustarStock {
  ProductoId: {id},
  NuevoStock: 100,
  NegocioId: {negId}
}
→ verify stock = 100
```

**Inventory movements audit:**
```
GET /Negocios/GetMovimientos?productoId={id}
→ verify 4 movements: sale(-5), return(+2), restock(+50), adjustment

GET /Negocios/GetInventarioStats?negocioId={id}
→ verify { totalProductos, valorTotal, stockBajo }
```

**Negative stock test:**
```
# Try to sell more than available
POST /Negocios/VenderProducto {
  ProductoId: {id},
  Cantidad: 999
}
→ must FAIL or prevent negative stock
```

**Excel export:**
```
GET /Negocios/ExportInventarioExcel?negocioId={id}
→ verify returns .xlsx
```

**Financial integration:**
```
# After sales, verify revenue appears in stats
GET /Negocios/GetVentasStats?negocioId={id}
→ verify product sales reflected

GET /Negocios/GetChartData?negocioId={id}&periodo=mes
→ verify chart includes today's sales
```

**Delete product:**
```
POST /Negocios/EliminarProducto { ProductoId: {id} }
→ verify soft-deleted (still in DB but hidden)
```

### Agent 6: EDGE CASES, STRESS & BOUNDARY TESTING

**Invalid data tests:**
```
# Empty required fields
POST /Negocios/CrearCliente { Nombre: "", Email: "" }
→ must fail validation

# Invalid email format
POST /Negocios/CrearCliente { Email: "not-an-email" }
→ must fail

# Negative prices
POST /Negocios/CrearProducto { Precio: -10, Stock: -5 }
→ must fail

# Future end date before start date
POST /Negocios/CrearCliente { FechaInicio: "2026-03-01", FechaFin: "2026-02-01" }
→ must fail or warn

# Extremely long strings
POST /Negocios/CrearCliente { Nombre: "A" * 10000 }
→ must handle gracefully (truncate or reject)

# Special characters / XSS in fields
POST /Negocios/CrearCliente { Nombre: "<script>alert('xss')</script>" }
→ must be escaped in response

# SQL injection attempts
POST /Negocios/CrearCliente { Nombre: "'; DROP TABLE Clientes; --" }
→ must be parameterized (EF Core should handle this)
```

**Boundary conditions:**
```
# Zero-day membership
POST /Negocios/CrearCliente { FechaInicio: today, FechaFin: today }
→ verify handling

# Appointment at midnight boundary
POST /Negocios/CrearCita { FechaHoraInicio: "2026-02-20T23:45:00" }
→ verify works across day boundary

# Appointment in the past
POST /Negocios/CrearCita { FechaHoraInicio: "2025-01-01T10:00:00" }
→ should reject or warn

# Commission for exactly $15 and exactly 30 days (boundary)
POST /Negocios/VendedorCrearNegocio { PrecioMensual: 15.00, DiasPagados: 30 }
→ verify commission IS generated (>= condition)

# Commission for $14.99 and 29 days (just below boundary)
POST /Negocios/VendedorCrearNegocio { PrecioMensual: 14.99, DiasPagados: 29 }
→ verify NO commission
```

**Concurrent operations:**
```
# Rapid sequential requests (simulate concurrent users)
for i in 1..10; do
  curl POST /Negocios/VenderProducto { Cantidad: 1 } &
done
→ verify stock decremented exactly 10 (no race condition)

# Double-submit protection
POST /Negocios/RegistrarPagoCita { CitaId: X } (twice rapidly)
→ verify only one payment recorded
```

**Large data handling:**
```
# Create 100 clients rapidly
for i in 1..100; do
  POST /Negocios/CrearCliente { Nombre: "Stress Test $i" }
done
→ verify all created, list endpoint handles pagination

# Request large dataset
GET /Negocios/GetClientes with 100+ clients
→ verify response time reasonable, no timeout
```

**Error recovery:**
```
# Access non-existent IDs
GET /Negocios/GetCliente?clienteId=99999 → must return 404/null gracefully
GET /Negocios/GetProducto?productoId=99999 → must return 404/null
POST /Negocios/EditarCliente { ClienteId: 99999 } → must fail gracefully

# Delete then access
POST /Negocios/EliminarCliente { ClienteId: {id} }
GET /Negocios/GetCliente?clienteId={id} → must handle deleted entity
POST /Negocios/RenovarCliente { ClienteId: {id} } → must fail gracefully
```

**Suggestions:**
```
POST /Negocios/EnviarSugerencia {
  NegocioId: {id},
  Contenido: "Test suggestion from protocol 002"
}
→ verify success

# As admin
GET /Negocios/GetSugerencias → verify suggestion appears
POST /Negocios/MarcarSugerenciaLeida { sugerenciaId: {id} }
```

## Phase 2 — Fix Agents (2 Opus agents)

After all 6 test agents complete, their error reports are compiled. If ANY errors were found:

**Fix Agent A — Backend (Controllers + Services):**
- Receives all backend errors (500s, logic bugs, validation failures, data integrity issues)
- Reads the relevant controller and service files
- Implements fixes with proper error handling
- Verifies fixes compile with `dotnet build`

**Fix Agent B — Frontend (Views + JavaScript):**
- Receives all frontend errors (XSS, rendering issues, JS errors, broken UI)
- Reads the relevant .cshtml view files
- Implements fixes
- Verifies no syntax errors

Both fix agents:
- Use Opus model for maximum accuracy
- Provide file:line references for every change
- Explain WHY each fix is needed
- Run `dotnet build` after all fixes to verify compilation

## Error Report Format

Each test agent produces:
```
## [AGENT NAME] — Test Report

### TESTS RUN: X
### PASSED: X
### FAILED: X

### FAILURES (ordered by severity)

#### CRITICAL
1. [Endpoint] [Method] — Expected: X, Got: Y
   Status: HTTP {code}
   Response: { ... }
   Impact: [What could break in production]

#### HIGH
...

#### MEDIUM
...

### PASSED TESTS
- [List of all successful tests with response summaries]
```

## After Protocol Completes

Claude will:
1. Show a **consolidated dashboard** with pass/fail counts per agent
2. List all CRITICAL and HIGH failures
3. Confirm which errors the fix agents resolved
4. Run `dotnet build` to verify everything compiles
5. Optionally re-run failed tests to confirm fixes work
