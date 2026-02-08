# Guía de Instalación

## Prerrequisitos

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/sql-server) (o SQL Server Express / LocalDB)
- Editor de código (Visual Studio 2022, VS Code, Rider)

## Instalación

### 1. Clonar el repositorio

```bash
git clone <url-del-repositorio>
cd Gimnasio
```

### 2. Restaurar paquetes NuGet

```bash
dotnet restore
```

Paquetes principales:
- `BCrypt.Net-Next` - Hashing de contraseñas
- `ClosedXML` - Import/export Excel
- `Microsoft.EntityFrameworkCore.SqlServer` - ORM + SQL Server
- `Microsoft.EntityFrameworkCore.Tools` - Migraciones

### 3. Configurar la base de datos

Editar `appsettings.json` con tu cadena de conexión:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TU_SERVIDOR;Database=GimnasioDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 4. Configurar credenciales de admin

En `appsettings.json`, ajustar la sección `AdminSettings`:

```json
{
  "AdminSettings": {
    "Email": "admin@tudominio.com",
    "Password": "TuPasswordSeguro123!"
  }
}
```

### 5. Aplicar migraciones

```bash
dotnet ef database update
```

Esto crea las tablas: `Gimnasios`, `Clientes`, `Logs`, `Notificaciones`.

### 6. Ejecutar la aplicación

```bash
dotnet run
```

La aplicación estará disponible en:
- `https://localhost:5001` (HTTPS)
- `http://localhost:5000` (HTTP)

## Primer Uso

1. Navegar a `/Gimnasios/Login`
2. Ingresar con las credenciales de admin configuradas en `appsettings.json`
3. Crear un gimnasio desde el panel admin
4. Usar las credenciales del gimnasio para acceder al dashboard

## Solución de Problemas

### Error de conexión a SQL Server
Verificar que SQL Server esté corriendo y que la cadena de conexión sea correcta.

### Error en migraciones
```bash
dotnet ef migrations list        # Ver migraciones disponibles
dotnet ef database update        # Aplicar pendientes
```

### Puerto en uso
```bash
dotnet run --urls "https://localhost:5002"
```
