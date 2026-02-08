# Estructura del Proyecto

```
Gimnasio/
├── Controllers/
│   ├── AuthController.cs          # Login, Logout (público)
│   ├── AdminController.cs         # CRUD gimnasios, panel admin (requiere auth)
│   ├── ClientesController.cs      # Dashboard, CRUD clientes, Excel import/export
│   ├── LogsController.cs          # CRUD logs, Excel export
│   ├── VentasController.cs        # Estadísticas y gráficos de ventas
│   ├── NotificacionesController.cs # Sistema de notificaciones de vencimiento
│   ├── HomeController.cs          # Landing page pública
│   └── GimnasiosController.cs     # [NonController] - controlador legacy desactivado
│
├── Data/
│   └── ApplicationDbContext.cs    # EF Core DbContext (Gimnasios, Clientes, Logs, Notificaciones)
│
├── Migrations/                    # Migraciones EF Core
│
├── Models/
│   ├── Gimnasio.cs                # Entidad Gym (gimnasio)
│   ├── Cliente.cs                 # Entidad Cliente + ClienteCreateModel
│   ├── Logs.cs                    # Entidad Logs + LogCreateModel
│   ├── Notificacion.cs            # Entidad Notificacion (vencimientos)
│   ├── AdminSettings.cs           # POCO config admin (email/password)
│   ├── ErrorViewModel.cs          # Modelo de errores
│   └── DTOs/
│       ├── ClienteCreateDto.cs    # DTO creación/edición de clientes
│       ├── LogCreateDto.cs        # DTO creación de logs
│       ├── LoginDto.cs            # DTO login
│       ├── GimnasioCreateDto.cs   # DTO creación de gimnasio
│       ├── GimnasioEditDto.cs     # DTO edición de gimnasio
│       └── RenovarClienteDto.cs   # DTO renovación de membresía
│
├── Services/
│   ├── IAuthService.cs            # Interfaz autenticación
│   ├── AuthService.cs             # Login BCrypt + lazy migration
│   ├── IGimnasioService.cs        # Interfaz CRUD gimnasios
│   ├── GimnasioService.cs         # CRUD gimnasios + Excel export
│   ├── IClienteService.cs         # Interfaz CRUD clientes
│   ├── ClienteService.cs          # CRUD clientes + stats + Excel
│   ├── ILogService.cs             # Interfaz CRUD logs
│   ├── LogService.cs              # CRUD logs + Excel export
│   ├── IVentasService.cs          # Interfaz estadísticas ventas
│   ├── VentasService.cs           # Stats ventas + datos gráficos
│   ├── INotificationService.cs    # Interfaz notificaciones
│   └── NotificationService.cs     # Generación auto de notificaciones
│
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml         # Layout público (landing, admin list)
│   │   └── _DashboardLayout.cshtml # Layout dashboard (sidebar + topbar)
│   ├── Home/
│   │   └── Index.cshtml           # Landing page pública
│   └── Gimnasios/
│       ├── Login.cshtml           # Página de login
│       ├── Index.cshtml           # Panel admin: listado de gimnasios
│       ├── Create.cshtml          # Formulario crear gimnasio (admin)
│       └── Dashboard.cshtml       # Dashboard del gimnasio (clientes, logs, ventas, notificaciones)
│
├── wwwroot/
│   ├── css/
│   │   ├── site.css               # CSS base del sitio
│   │   └── metronic-style.css     # Design system Metronic v8
│   ├── js/
│   │   └── site.js                # JS base del sitio
│   └── lib/                       # Librerías (Bootstrap, jQuery)
│
├── Program.cs                     # Punto de entrada, DI, middleware
├── appsettings.json               # Configuración (conexión DB, admin settings)
└── Gimnasio.csproj                # Archivo de proyecto .NET 9.0
```

## Capas de la Aplicación

1. **Controllers** - Reciben HTTP requests, validan auth, delegan a servicios
2. **Services** - Lógica de negocio, acceso a datos, operaciones complejas
3. **Models/DTOs** - Entidades de base de datos y objetos de transferencia
4. **Views** - Razor views con Bootstrap 5 + Metronic design system
5. **Data** - Entity Framework Core DbContext
