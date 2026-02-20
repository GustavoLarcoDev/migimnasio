# GUIA DE TESTING #1 — ADMINISTRADOR

## Persona: [Nombre del tester] | Rol: Administrador General

---

## OBJETIVO

El administrador es el "dios" del sistema My-Negocio. Desde su panel puede:

- Ver todos los negocios registrados en la plataforma con sus estadísticas
- Crear negocios de cualquier tipo (membresías, artesanal, tienda, restaurante)
- Gestionar vendedores y sus datos bancarios
- Ver y pagar comisiones a vendedores
- "Entrar" (impersonar) cualquier negocio para dar soporte sin conocer su contraseña
- Ver sugerencias enviadas por los negocios
- Exportar datos a Excel
- Ver logs de todas las acciones realizadas en el sistema
- Ver y emitir recibos de suscripciones y comisiones

El flujo de impersonación tiene dos niveles: Admin → Vendedor → Negocio. En cada nivel se puede volver atrás sin necesidad de re-ingresar contraseñas.

---

## ANTES DE EMPEZAR

### URL del sistema
```
http://localhost:5170
```
(Si el sistema está en producción, usar la URL de producción correspondiente.)

### Credenciales del administrador
```
Email:    gustavo.larco@mynegocio.com
Password: (la contraseña original, no el hash — consultar al desarrollador)
```

> Nota técnica: la contraseña está almacenada como hash BCrypt en `appsettings.json` bajo la clave `AdminSettings.PasswordHash`. El email es `gustavo.larco@mynegocio.com`.

### Lo que cambia al loguearse como admin
Cuando el admin inicia sesión, el sistema detecta el rol `Admin` en la cookie de sesión y redirige automáticamente al panel de administración en `/Negocios`. El sidebar del admin muestra opciones distintas a las de un negocio normal: Resumen, Negocios, Crear Negocio, Ventas, Logs, Vendedores, Comisiones, Recibos y Sugerencias.

---

## SECCION 1: LANDING PAGE (antes de loguearse)

### Paso a paso
1. Abrir el navegador y ir a `http://localhost:5170` (o simplemente a `/`)
2. Verificar que se carga la landing page pública de My-Negocio

### Qué se debe ver en la landing page

**Hero principal (parte superior):**
- Fondo con gradiente oscuro y efecto de grid
- Dos "orbs" decorativos flotantes en el fondo
- Título principal: "Tu negocio. Organizado. Rentable."
- Texto descriptivo mencionando membresías, citas y reservaciones, y punto de venta
- Badge amarillo que dice "NUEVO" con el texto "Ahora con punto de venta y recibos digitales"
- Botón amarillo grande: "Probar gratis 7 dias" (lleva a la sección de contacto `#contacto`)
- Botón de contorno claro: "Escribenos" (abre WhatsApp al número +593997143142)
- Tres checks verdes: "Sin tarjeta requerida", "Setup en 5 minutos", "Cancela cuando quieras"
- Mockup flotante de un dashboard con estadísticas animadas ($4,280 ingresos, 156 clientes)
- Animaciones de actividad reciente: renovación de membresía, cita confirmada, pago POS

**Los 4 modelos de negocio (sección "Elige como funciona tu negocio"):**

1. **Membresías** (borde azul `#3E97FF`) — para gimnasios, yoga, CrossFit, artes marciales, piscinas, academias. Badges: "Control de pagos", "Vencimientos", "Recordatorios"

2. **Citas / Reservaciones** (borde violeta `#7239EA`) — para barberías, peluquerías, salones de belleza, spas, estudios de tatuaje, consultorios dentales. Badges: "Agenda visual", "Empleados", "Servicios"

3. **Tienda / POS** (borde naranja `#F97316`) — para minimarkets, tiendas de ropa, librerías, bazares. Badges: "Punto de venta", "Recibos", "Catálogo"

4. **Restaurante** (borde rojo `#dc3545`) — para restaurantes, cafeterías, food trucks, cocinas fantasma, pizzerías. Badges: "POS", "Menu Digital", "Delivery", "Mesas"

**Showcase interactivo de dashboards:**
- Sección con un mockup de navegador que muestra el dashboard
- 4 botones para cambiar entre los tipos: "Membresias", "Citas / Artesanal", "Tienda / POS", "Restaurante"
- Hacer clic en cada botón cambia el panel mostrado — verificar que todos funcionan

**Formulario de contacto/lead:**
- Sección identificada con el id `#contacto` (al que lleva el botón amarillo del hero)
- Formulario para que los interesados dejen sus datos

**Botón de WhatsApp:**
- Botón "Escribenos" en el hero abre `https://wa.me/593997143142` en una pestaña nueva

---

## SECCION 2: LOGIN Y DASHBOARD ADMIN

### Paso a paso

**Paso 1 — Ir al login:**
Navegar a `http://localhost:5170/Auth/Login`

**Paso 2 — Ingresar credenciales:**
- Campo "Email": `gustavo.larco@mynegocio.com`
- Campo "Password": (contraseña del admin)
- Hacer clic en el botón de login

**Paso 3 — Verificar redirección:**
Al loguearse como admin, el sistema redirige automáticamente a `/Negocios` (el panel de administración), NO al dashboard de un negocio.

### Qué se ve en el dashboard admin

**Topbar (barra superior):**
- Nombre "Administrador" a la izquierda
- Botón de paleta de colores (cambio de tema) a la derecha
- Hamburger menu para sidebar en móvil

**Sidebar (menú lateral izquierdo) — opciones disponibles para el admin:**
- **Resumen** — estadísticas generales de la plataforma (tab activo por defecto)
- **Negocios** — tabla con todos los negocios
- **Crear Negocio** — formulario de alta de nuevo negocio
- **Ventas** — métricas financieras del SaaS
- **Logs** — registro de actividad del admin
- **Vendedores** — gestión de la red de vendedores
- **Comisiones** — comisiones pendientes e historial
- **Recibos** — recibos de suscripciones y comisiones
- **Sugerencias** — feedback enviado por los negocios
- **Cerrar Sesión** — cierra la sesión actual

**Tab Resumen — tarjetas de estadísticas (visibles de inmediato):**

Tarjeta grande con gradiente azul/violeta:
- **Ingresos Recurrentes (MRR)** — suma de precios de todos los negocios activos de pago

Cuatro tarjetas pequeñas en fila:
- **Total Negocios** — cantidad total de negocios registrados en la plataforma
- **Activos** — negocios de pago con suscripción activa
- **En Prueba** — negocios en período de prueba gratuita
- **Total Clientes** — suma de todos los clientes de todos los negocios

Segunda fila de tarjetas:
- **Ingresos Mensuales** (tarjeta verde) — ingresos del mes en curso
- **Por Vencer (7 días)** — negocios cuya suscripción vence en los próximos 7 días
- **Expirados** — negocios con suscripción ya vencida

**Gráfico de barras — "Ingresos Mensuales":**
- Gráfico de barras ApexCharts mostrando ingresos de los últimos meses
- Verificar que se renderiza correctamente (no debe aparecer en blanco ni dar error)

**Saludo dinámico:**
- El texto "Buen día" / "Buenas tardes" / "Buenas noches" cambia según la hora actual

---

## SECCION 3: CREAR UN NEGOCIO (probar los 4 tipos)

### Acceder al formulario de creación

Desde el sidebar, hacer clic en **"Crear Negocio"** (o desde la pestaña Negocios, clic en el botón azul **"Nuevo Negocio"**).

Se muestra el formulario de alta de negocio dividido en dos columnas.

### Campos del formulario — Columna izquierda

| Campo | Descripción | Ejemplo |
|-------|-------------|---------|
| **Nombre del Negocio** | Nombre comercial visible | `Fitness Plus` |
| **Nombre del Dueño** | Nombre completo del propietario | `Juan Pérez` |
| **Teléfono** | Número de contacto principal | `+593 99 123 4567` |

### Campos del formulario — Columna derecha

| Campo | Descripción | Ejemplo |
|-------|-------------|---------|
| **Email** | Correo para acceso al sistema | `juan@fitnessplus.com` |
| **Contraseña** | Password inicial (mínimo 6 caracteres) | `123456` |
| Ojo (icono) | Botón para mostrar/ocultar la contraseña | — |

### Selector de Modelo de Negocio (4 opciones tipo tarjeta)

Hacer clic en una de las 4 tarjetas visuales para seleccionar el modelo. La tarjeta seleccionada se resalta con borde azul y fondo azul claro:

- **Membresías** (icono calendario azul) — Gimnasios, yoga, etc.
- **Artesanal** (icono tijeras verde) — Barberías, uñas, etc.
- **Tienda** (icono tienda amarillo) — Ropa, productos, etc.
- **Restaurante** (icono taza rojo) — Comida, delivery, etc.

### Selector de Tipo de Negocio (radio buttons)

- **Negocio de Pago (Activo)** — el negocio realizará pagos regulares (radio preseleccionado)
- **Negocio de Prueba** — 7 días de prueba gratis, sin costo

### Sección de Suscripción (debajo de la línea divisoria)

| Campo | Descripción | Ejemplo (pago) |
|-------|-------------|----------------|
| **Fecha de Inicio** | Inicio de la suscripción (date picker) | Fecha de hoy |
| **Fecha de Expiración** | Cuándo vence la suscripción (date picker) | Fecha de hoy + 30 días |
| **Precio Suscripción ($)** | Monto mensual a cobrar | `20.00` |
| **Días Pagados** | Días que cubre el pago (editar recalcula la fecha de expiración) | `30` |

> Nota: Si se selecciona "Negocio de Prueba", los campos de suscripción pueden dejarse vacíos.

### Botones del formulario
- **"Cancelar"** — regresa a la pestaña anterior sin guardar
- **"Crear Negocio"** (botón azul primario) — envía el formulario

### Prueba 1 — Negocio tipo Membresías (de pago)

1. Nombre del Negocio: `Gym Demo Membresías`
2. Nombre del Dueño: `Ana García`
3. Teléfono: `+593 99 000 0001`
4. Email: `ana.membresias@demo.com`
5. Contraseña: `demo123`
6. Seleccionar modelo: **Membresías** (clic en la tarjeta del calendario azul)
7. Tipo de Negocio: **Negocio de Pago (Activo)** (ya preseleccionado)
8. Fecha de Inicio: (hoy)
9. Fecha de Expiración: (hoy + 30 días)
10. Precio Suscripción: `20.00`
11. Días Pagados: `30`
12. Clic en **"Crear Negocio"**

**Resultado esperado:** Mensaje de éxito tipo toast verde/SweetAlert2 que dice el negocio fue creado. El sistema envía un email de bienvenida a `ana.membresias@demo.com` con las credenciales y una guía específica para negocios de membresías.

### Prueba 2 — Negocio tipo Artesanal (en prueba)

1. Nombre del Negocio: `Barbería Demo`
2. Nombre del Dueño: `Carlos López`
3. Teléfono: `+593 99 000 0002`
4. Email: `carlos.barberia@demo.com`
5. Contraseña: `demo123`
6. Seleccionar modelo: **Artesanal** (clic en la tarjeta de tijeras verde)
7. Tipo de Negocio: **Negocio de Prueba**
8. (Los campos de suscripción pueden quedar vacíos para un negocio de prueba)
9. Clic en **"Crear Negocio"**

**Resultado esperado:** Mensaje de éxito. Email de bienvenida enviado con guía para negocios artesanales (citas/agenda).

### Prueba 3 — Negocio tipo Tienda (de pago)

1. Nombre del Negocio: `Tienda Demo POS`
2. Nombre del Dueño: `María Rodríguez`
3. Teléfono: `+593 99 000 0003`
4. Email: `maria.tienda@demo.com`
5. Contraseña: `demo123`
6. Seleccionar modelo: **Tienda** (clic en la tarjeta de tienda amarilla)
7. Tipo de Negocio: **Negocio de Pago (Activo)**
8. Fecha de Inicio: (hoy)
9. Precio Suscripción: `15.00`
10. Días Pagados: `30`
11. Clic en **"Crear Negocio"**

**Resultado esperado:** Mensaje de éxito. Email de bienvenida enviado con guía para tiendas/POS. Adicionalmente se genera y envía un recibo de pago de suscripción al email del negocio.

### Prueba 4 — Negocio tipo Restaurante (en prueba)

1. Nombre del Negocio: `Restaurante Demo`
2. Nombre del Dueño: `Pedro Martínez`
3. Teléfono: `+593 99 000 0004`
4. Email: `pedro.restaurante@demo.com`
5. Contraseña: `demo123`
6. Seleccionar modelo: **Restaurante** (clic en la tarjeta de taza roja)
7. Tipo de Negocio: **Negocio de Prueba**
8. Clic en **"Crear Negocio"**

**Resultado esperado:** Mensaje de éxito. Email de bienvenida enviado con guía para restaurantes.

### Verificar que aparecen en la tabla

Hacer clic en **"Negocios"** en el sidebar. La tabla debe mostrar los 4 negocios recién creados con columnas:
- **Negocio** — nombre del negocio
- **Dueño** — nombre del propietario
- **Contacto** — email y teléfono
- **Clientes** — cantidad de clientes registrados (0 al inicio)
- **Vendedor** — nombre del vendedor asignado (vacío si fue creado por el admin)
- **Estado** — badge de color: "Activo" (verde), "Prueba" (amarillo), u otro estado
- **Suscripción** — precio y días pagados
- **Expira** — fecha de expiración de la suscripción
- **Acciones** — botones de acción (ver más abajo)

---

## SECCION 4: GESTIONAR NEGOCIOS DESDE LA TABLA

### Acciones disponibles por cada fila en la tabla de Negocios

Cada fila de la tabla tiene un menú de acciones a la derecha con los siguientes botones:

- **"Entrar"** (botón azul) — impersona el negocio y accede a su dashboard
- **"Editar"** (icono lápiz) — abre el modal de edición del negocio
- **"Cambiar Estado"** (icono toggle) — alterna entre "De Pago" y "En Prueba"
- **"Eliminar"** (icono basura rojo) — elimina el negocio (solo si no tiene clientes)

### Editar un negocio

1. En la tabla de Negocios, clic en el icono de **lápiz (Editar)** en la fila del negocio
2. Se abre el **modal "Editar Negocio"** con los campos pre-llenados:
   - **Nombre del Negocio** (campo de texto, requerido)
   - **Nombre del Dueño** (campo de texto, requerido)
   - **Email** (campo de email, requerido)
   - **Teléfono** (campo de teléfono, requerido)
   - **Contraseña** — dejar en blanco para mantener la contraseña actual; llenar solo si se quiere cambiar
   - **Tipo de Negocio** — radio: "De Pago (Activo)" o "De Prueba"
   - Sección **Suscripción**: Fecha de pago, Fecha expiración, Días pagados, Precio suscripción ($)
3. Clic en **"Guardar Cambios"** (botón azul en el pie del modal)
4. Resultado esperado: toast de éxito y la tabla se actualiza con los nuevos datos

### Cambiar estado de un negocio

1. Clic en el icono de **toggle (Cambiar Estado)** en la fila del negocio
2. Si el negocio estaba "De Pago", pasa a "En Prueba" y viceversa
3. El badge de estado en la tabla se actualiza sin recargar la página
4. La acción queda registrada en el log de auditoría

### Eliminar un negocio

1. Clic en el icono de **basura roja (Eliminar)** en la fila del negocio
2. Se muestra un SweetAlert2 de confirmación: "¿Estás seguro que deseas eliminar este negocio?"
3. Clic en **"Sí, eliminar"**
4. Resultado esperado: si el negocio no tiene clientes, se elimina y desaparece de la tabla
5. Si el negocio tiene clientes, el sistema muestra un error: no se puede eliminar

> Nota: Para la demo, eliminar uno de los negocios de prueba creados en la Sección 3.

---

## SECCION 5: GESTIONAR VENDEDORES

### Acceder a la pestaña de Vendedores

En el sidebar, clic en **"Vendedores"**. Se muestra la tabla de vendedores con columnas:
- **Nombre** — nombre completo del vendedor
- **Correo** — email del vendedor
- **Teléfono** — teléfono de contacto
- **Negocios Activos** — cantidad de negocios que ha creado
- **Fecha** — fecha de registro
- **Acciones** — botones de editar, impersonar y eliminar

### Crear un vendedor

1. Clic en el botón **"Nuevo Vendedor"** (esquina superior derecha de la sección Vendedores)
2. Se abre el **modal "Nuevo Vendedor"** con los campos:

   **Datos personales:**
   - **Nombre** (requerido) — ej: `Carlos`
   - **Apellido** (requerido) — ej: `Vega`
   - **Correo** (requerido) — ej: `carlos.vega@vendedores.com`
   - **Teléfono** (requerido) — ej: `+593 99 555 0001`
   - **Contraseña** — mínimo 6 caracteres — ej: `vend123`

   **Datos para Transferencia (Opcional — para poder pagar comisiones):**
   - **Nombre del Banco** — ej: `Banco Pichincha`
   - **Número de Cédula** — ej: `1234567890`
   - **Número de Cuenta** — ej: `2200123456789`

3. Clic en **"Guardar"** (botón azul en el pie del modal)
4. Resultado esperado: toast de éxito, el vendedor aparece en la tabla, y se envía un email de bienvenida al correo del vendedor con sus credenciales

### Editar un vendedor

1. En la tabla de vendedores, clic en el icono de **lápiz (Editar)**
2. El modal se abre pre-llenado con los datos actuales del vendedor
3. Modificar cualquier campo — ej: cambiar el número de cuenta bancaria
4. **Contraseña**: dejar en blanco para mantener la actual; llenar solo si se quiere cambiar
5. Clic en **"Guardar"**

### Ver el dashboard del vendedor desde el admin (impersonar vendedor)

1. En la tabla de vendedores, clic en el botón de **"Ver como Vendedor"** (icono de ojo)
2. El admin adopta la identidad del vendedor: la cookie de sesión cambia al rol "Vendedor"
3. Se redirige al **Panel Vendedor** (`/Negocios/VendedorDashboard`)
4. Aparece un banner anaranjado en la parte superior: **"Estás viendo como vendedor: [Nombre del Vendedor]"** con el botón **"Volver al Panel Admin"**
5. Se puede navegar por todas las secciones del panel del vendedor:
   - **Resumen** — estadísticas del vendedor (negocios creados, activos, en prueba, total clientes)
   - **Mis Negocios** — tabla de negocios que el vendedor ha creado
   - **Crear Negocio** — formulario para que el vendedor cree nuevos negocios
6. Para volver, clic en **"Volver al Panel Admin"** en el banner — el sistema restaura la sesión del admin automáticamente

---

## SECCION 6: COMISIONES

### Cómo se generan las comisiones

El sistema genera automáticamente una comisión de **$5** para el vendedor cuando:
- El negocio que creó tiene **Precio de Suscripción >= $15**, Y
- **Días Pagados >= 30**

Si alguna de las dos condiciones no se cumple (ej: precio $10 o días 7), **no se genera comisión**.

### Probar la generación de comisiones

Para probar esto correctamente:

1. Primero crear un vendedor (ver Sección 5)
2. Impersonar al vendedor (clic en "Ver como Vendedor")
3. En el Panel Vendedor, ir a **"Crear Negocio"**
4. Llenar el formulario:
   - Nombre: `Negocio Con Comisión`
   - Dueño: `Test Comisión`
   - Email: `comision@test.com`
   - Contraseña: `demo123`
   - Tipo de Negocio: **De Pago** (no prueba)
   - Duración: **30 días** (o más)
   - Precio: `$20.00` (mayor o igual a $15)
5. Clic en **"Crear Negocio"**
6. Volver al Panel Admin (clic en "Volver al Panel Admin" en el banner)

### Ver comisiones pendientes

1. En el sidebar del admin, clic en **"Comisiones"**
2. Se muestran 4 tarjetas en la parte superior:
   - **Pendiente Total** (amarillo) — suma total de comisiones aún no pagadas
   - **Pagado Total** (verde) — suma histórica de comisiones ya pagadas
   - **Vendedores Activos** — cantidad de vendedores que tienen comisiones
   - **Negocios con Comisión** — total de negocios que generaron comisiones
3. Debajo, la sección **"Comisiones Pendientes por Vendedor"** muestra una tarjeta por vendedor que tiene comisiones pendientes, con:
   - Nombre del vendedor
   - Datos bancarios (banco, número de cédula, número de cuenta — para hacer la transferencia)
   - Lista de negocios que generaron la comisión (nombre, precio pagado, días)
   - Monto total pendiente
   - Botón **"Pagar $X.XX"** para marcar todas las comisiones del vendedor como pagadas

### Pagar comisiones a un vendedor

1. En la tarjeta del vendedor en la sección "Comisiones Pendientes por Vendedor", clic en **"Pagar $X.XX"**
2. Se muestra un SweetAlert2 de confirmación mostrando el monto total y el detalle de negocios
3. Clic en **"Sí, pagar"**
4. Resultado esperado:
   - Las comisiones se marcan como pagadas en la base de datos
   - Se envía un recibo de comisión al email del vendedor
   - El monto pasa de "Pendiente Total" a "Pagado Total"
   - La tarjeta del vendedor desaparece de "Comisiones Pendientes"

### Ver historial de comisiones pagadas

En la misma pestaña de Comisiones, la sección inferior **"Historial de Pagos"** muestra todos los pagos de comisiones realizados históricamente con:
- Fecha del pago
- Nombre del vendedor
- Monto pagado
- Detalle de negocios incluidos

---

## SECCION 7: IMPERSONAR NEGOCIOS

Esta función permite al admin "entrar" a cualquier negocio para ver exactamente lo que el dueño del negocio ve, sin necesitar conocer su contraseña. Es esencial para dar soporte técnico.

### Paso a paso

1. Ir a la pestaña **"Negocios"** en el sidebar del admin
2. En la tabla, encontrar el negocio al que se quiere entrar
3. Clic en el botón **"Entrar"** (botón azul) en la columna Acciones de esa fila
4. El sistema:
   - Reemplaza la cookie de sesión del admin con una del negocio impersonado
   - Registra la acción en el log de auditoría
   - Redirige al dashboard del negocio correspondiente

### Qué se ve al impersonar un negocio

Aparece un **banner oscuro** en la parte superior de la pantalla con el texto:
```
"Estás viendo como: [Nombre del Negocio]"
```
y un botón **"Volver al Admin"**.

El sidebar del dashboard cambia según el tipo de negocio:

**Para negocio tipo Membresías:**
Resumen, Clientes, Movimientos, Reportes, Inventario, Recibos, Notificaciones, Sugerencias

**Para negocio tipo Artesanal:**
Agenda, Clientes, Servicios, Empleados, Movimientos, Reportes, Inventario, Recibos, Notificaciones, Sugerencias

**Para negocio tipo Tienda:**
POS, Inventario, Proveedores, Catálogo, Movimientos, Reportes, Recibos, Notificaciones, Sugerencias

**Para negocio tipo Restaurante:**
POS, Mesas, Menus, Inventario, Distribuidores, Repartidores, Movimientos, Reportes, Recibos, Notificaciones, Sugerencias

### Volver al Panel Admin

1. Con el banner de impersonación visible, clic en el botón **"Volver al Admin"**
2. El sistema restaura la sesión del admin (lee el email del admin que se guardó en los claims al iniciar la impersonación)
3. Redirige automáticamente al panel de administración `/Negocios`
4. El banner de impersonación desaparece

> Importante verificar: después de volver, el usuario en la topbar debe volver a decir "Administrador" y el sidebar debe volver a mostrar las opciones de admin.

---

## SECCION 8: FUNCIONES ADICIONALES

### 8.1 — Ver sugerencias de los negocios

Los negocios pueden enviar sugerencias o comentarios sobre el sistema desde su propio panel (pestaña "Sugerencias"). El admin las ve aquí.

1. Clic en **"Sugerencias"** en el sidebar del admin
2. Se muestran dos tarjetas de estadísticas:
   - **Total Sugerencias** — todas las sugerencias recibidas
   - **Sin Leer** — sugerencias que aún no han sido marcadas como leídas
3. La lista de sugerencias muestra cada una con: nombre del negocio, texto, fecha y badge de estado (leída/sin leer)
4. Clic en una sugerencia para marcarla como leída

### 8.2 — Exportar lista de negocios a Excel

1. Ir a la pestaña **"Negocios"** en el sidebar
2. Clic en el botón verde **"Exportar Excel"** (esquina superior izquierda del tab)
3. El navegador debe descargar automáticamente un archivo `.xlsx` con el nombre `Negocios_YYYYMMDD.xlsx` (donde YYYYMMDD es la fecha actual)
4. Abrir el archivo en Excel o Numbers para verificar que contiene los datos de todos los negocios

### 8.3 — Ver logs del sistema (auditoría)

1. Clic en **"Logs"** en el sidebar del admin
2. Se muestra una tabla con las últimas 200 acciones del admin con columnas:
   - **Fecha** — timestamp de la acción
   - **Acción** — tipo de acción (Crear, Editar, Eliminar, CambiarEstado, Impersonar, PagarComisiones, etc.)
   - **Detalle** — descripción textual de qué se hizo
   - **Negocio Afectado** — nombre del negocio involucrado (cuando aplica)
3. Verificar que todas las acciones realizadas durante la demo aparecen en el log

### 8.4 — Ver ventas/finanzas del SaaS

1. Clic en **"Ventas"** en el sidebar del admin
2. Se muestran 4 tarjetas de estadísticas financieras:
   - **MRR** (gradiente azul) — Monthly Recurring Revenue de todos los negocios activos
   - **Ingresos Totales** (gradiente verde) — ingresos históricos totales
   - **Ingreso Promedio** — precio promedio por negocio de pago (ARPU)
   - **Negocios Pagando** — número de negocios con suscripción activa
3. Dos gráficos (ApexCharts):
   - **"Ingresos por Mes"** (barras) — historial mensual
   - **"Distribución de Negocios"** (dona) — proporción entre activos y en prueba
4. Tabla **"Negocios con Suscripción"** con columnas: Negocio, Plan, Precio, Fecha Pago, Expira, Días Restantes

### 8.5 — Ver y descargar recibos

1. Clic en **"Recibos"** en el sidebar del admin
2. Se muestra la tabla de todos los recibos generados con columnas:
   - **#Recibo** — número secuencial del recibo
   - **Tipo** — "suscripcion_negocio" o "pago_comision"
   - **Destinatario** — email del negocio o vendedor
   - **Concepto** — descripción del recibo
   - **Monto** — valor del recibo
   - **Fecha** — fecha de emisión
   - **Acciones** — botón para ver el recibo en HTML
3. Clic en "Ver" abre un modal con el recibo en formato HTML listo para imprimir o reenviar

### 8.6 — Cambiar tema visual

El admin (y cualquier usuario) puede cambiar el tema visual haciendo clic en el ícono de **paleta** en el topbar. Se despliega un menú con 4 opciones:
- **Claro** — tema por defecto blanco/azul
- **Oscuro** — tema oscuro
- **Océano** — variante en tonos de celeste
- **Atardecer** — variante en tonos naranja

El tema se guarda en `localStorage` del navegador y persiste entre sesiones.

---

## SECCION 9: PUNTOS CLAVE PARA LA DEMO A DUEÑOS DE NEGOCIOS

Cuando se presente My-Negocio a potenciales clientes, destacar estos puntos desde el rol de admin:

1. **El admin es quien crea las cuentas** — el dueño del negocio no necesita registrarse solo; el admin (o el vendedor) llena el formulario y el sistema envía todo por email.

2. **Email de bienvenida automático** — al crear el negocio, el dueño recibe un email con sus credenciales de acceso y una guía personalizada según el tipo de negocio que eligió (membresías, artesanal, tienda o restaurante).

3. **El admin puede "entrar" a cualquier negocio** — el botón "Entrar" permite al equipo de soporte acceder al dashboard exacto del cliente para ayudarle sin interrumpirle, sin conocer su contraseña, y el cliente no lo sabe a menos que lo vea directamente.

4. **Los vendedores ganan comisión de $5** — por cada negocio que crean con precio >= $15 y duración >= 30 días. El admin ve las comisiones pendientes con datos bancarios del vendedor y las marca como pagadas con un clic. Se genera un recibo automáticamente.

5. **4 tipos de negocios en una sola plataforma** — el admin puede tener una cartera mixta: gimnasios, barberías, tiendas, restaurantes, todos gestionados desde el mismo panel.

6. **Temas visuales** — 4 temas de color permiten personalizar la experiencia visual. El tema del sistema admin es independiente del tema de cada negocio.

7. **Excel siempre disponible** — el admin puede exportar la lista completa de negocios en cualquier momento con un solo clic, en formato `.xlsx` compatible con Microsoft Excel y Google Sheets.

8. **Logs de auditoría** — el sistema registra cada acción del admin (creación, edición, eliminación, impersonación, pago de comisiones) para trazabilidad completa.

---

## CHECKLIST DE VERIFICACION

Marcar cada item como verificado durante el testing:

### Landing Page
- [ ] La landing page carga en `http://localhost:5170` sin errores
- [ ] El hero muestra el gradiente y las animaciones correctamente
- [ ] El botón "Probar gratis 7 dias" lleva a la sección `#contacto`
- [ ] El botón "Escribenos" abre WhatsApp con el número correcto (+593997143142)
- [ ] Las 4 tarjetas de modelos de negocio se muestran correctamente (Membresías, Citas, Tienda, Restaurante)
- [ ] El showcase interactivo cambia de panel al hacer clic en cada tab (Membresias / Citas Artesanal / Tienda POS / Restaurante)
- [ ] El formulario de contacto/lead se muestra en la sección `#contacto`

### Login y Acceso Admin
- [ ] El login en `/Auth/Login` carga correctamente
- [ ] Las credenciales de admin funcionan y redirigen a `/Negocios`
- [ ] El sidebar del admin muestra los 9 ítems: Resumen, Negocios, Crear Negocio, Ventas, Logs, Vendedores, Comisiones, Recibos, Sugerencias
- [ ] El saludo (Buen día / Buenas tardes / Buenas noches) es correcto según la hora

### Dashboard — Tab Resumen
- [ ] La tarjeta grande "Ingresos Recurrentes (MRR)" muestra un valor (o $0.00 si no hay negocios de pago)
- [ ] Las 4 tarjetas pequeñas (Total Negocios, Activos, En Prueba, Total Clientes) se cargan con datos
- [ ] Las tarjetas de segunda fila (Ingresos Mensuales, Por Vencer 7 días, Expirados) se cargan
- [ ] El gráfico de barras "Ingresos Mensuales" se renderiza sin errores

### Crear Negocio
- [ ] El botón "Nuevo Negocio" en la pestaña Negocios abre el formulario de creación
- [ ] El enlace "Crear Negocio" en el sidebar también abre el formulario
- [ ] Los 4 campos requeridos de la izquierda (Nombre, Dueño, Teléfono) funcionan
- [ ] Los 3 campos requeridos de la derecha (Email, Contraseña, con el ojo para mostrar/ocultar) funcionan
- [ ] Las 4 tarjetas de modelo de negocio se pueden seleccionar con clic y se resaltan correctamente
- [ ] Los radio buttons de "Negocio de Pago" y "Negocio de Prueba" funcionan
- [ ] Los campos de suscripción (fecha inicio, fecha expiración, precio, días) son editables
- [ ] Crear un negocio tipo **Membresías de pago** — aparece toast de éxito
- [ ] Crear un negocio tipo **Artesanal en prueba** — aparece toast de éxito
- [ ] Crear un negocio tipo **Tienda de pago** — aparece toast de éxito
- [ ] Crear un negocio tipo **Restaurante en prueba** — aparece toast de éxito
- [ ] Los 4 negocios aparecen en la tabla de la pestaña "Negocios"
- [ ] El botón "Cancelar" regresa a la pantalla anterior sin guardar

### Tabla de Negocios
- [ ] La tabla carga todos los negocios existentes vía AJAX
- [ ] Las columnas (Negocio, Dueño, Contacto, Clientes, Vendedor, Estado, Suscripción, Expira, Acciones) son visibles
- [ ] Los badges de estado muestran el color correcto (verde = Activo, amarillo = Prueba)
- [ ] La búsqueda de DataTables filtra los negocios correctamente
- [ ] El botón "Exportar Excel" descarga un archivo `.xlsx` con los datos
- [ ] El botón "Editar" abre el modal de edición pre-llenado con los datos actuales
- [ ] El modal de edición tiene los campos: Nombre, Dueño, Email, Teléfono, Contraseña, Tipo (Pago/Prueba), Suscripción
- [ ] Guardar cambios en el modal actualiza la tabla correctamente
- [ ] El botón "Cambiar Estado" alterna el estado y actualiza el badge en la tabla sin recargar
- [ ] El botón "Eliminar" muestra confirmación SweetAlert2 antes de borrar
- [ ] Eliminar un negocio sin clientes funciona correctamente
- [ ] El botón "Entrar" inicia la impersonación del negocio

### Gestión de Vendedores
- [ ] La pestaña "Vendedores" carga la tabla de vendedores
- [ ] El botón "Nuevo Vendedor" abre el modal correctamente
- [ ] El modal muestra los campos: Nombre, Apellido, Correo, Teléfono, Contraseña, y la sección de datos bancarios (Banco, Cédula, Cuenta)
- [ ] Crear un vendedor con todos los datos bancarios completos funciona
- [ ] El vendedor aparece en la tabla con sus datos
- [ ] El botón de editar pre-llena el modal con los datos del vendedor
- [ ] Editar el vendedor (cambiar datos bancarios, por ejemplo) guarda correctamente
- [ ] El botón de impersonar vendedor ("Ver como Vendedor") redirige al Panel Vendedor
- [ ] El banner de impersonación de vendedor muestra el nombre del vendedor
- [ ] El botón "Volver al Panel Admin" en el banner del panel vendedor restaura la sesión de admin
- [ ] Eliminar un vendedor muestra confirmación y elimina de la tabla

### Panel del Vendedor (al impersonarlo)
- [ ] El Panel Vendedor carga en `/Negocios/VendedorDashboard`
- [ ] El banner superior muestra "Estás viendo como vendedor: [Nombre]"
- [ ] El sidebar del vendedor muestra: Resumen, Mis Negocios, Crear Negocio
- [ ] La pestaña Resumen muestra: tarjetas de Negocios Creados, Activos, En Prueba, Total Clientes
- [ ] La sección "Interesados" (Leads) muestra la tabla correspondiente
- [ ] La pestaña "Mis Negocios" muestra solo los negocios creados por ese vendedor
- [ ] El formulario "Crear Negocio" del vendedor funciona con opciones de duración (30, 60, 90, 180, 365 días o Personalizado)
- [ ] El selector de duración muestra el precio sugerido y las fechas calculadas

### Comisiones
- [ ] La pestaña "Comisiones" carga las 4 tarjetas estadísticas (Pendiente Total, Pagado Total, Vendedores Activos, Negocios con Comisión)
- [ ] La sección "Comisiones Pendientes por Vendedor" muestra los grupos por vendedor con datos bancarios visibles
- [ ] El botón "Pagar $X.XX" muestra confirmación SweetAlert2
- [ ] Pagar comisiones marca las comisiones como pagadas
- [ ] El monto pagado aparece en "Historial de Pagos" después del pago
- [ ] Los negocios de pago con precio >= $15 y días >= 30 generan comisión automáticamente
- [ ] Los negocios con precio < $15 o días < 30 NO generan comisión

### Impersonación de Negocios
- [ ] El botón "Entrar" en la tabla de negocios inicia la impersonación
- [ ] El banner oscuro "Estás viendo como: [Nombre del Negocio]" aparece en la parte superior
- [ ] El sidebar muestra las opciones correctas según el tipo de negocio (membresías vs artesanal vs tienda vs restaurante)
- [ ] El botón "Volver al Admin" restaura la sesión de admin
- [ ] Después de volver, el topbar muestra "Administrador" y el sidebar vuelve a ser el del admin
- [ ] La acción de impersonación queda registrada en el Log de auditoría

### Logs del Sistema
- [ ] La pestaña "Logs" carga la tabla con las acciones del admin
- [ ] La columna "Acción" muestra los tipos correctos (Crear, Editar, Eliminar, Impersonar, PagarComisiones, etc.)
- [ ] Las acciones realizadas durante la demo (crear negocios, editar, impersonar) aparecen en el log
- [ ] La tabla soporta búsqueda y paginación

### Ventas y Finanzas
- [ ] La pestaña "Ventas" carga las 4 tarjetas (MRR, Ingresos Totales, Ingreso Promedio, Negocios Pagando)
- [ ] El gráfico de barras "Ingresos por Mes" se renderiza correctamente
- [ ] El gráfico de dona "Distribución de Negocios" se renderiza correctamente
- [ ] La tabla "Negocios con Suscripción" muestra los negocios de pago con sus fechas

### Recibos
- [ ] La pestaña "Recibos" carga la tabla de recibos
- [ ] Los recibos de suscripción aparecen cuando se creó un negocio de pago
- [ ] El botón "Ver" abre el modal con el recibo en HTML
- [ ] El iframe del recibo muestra el contenido correctamente

### Sugerencias
- [ ] La pestaña "Sugerencias" carga las tarjetas estadísticas (Total, Sin Leer)
- [ ] Si hay sugerencias, se muestran en la lista con nombre del negocio, texto y fecha
- [ ] Marcar como leída funciona y actualiza el contador "Sin Leer"

### Temas Visuales
- [ ] El ícono de paleta en el topbar muestra el dropdown de temas
- [ ] Seleccionar "Oscuro" cambia el tema a fondo oscuro
- [ ] Seleccionar "Océano" cambia a tonos azul celeste
- [ ] Seleccionar "Atardecer" cambia a tonos naranja
- [ ] Seleccionar "Claro" vuelve al tema original
- [ ] El tema persiste al recargar la página (guardado en localStorage)

### Cerrar sesión
- [ ] El botón "Cerrar Sesión" en el sidebar cierra la sesión correctamente
- [ ] Después de cerrar sesión, intentar acceder a `/Negocios` redirige al login

---

*Guía preparada para testing de la noche del 2026-02-20. My-Negocio — Panel Administrador.*
