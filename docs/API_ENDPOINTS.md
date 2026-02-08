# API Endpoints

Todos los endpoints usan la ruta base `/Gimnasios`.

## Autenticación (AuthController) - Público

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| GET | `/Gimnasios/Login` | Mostrar formulario login | No |
| POST | `/Gimnasios/Login` | Procesar login | No |
| GET | `/Gimnasios/Logout` | Cerrar sesión | No |

### POST /Gimnasios/Login
**Parámetros (form):**
- `email` (string, requerido)
- `password` (string, requerido)

**Respuesta:** Redirect a `/Gimnasios/Index` (admin) o `/Gimnasios/{id}/Dashboard` (gimnasio)

---

## Admin (AdminController) - Requiere Auth + Rol Admin

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/Gimnasios` | Panel admin (lista de gimnasios) |
| GET | `/Gimnasios/Index` | Alias del panel admin |
| GET | `/Gimnasios/GetGimnasios` | JSON: todos los gimnasios |
| GET | `/Gimnasios/GetGimnasio/{id}` | JSON: un gimnasio por ID |
| GET | `/Gimnasios/Crear` | Formulario crear gimnasio |
| POST | `/Gimnasios/Create` | Crear gimnasio |
| POST | `/Gimnasios/Editar` | Editar gimnasio |
| POST | `/Gimnasios/Eliminar` | Eliminar gimnasio |
| POST | `/Gimnasios/CambiarEstado` | Toggle activo/inactivo |
| GET | `/Gimnasios/ExportExcel` | Descargar Excel de gimnasios |

### POST /Gimnasios/Create
**Parámetros (form):**
- `NombreGimnasio` (string)
- `duenoGimnasio` (string)
- `telefono` (string)
- `EmailGimnasio` (string)
- `passwordGimnasio` (string)
- `isActive` (bool)
- `esPrueba` (bool)

### POST /Gimnasios/Editar
**Parámetros (form):** Objeto `Gym` completo (GimnasioId, GimnasioNombre, DuenoGimnasio, Email, Telefono, Password, IsActive, EsPrueba)

### POST /Gimnasios/Eliminar | POST /Gimnasios/CambiarEstado
**Parámetros (form):** `id` (string)

---

## Clientes (ClientesController) - Requiere Auth

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/Gimnasios/{id}/Dashboard` | Dashboard del gimnasio |
| GET | `/Gimnasios/GetDashboardStats` | JSON: estadísticas del dashboard |
| GET | `/Gimnasios/GetClientes` | JSON: lista de clientes |
| GET | `/Gimnasios/GetCliente` | JSON: un cliente por ID |
| POST | `/Gimnasios/CrearCliente` | Crear cliente |
| POST | `/Gimnasios/EditarCliente` | Editar cliente |
| POST | `/Gimnasios/EliminarCliente` | Eliminar cliente |
| POST | `/Gimnasios/RenovarCliente` | Renovar membresía |
| GET | `/Gimnasios/ExportClientesExcel` | Descargar Excel de clientes |
| POST | `/Gimnasios/ImportarClientesExcel` | Importar clientes desde Excel |
| GET | `/Gimnasios/GetClientesDiarios` | JSON: clientes registrados hoy |

### GET /Gimnasios/GetDashboardStats
**Parámetros (query):** `gimnasioId` (string)
**Respuesta:**
```json
{
  "totalClientes": 50,
  "clientesActivos": 35,
  "clientesVencidos": 15,
  "ingresosDelMes": 5000.00
}
```

### POST /Gimnasios/CrearCliente
**Parámetros (form):** `ClienteCreateDto` (GimnasioId, Nombre, Telefono, Precio, Dias, FechaInicio)

### POST /Gimnasios/RenovarCliente
**Parámetros (form):** `id`, `gimnasioId`, `dias` (int), `precio` (decimal)

### POST /Gimnasios/ImportarClientesExcel
**Parámetros (form):** `gimnasioId` (string), `file` (IFormFile - archivo .xlsx)

---

## Logs (LogsController) - Requiere Auth

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/Gimnasios/CrearLog` | Crear log manual |
| GET | `/Gimnasios/GetLogs` | JSON: lista de logs |
| GET | `/Gimnasios/GetLog` | JSON: un log por ID |
| POST | `/Gimnasios/EditarLog` | Editar log |
| POST | `/Gimnasios/EliminarLog` | Eliminar log |
| POST | `/Gimnasios/EliminarTodosLogs` | Eliminar todos los logs |
| GET | `/Gimnasios/ExportLogsExcel` | Descargar Excel de logs |

### POST /Gimnasios/CrearLog
**Parámetros (form):** `LogCreateDto` (GimnasioId, Message, Monto)

### GET /Gimnasios/GetLogs
**Parámetros (query):** `gimnasioId` (string)

---

## Ventas (VentasController) - Requiere Auth

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/Gimnasios/GetVentasStats` | JSON: estadísticas de ventas |
| GET | `/Gimnasios/GetChartData` | JSON: datos para gráficos |

### GET /Gimnasios/GetVentasStats
**Parámetros (query):** `gimnasioId` (string)
**Respuesta:**
```json
{
  "ventasHoy": 500.00,
  "ventasMes": 12000.00,
  "ventasAnio": 144000.00
}
```

### GET /Gimnasios/GetChartData
**Parámetros (query):** `gimnasioId` (string), `periodo` (string: "semana" | "mes" | "anio", default: "semana")
**Respuesta:**
```json
{
  "labels": ["Lun", "Mar", "Mié", ...],
  "data": [500, 300, 800, ...]
}
```

---

## Notificaciones (NotificacionesController) - Requiere Auth

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/Gimnasios/GetNotificaciones` | JSON: lista de notificaciones |
| GET | `/Gimnasios/GetNotificacionesCount` | JSON: conteo de no leídas |
| POST | `/Gimnasios/MarcarNotificacionLeida` | Marcar una como leída |
| POST | `/Gimnasios/MarcarTodasNotificacionesLeidas` | Marcar todas como leídas |
| POST | `/Gimnasios/GenerarNotificaciones` | Generar notificaciones de vencimiento |

### GET /Gimnasios/GetNotificacionesCount
**Parámetros (query):** `gimnasioId` (string)
**Respuesta:**
```json
{
  "count": 5
}
```

### POST /Gimnasios/GenerarNotificaciones
**Parámetros (form):** `gimnasioId` (string)
Genera automáticamente notificaciones para clientes cuya membresía vence en los próximos 3 días.
