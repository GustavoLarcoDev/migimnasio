# My-Negocio — Complete System Documentation

## Overview

My-Negocio is a multi-tenant SaaS platform built with ASP.NET Core 9.0 MVC for managing businesses. It supports two business models:
- **Membresias** (gyms, studios): Client memberships with expiration dates, daily passes, renewals
- **Artesanal** (salons, barbershops, spas): Appointment-based with employees, services, calendar scheduling

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 9.0 / ASP.NET Core MVC |
| Database | SQL Server + Entity Framework Core 9.0.10 |
| Auth | Cookie Authentication + BCrypt password hashing |
| Email | MailKit via Gmail SMTP |
| WhatsApp | Meta Cloud API (background services) |
| Frontend | Bootstrap 5, Metronic v8, DataTables, ApexCharts, Toastr, SweetAlert2, FullCalendar |
| Excel | ClosedXML for import/export |
| Design | 4 color themes (Light, Dark, Ocean, Sunset) via CSS custom properties |

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        FRONTEND                              │
│  Razor Views (.cshtml) + jQuery AJAX + Bootstrap 5           │
│  Theme: Metronic v8 design system with 4 themes              │
├─────────────────────────────────────────────────────────────┤
│                      CONTROLLERS (15)                        │
│  Auth, Admin, Clientes, Citas, Empleados, Servicios,        │
│  Inventario, Logs, Ventas, Notificaciones, Vendedor,        │
│  Sugerencias, Home, Seed, TestAgent                          │
├─────────────────────────────────────────────────────────────┤
│                      SERVICES (21)                           │
│  Business logic layer — all interface + implementation       │
│  Background: MembershipReminder, DailyReport,                │
│  AppointmentReminder, DailyEmailReport                       │
├─────────────────────────────────────────────────────────────┤
│                      DATA LAYER                              │
│  ApplicationDbContext → SQL Server                            │
│  17+ DbSets, EF Core migrations, auto-migrate on startup    │
├─────────────────────────────────────────────────────────────┤
│                    EXTERNAL SERVICES                         │
│  Gmail SMTP (MailKit), WhatsApp Cloud API                    │
└─────────────────────────────────────────────────────────────┘
```

## User Roles & Auth Flow

| Role | Login Route | Dashboard | Capabilities |
|------|-----------|-----------|-------------|
| Admin | `/Auth/Login` | `/Negocios/Index` | CRUD businesses, vendors, commissions, impersonation, exports |
| Vendedor | `/Auth/LoginVendedor` | `/Vendedor/Dashboard` | Create businesses, view leads, earn commissions |
| Negocio (membresias) | `/Auth/Login` | `/Negocios/{id}/Dashboard` | Manage clients, memberships, inventory, sales, notifications |
| Negocio (artesanal) | `/Auth/Login` | `/Negocios/{id}/DashboardArtesanal` | Calendar, appointments, employees, services, payments |

## Multi-Tenancy

Every data query is filtered by `NegocioId` from the authenticated user's cookie claims. Controllers verify `_authService.GetNegocioId(User)` matches the request's `NegocioId` before any operation.

## Models (17 entities)

| Model | Purpose |
|-------|---------|
| Gym (Negocio) | Business entity — name, owner, email, phone, subscription, type |
| Cliente | Client of a business — membership dates, price, contact info |
| Logs | Financial/audit log per business |
| Notificacion | Membership expiry alerts (auto-generated) |
| Producto | Inventory items with stock tracking |
| MovimientoInventario | Immutable inventory audit trail |
| Sugerencia | Feedback from businesses to admin |
| AdminLog | Admin audit trail |
| Vendedor | Sales agents — credentials, bank info, commission tracking |
| ComisionVendedor | Commission records ($5 flat per qualifying business) |
| LeadVendedor | Sales leads tracked by vendors |
| Empleado | Staff for artesanal businesses — schedule, speciality, email |
| ServicioNegocio | Services offered by artesanal businesses |
| HorarioEmpleado | Weekly recurring schedule per employee |
| HorarioExcepcion | Date-specific schedule overrides |
| Cita | Appointments — client, employee, service, time, status, payment |
| PagoCita | Payment records for completed appointments |

## Background Services (4)

| Service | Schedule | Purpose |
|---------|----------|---------|
| MembershipReminderService | 8:00 AM Ecuador | WhatsApp alerts for memberships expiring in 3/1 days |
| DailyReportService | 9:00 PM Ecuador | WhatsApp daily financial summary to business owners |
| AppointmentReminderService | Every 5 min | WhatsApp + email reminders 30min before appointments |
| DailyEmailReportService | 11:00 PM Ecuador | Email daily report (revenue, clients, products, etc.) |

## Email System (12 email types)

| Method | Trigger | Recipient |
|--------|---------|-----------|
| EnviarBienvenidaVendedorAsync | Admin creates vendor | Vendor |
| EnviarBienvenidaNegocioAsync | Business created | Business owner |
| EnviarReporteDiarioMembresiaAsync | 11 PM daily | Business owner (membresias) |
| EnviarReporteDiarioArtesanalAsync | 11 PM daily | Business owner (artesanal) |
| EnviarReciboPagoNegocioAsync | Business pays SaaS | Business owner |
| EnviarReciboComisionAsync | Admin pays commission | Vendor |
| EnviarReciboPagoClienteAsync | Client pays membership | Client |
| EnviarConfirmacionReservaAsync | Appointment booked | Client |
| EnviarRecordatorioCitaEmailAsync | 30min before appt | Client |
| EnviarRecordatorioCitaEmpleadoAsync | 30min before appt | Employee |
| EnviarReciboCitaCompletadaAsync | Appointment paid | Client |

## Commission System

- **Formula**: Flat $5 per qualifying business
- **Requirements**: Business price >= $15 AND days >= 30 (not trial)
- **One-time**: Only one commission per business (no duplicates on renewal)
- **Flow**: Vendor creates business → commission auto-generated → Admin sees pending → Admin pays → Email receipt sent

## Security Measures

- Cookie auth with `HttpOnly`, `Secure` (prod), `SameSite=Strict`
- BCrypt password hashing with lazy migration from plaintext
- CSRF via `AutoValidateAntiforgeryToken`
- Rate limiting: 5/min login, 3/min leads (prod)
- Security headers: CSP, X-Frame-Options DENY, X-Content-Type-Options nosniff, HSTS
- No server header exposed
- Multi-tenant isolation on every query
- Admin impersonation with audit trail

## Deployment

- Auto-applies pending EF migrations on startup
- Health check at `/health` (checks DB + migration status)
- Fail-fast: exits with code 1 if migration fails
- ForwardedHeaders configured for Nginx reverse proxy
