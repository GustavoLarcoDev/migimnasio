# My-Negocio — Project Instructions

## Language
- The user communicates in Spanish. Respond in Spanish unless code/technical terms are involved.
- Code comments, variable names, and commit messages can be in Spanish or English as contextually appropriate.

## Brand
- Always refer to the product as **My-Negocio** (hyphenated, capital M and N).

## Tech Stack
- ASP.NET Core 9.0 MVC, SQL Server, EF Core 9.0.10
- Cookie Authentication + BCrypt
- MailKit (Gmail SMTP), WhatsApp Cloud API
- Bootstrap 5, Metronic v8, DataTables, ApexCharts, Toastr, SweetAlert2, FullCalendar
- ClosedXML for Excel import/export

## Conventions
- All business dashboard controllers use `[Route("Negocios")]`
- Multi-tenant: always filter by `NegocioId` from claims
- Background services use `IServiceScopeFactory` for scoped services
- Timezone: Ecuador (UTC-5) — use `TimeHelper.Now` not `DateTime.Now`
- Never use `Task.Run` with scoped DbContext — query data first, then fire-and-forget only the email/notification call
- Theme system uses `[data-theme]` attribute on `<html>` with CSS custom properties
- Modals use `modal-fullscreen-sm-down` for mobile responsiveness

## Protocolo 001 — Intensive Production Audit

When the user says **"run protocolo 001"** or **"protocolo 001"**, execute the following:

Launch **8 parallel Opus agents** using the Task tool. Each agent must read ALL relevant files in its domain (not just samples) and produce a severity-categorized report (CRITICAL / HIGH / MEDIUM / LOW / PASSED).

**Agent 1 — Security Audit:**
```
Read ALL controllers, Program.cs, auth configuration, and middleware. Audit:
- Cookie config (HttpOnly, Secure, SameSite)
- BCrypt implementation safety
- SQL injection (raw queries, string interpolation in EF)
- XSS in Razor views (@Html.Raw, unescaped output)
- CSRF validation on all state-changing endpoints
- Multi-tenant isolation (NegocioId filtering on EVERY query)
- Rate limiting, security headers, secrets in source
- Admin impersonation security
Report format: CRITICAL/HIGH/MEDIUM/LOW with file:line references and fixes.
```

**Agent 2 — Database & Data Integrity:**
```
Read ApplicationDbContext, all migrations, all services that query the DB. Audit:
- Missing indexes, N+1 queries, missing FK constraints
- Orphaned records risk, cascade delete config
- Transaction boundaries, concurrency conflicts
- DbContext lifetime (scoped vs singleton misuse)
- Decimal precision for money, DateTime timezone consistency
- Nullable handling on required fields
Report format: CRITICAL/HIGH/MEDIUM/LOW with file:line references and fixes.
```

**Agent 3 — API & Controller Audit:**
```
Read ALL 15 controllers. Audit:
- Missing [Authorize], missing input validation
- Missing null checks (404 vs 500), missing ModelState.IsValid
- Inconsistent error responses, file upload security
- Route conflicts, missing pagination, exception handling
- HTTP method correctness, proper status codes
Report format: CRITICAL/HIGH/MEDIUM/LOW with file:line references and fixes.
```

**Agent 4 — Background Services & Email:**
```
Read all 4 background services, EmailService, WhatsAppService. Audit:
- Crash recovery, memory leaks, scope management
- Email/WhatsApp failure handling and retry logic
- Timezone correctness (Ecuador UTC-5)
- Race conditions, graceful shutdown, duplicate prevention
- Configuration validation, dead letter tracking
Report format: CRITICAL/HIGH/MEDIUM/LOW with file:line references and fixes.
```

**Agent 5 — Frontend & UI Audit:**
```
Read ALL .cshtml views, metronic-style.css, all JavaScript. Audit:
- JS errors (undefined vars, null refs), missing AJAX error handling
- Missing loading states, form validation alignment
- Mobile responsiveness, broken links, theme consistency
- DataTables config, accessibility, asset loading, DOM XSS
Report format: CRITICAL/HIGH/MEDIUM/LOW with file:line references and fixes.
```

**Agent 6 — Business Logic Audit:**
```
Read all services (Services/*.cs). Audit:
- Commission calculation ($5 flat, one-time, >=30 days, >=$15)
- Membership expiration logic, appointment conflict prevention
- Inventory stock negativity, financial rounding
- Notification generation, daily report accuracy
- Payment integrity, subscription/trial logic
- Edge cases: midnight, DST, business type separation
Report format: CRITICAL/HIGH/MEDIUM/LOW with file:line references and fixes.
```

**Agent 7 — Performance & Scalability:**
```
Read all controllers and services. Audit:
- Unbounded queries, missing AsNoTracking
- Large AJAX responses, memory allocation
- Static file caching, response compression
- Connection pool risk, sync-over-async
- Large file handling, background service resources
Report format: CRITICAL/HIGH/MEDIUM/LOW with file:line references and fixes.
```

**Agent 8 — Production Deployment:**
```
Read Program.cs, appsettings*.json, health check, middleware. Audit:
- Config separation (dev vs prod), health check completeness
- Auto-migration safety, logging config
- Custom error pages, HTTPS enforcement, CORS
- Startup failure modes, graceful degradation
- Dependency versions, known vulnerabilities
Report format: CRITICAL/HIGH/MEDIUM/LOW with file:line references and fixes.
```

After all 8 agents complete, compile a **consolidated report** counting issues by severity, then create a **prioritized fix list**. Ask the user if they want to auto-fix CRITICAL and HIGH issues.

See `docs/PROTOCOLO_001.md` for full protocol documentation.

## Protocolo 002 — Intensive System Testing with Fake Data

When the user says **"run protocolo 002"** or **"protocolo 002"**, execute the following:

This is a **3-phase testing pipeline** that creates fake data, tests every endpoint, tries to break the system, and auto-fixes errors.

### Phase 0 — Setup (sequential, before launching test agents)

```
1. Run `dotnet build` — verify 0 errors
2. Start the app: `dotnet run --project /Users/gustavolarco/Desktop/Gimnasio/Gimnasio.csproj --environment Development` in background
3. Wait for health check: curl http://localhost:5170/health (retry up to 30s)
4. Seed database: curl http://localhost:5170/Seed/Generate
5. Verify seed response contains business names
```

All test agents use `curl -b /tmp/p002-agentN.cookies -c /tmp/p002-agentN.cookies` for sessions and `TestAgentController` for JSON auth (no CSRF needed). Parse responses with `jq`. Report every test as PASS/FAIL with HTTP status and response body.

### Phase 1 — Launch 6 Test Agents in Parallel (all Opus)

**Test Agent 1 — Auth & Security Testing:**
```
Test ALL authentication flows and multi-tenant isolation.
BASE_URL=http://localhost:5170, COOKIES=/tmp/p002-agent1.cookies

Tests to run:
- POST /TestAgent/LoginAsAdmin → verify success+role
- POST /TestAgent/Login with valid membresias credentials (from seed: use iron@fitness.com or check seed emails) → verify role=Negocio
- POST /TestAgent/Login with valid artesanal credentials → verify role=Negocio, tipoNegocio=artesanal
- POST /TestAgent/Login with WRONG password → verify failure
- POST /TestAgent/Login with non-existent email → verify failure
- GET /TestAgent/WhoAmI → verify claims after each login
- Admin impersonation: LoginAsAdmin → Impersonate/{id} → WhoAmI → StopImpersonation → WhoAmI
- MULTI-TENANT ISOLATION (CRITICAL): Login as Business A, try GET /Negocios/GetClientes, GetProductos, GetLogs with Business B's NegocioId → must return empty/403
- Unauthorized access: Without login, GET protected endpoints → must return 401/302
- Role separation: As Negocio, try admin endpoints (GetVendedores, GetNegocios) → must fail

Read SeedController.cs to find the exact emails and credentials used for seed data.
Report: PASS/FAIL for each test with HTTP status code and response snippet.
```

**Test Agent 2 — Admin & Vendedor Testing:**
```
Test admin CRUD, vendor lifecycle, and commission system.
BASE_URL=http://localhost:5170, COOKIES=/tmp/p002-agent2.cookies

Login as admin: POST /TestAgent/LoginAsAdmin

Admin tests:
- GET /Negocios/GetNegocios → verify returns seeded businesses
- GET /Negocios/GetAdminDashboardStats → verify totalNegocios > 0
- GET /Negocios/GetAdminLogs → verify returns array
- POST /Negocios/Create new business → verify success, capture ID
- GET /Negocios/GetNegocio/{id} → verify created
- POST /Negocios/Editar → update name → verify
- POST /Negocios/CambiarEstado → toggle → verify
- POST /Negocios/Eliminar → delete → verify
- GET /Negocios/ExportExcel → verify returns file

Vendor tests:
- POST /Negocios/CrearVendedor with full bank details → capture vendedorId
- GET /Negocios/GetVendedores → verify vendor exists
- POST /Negocios/EditarVendedor → update → verify
- Login as vendor: POST /TestAgent/Login with vendor credentials
- GET /Negocios/VendedorDashboard stats
- POST /Negocios/VendedorCrearNegocio with PrecioMensual>=15, DiasPagados>=30 → verify commission auto-created
- POST /Negocios/VendedorCrearNegocio with PrecioMensual=10, DiasPagados=7 → verify NO commission
- Login as admin again, GET /Negocios/GetComisionesPendientes → verify pending with bank details
- GET /Negocios/GetResumenComisiones → verify montoPendiente=5
- POST /Negocios/PagarComisionesVendedor → verify success
- GET /Negocios/GetHistorialComisiones → verify paid record
- Cleanup: delete test vendor and businesses

Report: PASS/FAIL for each test.
```

**Test Agent 3 — Membresias Testing (Clients & Memberships):**
```
Test client CRUD, memberships, notifications, logs, sales, Excel.
BASE_URL=http://localhost:5170, COOKIES=/tmp/p002-agent3.cookies

Login as a membresias business from seed data.
Read SeedController.cs to find exact credentials.

Client CRUD:
- POST CrearCliente (full data) → capture clienteId
- GET GetCliente → verify fields match
- POST EditarCliente → change name → verify
- POST CrearCliente with PaseDiario=true → verify daily pass
- GET GetClientes → verify both appear
- GET GetClientesDiarios → verify daily client
- POST LimpiarClientesDiarios → verify cleared

Membership:
- POST RenovarCliente → extend date, change price → verify
- GET GetDashboardStats → verify counts and revenue
- GET GetSuscripcionStatus → verify subscription info

Notifications:
- POST GenerarNotificaciones → verify created
- GET GetNotificaciones → verify returned
- GET GetNotificacionesCount → verify count
- POST MarcarNotificacionLeida → verify
- POST MarcarTodasNotificacionesLeidas → verify

Logs:
- POST CrearLog (ingreso, $50) → verify
- POST CrearLog (gasto, $20) → verify
- GET GetLogs → verify both
- GET GetOldestLogDate → verify date
- POST EliminarTodosLogs → verify empty

Sales:
- GET GetVentasStats → verify structure
- GET GetChartData → verify structure
- GET GetClientesChartData → verify structure

Excel:
- GET ExportClientesExcel → verify returns file (check Content-Type)

Cleanup:
- POST EliminarCliente → verify deleted

Report: PASS/FAIL for each test.
```

**Test Agent 4 — Artesanal Testing (Appointments, Employees, Services):**
```
Test full artesanal flow: services, employees, schedules, appointments, payments.
BASE_URL=http://localhost:5170, COOKIES=/tmp/p002-agent4.cookies

Login as an artesanal business from seed data.
Read SeedController.cs to find exact credentials.

Services:
- POST CrearServicio → capture servicioId
- GET GetServicios → verify exists
- GET GetServicio → verify fields
- POST EditarServicio → update → verify
- Keep service for appointment tests

Employees:
- POST CrearEmpleado (with Email) → capture empleadoId
- GET GetEmpleados → verify
- GET GetEmpleado → verify fields
- POST EditarEmpleado → update → verify

Schedules:
- POST GuardarHorarios (Mon-Sat working, Sun off)
- GET GetHorarios → verify saved
- POST CrearExcepcionHorario (vacation day)
- POST EliminarExcepcionHorario

Clients:
- POST CrearClienteArtesanal → capture clienteId
- GET GetClientesArtesanal → verify
- POST EditarClienteArtesanal → verify
- GET BuscarClientes → search by name → verify

Appointments (full lifecycle):
- GET GetSlotsDisponibles → verify available slots
- POST CrearCitaRapida (walk-in) → capture citaId1
- POST CrearCita (with client) → capture citaId2
- GET GetCita → verify fields
- GET GetCitasCalendario → verify both appear
- POST CambiarEstadoCita → confirm citaId1
- POST MoverCita → reschedule citaId1
- POST CambiarEstadoCita → en_progreso
- POST RegistrarPagoCita → pay citaId1 → verify
- GET GetPagoCita → verify payment
- POST CambiarEstadoCita → cancel citaId2
- GET GetCitasDashboardStats → verify stats
- GET GetHistorialCliente → verify history

Double-booking:
- POST CrearCita same employee+time as existing → MUST fail or warn

Cleanup:
- POST EliminarServicio
- POST EliminarEmpleado

Report: PASS/FAIL for each test.
```

**Test Agent 5 — Inventario & Ventas Testing:**
```
Test product CRUD, stock operations, movements, financial stats.
BASE_URL=http://localhost:5170, COOKIES=/tmp/p002-agent5.cookies

Login as a membresias business from seed data.

Products:
- POST CrearProducto (Stock:100, Precio:45, Costo:25) → capture productoId
- GET GetProductos → verify
- GET GetProducto → verify fields
- POST EditarProducto → change price → verify

Stock operations:
- POST VenderProducto (qty:5) → verify stock=95
- POST DevolverProducto (qty:2) → verify stock=97
- POST RestockProducto (qty:50) → verify stock=147
- POST AjustarStock (newStock:100) → verify stock=100

Movements audit:
- GET GetMovimientos → verify 4 movements in order

Negative stock test (CRITICAL):
- POST VenderProducto (qty:999) → MUST fail or prevent negative

Stats:
- GET GetInventarioStats → verify totals
- GET ExportInventarioExcel → verify file returned

Cleanup:
- POST EliminarProducto → verify soft-deleted

Report: PASS/FAIL for each test.
```

**Test Agent 6 — Edge Cases, Stress & Boundary Testing:**
```
Test invalid data, XSS, SQL injection, boundaries, concurrent ops.
BASE_URL=http://localhost:5170, COOKIES=/tmp/p002-agent6.cookies

Login as admin first, create a test business, then login as that business.

Invalid data:
- POST CrearCliente with empty Nombre → must fail
- POST CrearCliente with invalid email → must fail or handle
- POST CrearProducto with negative price → must fail
- POST CrearCliente with FechaFin < FechaInicio → must fail or handle
- POST CrearCliente with Nombre="A"*10000 → must handle gracefully

XSS:
- POST CrearCliente with Nombre="<script>alert('xss')</script>" → must be escaped
- GET GetCliente → verify name is escaped in response

SQL injection:
- POST CrearCliente with Nombre="'; DROP TABLE Clientes; --" → must be safe (EF parameterizes)

Boundary conditions:
- POST CrearCliente with FechaInicio=today, FechaFin=today (0-day)
- POST CrearCita in the past → should reject
- Commission for exactly $15 / 30 days → must generate commission (>= boundary)
- Commission for $14.99 / 29 days → must NOT generate

Non-existent IDs:
- GET GetCliente?clienteId=99999 → must return null/404 gracefully
- POST EditarCliente { ClienteId: 99999 } → must fail gracefully
- POST RenovarCliente { ClienteId: 99999 } → must fail gracefully

Delete then access:
- Create client → delete → try to GET, edit, renew → all must handle gracefully

Suggestions:
- POST EnviarSugerencia → verify
- As admin: GET GetSugerencias → verify appears
- POST MarcarSugerenciaLeida → verify

Report: PASS/FAIL for each test with response details.
```

### Phase 2 — Fix Agents (only if errors found)

After ALL 6 test agents complete, compile results. If there are FAILED tests:

**Fix Agent A — Backend Fixes (Opus):**
```
You receive all FAILED test results from Phase 1 that involve backend issues
(HTTP 500, wrong data returned, missing validation, data integrity problems).

For each failure:
1. Read the relevant controller and service files
2. Identify the root cause
3. Implement the fix
4. Explain what was wrong and why

After all fixes: run `dotnet build` to verify 0 errors, 0 warnings.
```

**Fix Agent B — Frontend Fixes (Opus):**
```
You receive all FAILED test results from Phase 1 that involve frontend issues
(XSS not escaped, wrong HTTP redirects, broken responses, UI-related).

For each failure:
1. Read the relevant .cshtml view files
2. Identify the root cause
3. Implement the fix

After all fixes: run `dotnet build` to verify 0 errors, 0 warnings.
```

After fix agents complete, report:
- Total tests: X, Passed: X, Failed: X
- Errors fixed by Agent A: X
- Errors fixed by Agent B: X
- Remaining issues (if any): list with explanations

See `docs/PROTOCOLO_002.md` for full protocol documentation.
