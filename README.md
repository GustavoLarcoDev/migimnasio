# My-Negocio — Multi-Tenant Business Management SaaS

**Production SaaS platform built, launched, and operated solo (2025–2026).** My-Negocio gave small businesses across Latin America — gyms, salons, restaurants, and retail stores — a single web platform to run their entire operation: clients, appointments, sales, inventory, invoicing, and automated customer communication.

> El SaaS todo-en-uno para gestionar tu negocio: clientes, citas, ventas, inventario y facturación electrónica.

---

## Highlights

- **Multi-tenant architecture** — strict per-tenant data isolation (`NegocioId` claim filtering on every query), 4 business models served from one codebase
- **Role-based access control** — Owner / Admin / Seller / Employee roles, cookie auth with BCrypt password hashing (lazy migration)
- **Ecuador SRI electronic invoicing** — XAdES-BES digital signatures, SRI-compliant XML generation, QR-coded RIDE PDFs, background services with automatic retry ([docs/PROTOCOLO_SRI.md](docs/PROTOCOLO_SRI.md))
- **WhatsApp Cloud API automation** — 17+ notification templates: appointment reminders, membership expirations, payment confirmations
- **Appointment scheduling** — FullCalendar-based engine with conflict prevention and per-employee calendars
- **POS + inventory** — point of sale with digital receipts, stock control with audit trails, Excel import/export (ClosedXML)
- **Employee commissions** — per-sale commission tracking and payout reports
- **Real-time dashboards** — ApexCharts financial and operational analytics per tenant, daily email digest reports (MailKit)
- **Production deployment** — AWS Lightsail behind Nginx, GitHub Actions CI/CD, blue-green deployments, automated daily backups, Let's Encrypt HTTPS

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | C#, ASP.NET Core 9 MVC, Entity Framework Core 9 |
| Database | SQL Server |
| Auth | Cookie Authentication + BCrypt |
| Frontend | Bootstrap 5, Metronic v8, DataTables, ApexCharts, FullCalendar, Toastr, SweetAlert2 |
| Integrations | WhatsApp Cloud API, MailKit (SMTP), ClosedXML (Excel), SRI e-invoicing (XAdES-BES) |
| Infrastructure | AWS Lightsail, Nginx, GitHub Actions CI/CD, Let's Encrypt |

## Architecture

ASP.NET Core MVC with a layered structure — Controllers → Services → Data — plus hosted background services for invoicing retries, notifications, and daily reports. Every business-scoped query is filtered by the tenant claim; timezone handling is centralized (`TimeHelper`, UTC-5 Ecuador).

More detail in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), [docs/PROJECT_STRUCTURE.md](docs/PROJECT_STRUCTURE.md), and [docs/API_ENDPOINTS.md](docs/API_ENDPOINTS.md), including the ER diagram (`docs/modelos-er-diagram.excalidraw`).

## Running Locally

1. Copy `appsettings.template.json` → `appsettings.json` and fill in your connection string and credentials (never committed — see [docs/SETUP.md](docs/SETUP.md))
2. `dotnet ef database update`
3. `dotnet run`

## Status

The platform ran in production during 2025–2026 and is currently offline (the AWS environment was decommissioned as a business decision). The codebase is published as a portfolio piece; build artifacts and configuration were scrubbed from git history before publishing.

---

**Gustavo Larco** — Full Stack Software Engineer
[gustavolarcodev.github.io](https://gustavolarcodev.github.io) · [linkedin.com/in/gustavo-larco](https://linkedin.com/in/gustavo-larco) · gustavo.larcoj@gmail.com
