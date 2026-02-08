# MiGimnasio

Software SaaS de gestión para gimnasios. Controla clientes, membresías, pagos, logs y notificaciones desde una plataforma web responsive.

## Funcionalidades

- **Gestión de Clientes** - CRUD completo con import/export Excel
- **Dashboard** - Estadísticas en tiempo real con gráficos interactivos
- **Control de Membresías** - Renovaciones, vencimientos, seguimiento de pagos
- **Logs de Actividad** - Registro de operaciones con montos
- **Notificaciones** - Alertas automáticas de membresías por vencer (3 días)
- **Panel Admin** - Gestión multi-gimnasio con estadísticas globales
- **Seguridad** - Contraseñas con BCrypt + lazy migration
- **Diseño Responsive** - Mobile-first con Metronic v8 design system

## Stack

ASP.NET Core 9.0 | Entity Framework Core | SQL Server | Bootstrap 5 | DataTables | ApexCharts

## Inicio Rápido

```bash
dotnet restore
# Editar appsettings.json con tu conexión a SQL Server
dotnet ef database update
dotnet run
```

Navegar a `/Gimnasios/Login` para acceder.

## Documentación

- [Estructura del Proyecto](docs/PROJECT_STRUCTURE.md)
- [API Endpoints](docs/API_ENDPOINTS.md)
- [Guía de Instalación](docs/SETUP.md)
- [Arquitectura](docs/ARCHITECTURE.md)
