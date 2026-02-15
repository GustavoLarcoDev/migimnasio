# Setup Guide

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/sql-server) (SQL Server Express, LocalDB, or Docker container)
- Code editor (Visual Studio 2022, VS Code, JetBrains Rider)

## Installation

### 1. Clone the Repository

```bash
git clone <repository-url>
cd Gimnasio
```

### 2. Restore NuGet Packages

```bash
dotnet restore
```

Key packages:
- `BCrypt.Net-Next` (4.0.3) — Password hashing
- `ClosedXML` (0.105.0) — Excel import/export
- `Microsoft.EntityFrameworkCore.SqlServer` (9.0.10) — ORM + SQL Server provider
- `Microsoft.EntityFrameworkCore.Sqlite` (9.0.10) — SQLite provider (alternative)
- `Microsoft.EntityFrameworkCore.Tools` (9.0.10) — EF Core migrations CLI
- `HttpContextExtensions` (0.1.3) — HTTP context utilities

### 3. Configure the Database

Edit `appsettings.json` with your SQL Server connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=GimnasioDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**Docker SQL Server example:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=GimnasioDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;Encrypt=False"
  }
}
```

### 4. Configure Admin Credentials

In `appsettings.json`, set the `AdminSettings` section:

```json
{
  "AdminSettings": {
    "Email": "admin@yourdomain.com",
    "Password": "YourSecurePassword123!"
  }
}
```

These credentials are used for the system administrator login. The admin password is compared directly (not hashed) against the config values.

### 5. Configure WhatsApp (Optional)

If you want to enable WhatsApp Business API integration for automatic membership reminders and daily reports, configure the `WhatsAppSettings` section:

```json
{
  "WhatsAppSettings": {
    "PhoneNumberId": "your-phone-number-id",
    "AccessToken": "your-meta-access-token",
    "ApiVersion": "v21.0",
    "Enabled": true
  }
}
```

Set `Enabled` to `false` to disable WhatsApp messaging. The background services (MembershipReminderService and DailyReportService) will still run but skip sending when disabled.

### 6. Apply Database Migrations

```bash
dotnet ef database update
```

This creates the following tables:
- `Negocios` — Registered businesses
- `Clientes` — Clients per business
- `Logs` — Financial records (immutable)
- `Notificaciones` — Expiry alert notifications
- `Sugerencias` — Business feedback/suggestions
- `AdminLogs` — Admin action audit trail
- `Productos` — Inventory products
- `MovimientosInventario` — Inventory movement audit trail
- `Vendedores` — Salesperson accounts

### 7. Run the Application

```bash
dotnet run
```

The application will be available at:
- `https://localhost:5001` (HTTPS)
- `http://localhost:5000` (HTTP)

Or with a custom port:
```bash
dotnet run --urls "https://localhost:5002"
```

## First Use

1. Navigate to `/Negocios/Login`
2. Log in with the admin credentials configured in `appsettings.json`
3. You will see the admin dashboard with tabs for: Summary, Businesses, Create, Sales, Logs, and Suggestions
4. Create a new business from the "Create" tab (or navigate to `/Negocios/Create` for the public registration page)
5. Use the business credentials (email + password) to log in as that business
6. The business dashboard provides tabs for: Summary, Clients, Logs, Sales, Inventory, Suggestions, and Notifications

## Background Services

Two hosted background services run automatically:

1. **MembershipReminderService** — Runs daily at 8:00 AM (Ecuador timezone, America/Guayaquil). Sends WhatsApp reminders to clients whose membership expires in 3 or 1 day(s).

2. **DailyReportService** — Runs daily at 9:00 PM (Ecuador timezone). Sends a WhatsApp summary to each active business owner with daily income, new clients, and memberships expiring tomorrow.

Both services require `WhatsAppSettings.Enabled = true` and valid Meta Cloud API credentials to send messages.

## Security Headers

The application adds the following security headers in production:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`

## Troubleshooting

### SQL Server Connection Error
Verify that SQL Server is running and the connection string is correct:
```bash
# Test connection (if using Docker)
docker ps  # Check container is running
```

### Migration Errors
```bash
dotnet ef migrations list           # List available migrations
dotnet ef database update           # Apply pending migrations
dotnet ef database update 0         # Reset database (drops all tables)
```

### Port Already in Use
```bash
dotnet run --urls "https://localhost:5002"
```

### EF Core Tools Not Installed
```bash
dotnet tool install --global dotnet-ef
```

### BCrypt Password Issues
If a business cannot log in after a password change, verify that the password in the database is a BCrypt hash (starts with `$2a$` or `$2b$`). The lazy migration only works for plaintext passwords on first login.

### WhatsApp Messages Not Sending
1. Verify `WhatsAppSettings.Enabled` is `true` in appsettings.json
2. Check that `PhoneNumberId` and `AccessToken` are valid Meta Cloud API credentials
3. Review application logs for `MembershipReminderService` and `DailyReportService` entries
4. Ensure the target phone numbers are in international format (e.g., `593999999999`)
