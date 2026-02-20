# GUIA DE TESTING #2 — MEMBRESIAS

## Persona: Carlos Mendoza | Rol: Dueño de Gimnasio/Club con Membresías

### OBJETIVO

Probar TODAS las funciones del modelo de membresías. Este es el modelo para gimnasios, clubes,
academias — cualquier negocio donde los clientes pagan una membresía mensual con fecha de
vencimiento. Al terminar esta guía habrás probado cada botón, formulario y reporte del sistema.

---

### ANTES DE EMPEZAR

- El administrador de My-Negocio ya creó tu cuenta con tipo de negocio **membresias**
- Tienes un email y password asignados
- URL del sistema (confirmar con el administrador)
- Tener a mano un número de teléfono de prueba con WhatsApp activo (para probar mensajes)
- Opcional: tener un archivo Excel con columnas Nombre, Apellido, Teléfono para probar importación

---

## SECCION 1: LOGIN

### Pasos

1. Abrir el navegador e ir a la URL del sistema
2. Se muestra la página de login con el logo de **My-Negocio**
3. Ingresar el **email** (o número de teléfono) en el campo correspondiente
4. Ingresar el **password**
5. Hacer clic en el botón de ingresar / login

### Qué verificar

- Que el sistema redirige automáticamente al **Dashboard de Membresías** (NO al dashboard de artesanal ni de tienda)
- La URL debe cambiar a algo como `/Negocios/{ID}/Dashboard`
- En la barra superior (topbar) debe aparecer el **nombre de tu negocio** y tu nombre como dueño
- En el sidebar izquierdo deben aparecer exactamente estos 8 items de navegación:
  - **Resumen** (icono de velocímetro) — activo por defecto
  - **Clientes** (icono de personas)
  - **Movimientos** (icono de diario)
  - **Reportes** (icono de gráfica)
  - **Inventario** (icono de caja)
  - **Recibos** (icono de recibo)
  - **Notificaciones** (icono de campana)
  - **Sugerencias** (icono de chat)

### ERROR POSIBLE

- Si el sistema muestra "Acceso denegado" o 403, contactar al administrador para verificar que la cuenta es tipo `membresias`
- Si las credenciales son incorrectas, el sistema muestra un mensaje de error en rojo sin redirigir

---

## SECCION 2: EL DASHBOARD — QUE SE VE AL ENTRAR

Al ingresar, el sistema muestra automáticamente la pestaña **Resumen**. Esta es la pantalla principal.

### 2.1 Banner de Periodo de Prueba (solo si aplica)

Si el negocio está en modo de prueba, aparece un banner amarillo en la parte superior con:
- Icono de reloj de arena naranja
- Texto: **"Periodo de prueba"** seguido de los días restantes (ej: "- Te quedan 14 días")
- Botón verde **"Activar Plan"** con icono de WhatsApp — al hacer clic abre WhatsApp para contactar al equipo de My-Negocio

> Probar: si hay menos de 5 días de prueba, el sistema muestra un popup (SweetAlert) advirtiendo
> del vencimiento con opción de contactar por WhatsApp.

### 2.2 Encabezado y Selector de Periodo

En la parte superior del Resumen hay:
- Un saludo dinámico que cambia según la hora: **"Buenos días"**, **"Buenas tardes"** o **"Buenas noches"**
- Subtítulo: **"Resumen de tu negocio"**
- Tres botones pill a la derecha: **Hoy** | **Semana** | **Mes**

**PROBAR:** Hacer clic en cada botón pill y verificar que los montos de las tarjetas de Ingresos, Gastos y Ganancia Neta cambian según el periodo seleccionado.

### 2.3 Las Tres Tarjetas Financieras Principales

Tres tarjetas de colores muestran el resumen financiero del periodo seleccionado:

| Tarjeta | Color | Icono | ID del valor |
|---------|-------|-------|-------------|
| **Ingresos** | Verde degradado | Flecha arriba en círculo | `resumenIngresos` |
| **Gastos** | Rojo/Rosa degradado | Flecha abajo en círculo | `resumenGastos` |
| **Ganancia Neta** | Azul degradado | Pila de billetes | `resumenGanancia` |

Al inicio estarán en `$0.00` si no hay datos. Los valores se cargan automáticamente via AJAX.

### 2.4 Las Cuatro Tarjetas de Clientes

Debajo de las financieras, 4 tarjetas más pequeñas muestran el estado de la membresía:

- **Total Clientes** (icono azul de personas)
- **Activos** (icono verde de check en círculo) — membresías vigentes
- **Vencidos** (icono rojo de X en círculo) — membresías expiradas
- **Nuevos Hoy** (icono azul claro de persona con +)

### 2.5 Gráfico de Ingresos vs Gastos

Ocupa la parte izquierda inferior del Resumen. Es un gráfico de barras (verde = ingresos, rojo = gastos).
Si no hay datos aún, muestra el mensaje: **"Sin movimientos en este periodo"**.

Cuando cambias el periodo (Hoy/Semana/Mes) con los botones pill, el gráfico se actualiza automáticamente.

### 2.6 Panel de Próximos a Vencer

Panel en la parte derecha inferior. Muestra la lista de clientes cuya membresía vence pronto (próximos 5 días).
- Tiene un badge amarillo con el conteo (`resumenProximosCount`)
- Cada cliente muestra nombre, fecha de vencimiento y un badge de días restantes:
  - **Rojo** = vence en 0 o 1 día
  - **Amarillo** = vence en 2 o 3 días
  - **Azul** = vence en 4 o 5 días
- Si no hay nadie próximo a vencer: muestra icono verde de check y texto **"Sin membresías próximas a vencer"**

### 2.7 Los Tabs del Sidebar — Navegación

Hacer clic en cada item del sidebar izquierdo y verificar que la pantalla principal cambia:

| Tab del Sidebar | Contenido que aparece |
|----------------|----------------------|
| **Resumen** | Panel de estadísticas, gráfico, próximos a vencer |
| **Clientes** | Tabla de todos los clientes con botones de acciones |
| **Movimientos** | Formulario para registrar movimientos + tabla de logs |
| **Reportes** | Estadísticas detalladas y gráficos avanzados |
| **Inventario** | Grid de tarjetas de productos con flechas |
| **Recibos** | Tabla de recibos generados |
| **Notificaciones** | Lista de notificaciones con botones de acción |
| **Sugerencias** | Formulario para enviar feedback al equipo de My-Negocio |

---

## SECCION 3: GESTION DE CLIENTES

Hacer clic en **"Clientes"** en el sidebar. Esta es la sección más importante del sistema.

### 3.1 La Pantalla de Clientes — Qué se ve

Al entrar a Clientes se cargan automáticamente:

**Tres botones en la parte superior:**
- **"Nuevo Cliente"** (azul, con icono +) — abre el modal para crear
- **"Exportar"** (verde, con icono Excel) — descarga todos los clientes en .xlsx
- **"Importar Excel"** (azul contorno, con icono de subir) — muestra zona de importación

**Cuatro tarjetas de estadísticas:**
- **Total Clientes** — número total registrado
- **Activos** — con membresía vigente hoy
- **Vencidos** — con membresía expirada
- **Ingresos Mes** — suma de todos los pagos del mes actual (en $)

**Tabla de clientes** con columnas:
- **Cliente** — nombre completo + teléfono clicable (enlace `tel:`)
- **Precio** — monto de la membresía en $
- **Fecha Inicio** — cuando empezó la membresía
- **Vencimiento** — fecha en que expira
- **Días Rest.** — badge de color indicando días restantes:
  - **Verde** = más de 3 días (activo)
  - **Amarillo** = 3 días o menos (próximo a vencer)
  - **Rojo** = "Vencido" si ya pasó la fecha
  - **Azul** = "Por empezar" si la fecha de inicio es futura
- **Acciones** — botones de WhatsApp (verde), Renovar (amarillo), Editar (azul), Eliminar (rojo)

La tabla tiene búsqueda en español. Ordenamiento por defecto: los que vencen más pronto aparecen primero.

---

### 3.2 Crear un Cliente Normal

1. Hacer clic en el botón **"Nuevo Cliente"** (azul con icono +)
2. Se abre el modal **"Nuevo Cliente"** con el formulario
3. Llenar los campos:

| Campo | Tipo | Obligatorio | Ejemplo |
|-------|------|-------------|---------|
| **Nombre** | Texto | SI | `Carlos` |
| **Apellido** | Texto | SI | `Mendoza` |
| **Email** | Email | No | `carlos@gmail.com` |
| **Teléfono** | Teléfono | SI | `0991234567` |
| **Dirección** | Texto | No | `Av. Amazonas 123` |
| **Fecha Inicio** | Fecha | SI | (hoy, pre-cargado automáticamente) |
| **Fecha Fin** | Fecha | SI | (seleccionar fecha futura) |
| **Días** | Número | No | (se calcula automáticamente al llenar Inicio y Fin) |
| **Precio ($)** | Decimal | SI | `35.00` |
| **Es Diario** | Switch | No | (desactivado por defecto) |

> **IMPORTANTE:** El campo "Días" se calcula solo. Si llenas Fecha Inicio y Fecha Fin, el campo Días se actualiza automáticamente. Si llenas Días directamente, la Fecha Fin se calcula a partir del Inicio.

4. Hacer clic en **"Guardar"** (con icono de disquete)
5. El botón muestra **"Guardando..."** con spinner mientras se procesa
6. Si el email fue ingresado: el sistema envía automáticamente un recibo al email del cliente Y lo guarda en la sección Recibos
7. Aparece una notificación verde (toast) con mensaje de éxito en la esquina superior derecha
8. El modal se cierra y el cliente aparece inmediatamente en la tabla

**Verificar:**
- El cliente aparece en la tabla con su badge verde de días restantes
- Las tarjetas de Total Clientes y Activos aumentaron en 1
- Ingresos Mes aumentó en el precio ingresado

---

### 3.3 Editar un Cliente

1. En la tabla, hacer clic en el botón **azul con icono de lápiz** (Editar) del cliente
2. El sistema carga los datos del cliente via AJAX
3. Se abre el mismo modal pero ahora el título dice **"Editar Cliente"** y los campos están pre-llenados
4. Cambiar el nombre (por ejemplo, agregar un segundo nombre)
5. Hacer clic en **"Guardar"**
6. La tabla se actualiza con el nuevo nombre
7. En la pestaña Movimientos aparece un registro automático de tipo **"Edición"** (badge amarillo) indicando qué cambió

**Verificar:** El cambio se reflejó en la tabla. El log de auditoría se creó.

---

### 3.4 Renovar la Membresía de un Cliente

Este es uno de los flujos más importantes del sistema.

1. En la tabla, hacer clic en el botón **amarillo con icono de flechas circulares** (Renovar)
2. Se abre el modal **"Renovar Membresía"** con:
   - Nombre del cliente (solo lectura)
   - **"Fecha de fin actual"** (el campo muestra la fecha actual, deshabilitado)
   - **"Nueva fecha de fin"** (fecha mínima = día siguiente a la fecha actual)
   - **"Días a agregar"** (se calcula automáticamente: muestra cuántos días se están agregando)
   - **"Precio ($)"** (pre-cargado con el precio anterior, editable)
3. Por defecto la nueva fecha es 30 días después de la fecha actual (un mes)
4. Cambiar la nueva fecha de fin si se desea
5. Verificar que "Días a agregar" cambia en tiempo real
6. Ingresar el precio de la renovación
7. Hacer clic en **"Guardar"** (botón verde con icono de disquete)
8. Si el cliente tiene teléfono registrado, después de guardar aparece un popup de SweetAlert con dos opciones:
   - **"Enviar WhatsApp"** (verde) — abre WhatsApp con un mensaje pre-escrito: _"Hola [Nombre], te informamos que tu membresía en [Negocio] ha sido renovada. Ahora tu suscripción termina el [Fecha]. ¡Gracias!"_
   - **"Cerrar"** — cierra el popup sin enviar mensaje

**Verificar:**
- La fecha de vencimiento del cliente en la tabla cambió a la nueva fecha
- Los días restantes en el badge aumentaron
- En Movimientos aparece un log de tipo **"Renovación"** (badge azul claro) con el monto cobrado
- Si el cliente tiene email: se generó un nuevo recibo en la sección Recibos

---

### 3.5 Crear un Pase Diario

Un pase diario es para visitantes que pagan solo por un día, sin membresía mensual.

1. Hacer clic en **"Nuevo Cliente"**
2. Llenar Nombre, Apellido, Teléfono
3. Activar el switch **"Es Diario"** (toggle verde)
4. Las fechas pueden ser del mismo día (inicio y fin = hoy)
5. Precio = lo que cobra por el día
6. Hacer clic en **"Guardar"**

**Verificar:**
- El cliente aparece en la tabla
- En la pestaña **Reportes** (antes llamada Ventas), en la tarjeta **"Clientes Diarios Hoy"** el número aumentó
- En la tabla de "Usuarios Diarios" dentro de Reportes aparece este cliente

---

### 3.6 Buscar un Cliente

La tabla de clientes tiene una barra de búsqueda en la parte superior derecha con texto **"Buscar:"**.

1. Escribir parte del nombre de un cliente
2. La tabla se filtra en tiempo real mostrando solo los que coinciden
3. Limpiar el texto para volver a ver todos

**Verificar:** La búsqueda funciona con nombre, apellido y teléfono.

---

### 3.7 Enviar WhatsApp a un Cliente Individual

En la columna "Acciones" de la tabla, si el cliente tiene un teléfono válido (10+ dígitos), aparece un botón **verde con icono de WhatsApp**.

1. Hacer clic en el botón verde de WhatsApp de cualquier cliente
2. Se abre una nueva pestaña del navegador con WhatsApp Web o la app móvil
3. El mensaje pre-escrito dice: _"Hola [Nombre Completo], te saludamos de [Nombre del Negocio], ¿cómo estás hoy?"_
4. Solo queda hacer clic en "Enviar" dentro de WhatsApp

> Si el cliente no tiene teléfono o tiene menos de 10 dígitos, el botón de WhatsApp NO aparece.

---

### 3.8 Eliminar un Cliente

1. Hacer clic en el botón **rojo con icono de basurero** (Eliminar) del cliente
2. Aparece un popup de confirmación (SweetAlert) con:
   - Título: **"¿Estás seguro?"**
   - Texto: _"Se eliminará al cliente "[Nombre]". Esta acción no se puede deshacer."_
   - Botón rojo: **"Sí, eliminar"**
   - Botón azul: **"Cancelar"**
3. Hacer clic en **"Sí, eliminar"**
4. El cliente desaparece de la tabla
5. Las estadísticas se actualizan

**Verificar:** El cliente ya no aparece en la tabla. Un log de tipo **"Eliminación"** (badge rojo) se creó en Movimientos.

---

## SECCION 4: INVENTARIO

Hacer clic en **"Inventario"** en el sidebar. Esta sección permite manejar productos que el negocio vende (bebidas, suplementos, accesorios, etc.).

### 4.1 La Pantalla de Inventario — Qué se ve

**Tres botones en la parte superior:**
- **"Nuevo Producto"** (azul, icono +)
- **"Exportar"** (verde, icono Excel)
- **"Historial"** (azul contorno, icono reloj) — muestra/oculta el historial de movimientos

**Cuatro tarjetas de estadísticas:**
- **Total Productos** — cantidad de productos registrados
- **Stock Bajo** (número amarillo) — productos que bajaron del stock mínimo configurado
- **Ventas Hoy** — unidades vendidas en el día
- **Ingresos Hoy** (en $) — dinero generado por ventas de inventario hoy

**Grid de tarjetas de productos:**
Cada producto aparece como una tarjeta compacta con:
- Nombre del producto (centrado, en negrita)
- Precio de venta (en $)
- Flecha arriba (naranja/amarillo) para devolución o actualización de inventario
- Número de stock actual (en verde si hay stock normal, en rojo si está bajo)
- Flecha abajo (azul) para vender 1 unidad — deshabilitada si stock = 0
- Botones de editar (lápiz) y eliminar (basurero) en la esquina superior derecha de la tarjeta
- Badge rojo **"Stock bajo"** si el stock cayó por debajo del mínimo configurado

---

### 4.2 Crear un Producto

1. Hacer clic en **"Nuevo Producto"**
2. Se abre el modal **"Nuevo Producto"** con el formulario:

| Campo | Tipo | Obligatorio | Ejemplo |
|-------|------|-------------|---------|
| **Nombre del Producto** | Texto | SI | `Botella de Agua 500ml` |
| **Precio de Venta ($)** | Decimal (min 0.01) | SI | `1.50` |
| **Costo de Compra ($)** | Decimal (min 0.01) | SI | `0.60` |
| **Stock Inicial** | Entero (min 0) | SI | `50` |
| **Stock Mínimo (alerta)** | Entero (min 0) | No | `5` (pre-cargado por defecto) |

> Debajo del campo "Costo de Compra" aparece un texto dinámico mostrando el margen de ganancia calculado en tiempo real.

> El **Stock Mínimo** define cuándo el sistema muestra la alerta "Stock bajo". Si el stock cae por debajo de este número, la tarjeta del producto se marca en rojo.

3. Hacer clic en **"Guardar"**
4. El producto aparece como una nueva tarjeta en el grid
5. La tarjeta "Total Productos" aumentó en 1

---

### 4.3 Vender un Producto (Flecha Abajo)

La forma más rápida de registrar una venta es la **flecha abajo azul** en la tarjeta del producto.

1. Localizar el producto en el grid
2. Hacer clic en la **flecha abajo azul** en la tarjeta
3. El número de stock en la tarjeta baja inmediatamente en 1 (animación visual instantánea)
4. Aparece un toast verde con mensaje de éxito
5. Las estadísticas de "Ventas Hoy" e "Ingresos Hoy" se actualizan
6. Si el Historial está visible, aparece un nuevo movimiento de tipo **"Venta"** (badge verde)
7. En Movimientos también se registra automáticamente un log de ingreso

**Probar error:** Intentar hacer clic en la flecha abajo cuando el stock es 0 — el botón debe estar deshabilitado (aparece en gris) y si se intenta aparece el mensaje **"No hay stock disponible"**.

---

### 4.4 Hacer una Devolución o Actualizar Inventario (Flecha Arriba)

La **flecha arriba naranja/amarilla** abre un menú con dos opciones.

1. Hacer clic en la **flecha arriba** de cualquier producto
2. Aparece un popup (SweetAlert) con:
   - Título: nombre del producto
   - Texto: stock actual y precio
   - Botón amarillo: **"Devolver Producto"**
   - Botón azul: **"Actualizar Inventario"**
   - Botón gris: **"Cancelar"**

#### Opción A: Devolver Producto

3. Hacer clic en **"Devolver Producto"** (amarillo)
4. Se abre un segundo popup con:
   - Campo **"Cantidad a devolver"** (número, default 1)
   - Campo **"Razón de la devolución"** (texto, ej: "Cliente cambió de opinión")
   - Mensaje en amarillo: _"Se reembolsará el dinero al cliente"_
5. Ingresar la cantidad y la razón
6. Hacer clic en **"Confirmar Devolución"**
7. El stock sube por la cantidad devuelta
8. Se registra un movimiento de tipo **"Devolución"** (badge amarillo)
9. Se registra un gasto en Movimientos (el dinero reembolsado)

#### Opción B: Actualizar Inventario (Restock o Ajuste Manual)

3. Hacer clic en **"Actualizar Inventario"** (azul)
4. Se abre un segundo popup con dos opciones más:
   - **"Compré más (Restock)"** — para cuando compraste mercadería nueva
   - **"Ajuste Manual"** — para cuando hiciste un conteo físico y el sistema no coincide

##### Restock (compré más)

5. Hacer clic en **"Compré más (Restock)"**
6. Popup con:
   - Campo **"Unidades compradas"** (número)
   - Campo **"Costo total de compra ($)"** (decimal) — cuánto gastaste en total
7. Ingresar valores y confirmar
8. El stock sube por las unidades ingresadas
9. Se registra un movimiento de tipo **"Restock"** (badge azul claro)
10. Se registra un gasto en Movimientos (el costo de la compra)

##### Ajuste Manual

5. Hacer clic en **"Ajuste Manual"**
6. Popup con:
   - Campo **"Stock real (conteo físico)"** — ingresar el número real que tienes
   - Campo **"Nota (opcional)"** — ej: "Conteo físico mensual"
7. Confirmar
8. El stock se actualiza al valor ingresado (sin importar si es mayor o menor al anterior)
9. Se registra un movimiento de tipo **"Ajuste"** (badge rojo)
10. Este movimiento es financieramente neutro (no afecta ingresos ni gastos)

---

### 4.5 Editar un Producto

1. Hacer clic en el botón **lápiz** (esquina superior derecha de la tarjeta del producto)
2. Se abre el modal con los datos actuales pre-cargados
3. Cambiar el nombre o precio
4. Hacer clic en **"Guardar"**

> Nota: Al editar un producto, el stock NO cambia. Para cambiar el stock usa la flecha arriba (Ajuste Manual).

---

### 4.6 Ver el Historial de Movimientos

1. Hacer clic en el botón **"Historial"** (azul contorno, icono reloj)
2. Aparece (o se oculta) una tabla con el historial completo de movimientos de inventario
3. Columnas: **Fecha**, **Producto**, **Tipo** (badge de color), **Cant.** (cantidad), **Total** ($), **Stock** (stock resultante), **Nota**

Los tipos de movimiento y sus colores:
- **Venta** — verde
- **Devolución** — amarillo
- **Restock** — azul claro
- **Ajuste** — rojo

4. Hacer clic en **"Historial"** nuevamente para ocultar la tabla

---

### 4.7 Eliminar un Producto

1. Hacer clic en el botón **basurero** (esquina superior derecha de la tarjeta)
2. Aparece un popup de confirmación
3. Confirmar la eliminación
4. La tarjeta del producto desaparece del grid (eliminación lógica: el historial se preserva)

---

### 4.8 Exportar Inventario a Excel

1. Hacer clic en el botón **"Exportar"** (verde, icono Excel)
2. Se descarga automáticamente un archivo `.xlsx` con todos los productos activos
3. El nombre del archivo incluye la fecha actual (ej: `Inventario_20260220.xlsx`)

---

## SECCION 5: MOVIMIENTOS (LOGS)

Hacer clic en **"Movimientos"** en el sidebar. Esta sección registra TODO lo que entra y sale de dinero, con auditoría completa.

### 5.1 La Pantalla de Movimientos — Qué se ve

**Formulario de registro manual** en la parte superior:
- Campo **"Descripción"** (texto) — ej: _"Vendido un Gatorade"_
- Campo **"Monto ($)"** (decimal) — positivo para ingresos, negativo para gastos
- Botón **"Registrar"** (azul, icono check)
- Texto en gris: _"Usa valores positivos para ingresos y negativos para gastos"_

**Nota de inmutabilidad** (abajo del formulario):
- Icono de candado con texto: _"Los registros son inmutables para proteger la integridad de tu negocio"_

**Botones de acción:**
- Botón **"Cerrar Periodo"** (rojo contorno, icono calendario) — solo aparece si hay registros con más de 30 días
- Botón **"Exportar"** (verde, icono Excel)

**Tabla de movimientos** con columnas:
- **Descripción** — texto del movimiento
- **Tipo** — badge de color (ver lista abajo)
- **Cliente** — nombre del cliente asociado (si aplica)
- **Monto** — en verde con + para ingresos, en rojo sin + para gastos
- **Fecha** — fecha y hora del movimiento

Los tipos de movimiento y sus badges:
- **Ingreso** — verde oscuro
- **Gasto** — rojo
- **Cliente Nuevo** — azul
- **Edición** — amarillo
- **Renovación** — azul claro
- **Eliminación** — rojo
- **Inicio Sesión** — gris
- **Venta Inv.** — verde
- **Dev. Inv.** — amarillo
- **Restock** — azul claro
- **Ajuste Inv.** — gris

La tabla se ordena por defecto de más reciente a más antiguo.

---

### 5.2 Registrar un Ingreso Manual

1. En el campo **"Descripción"** escribir: `Venta de proteína`
2. En el campo **"Monto ($)"** escribir: `25`  (positivo = ingreso)
3. Hacer clic en **"Registrar"**
4. El botón muestra **"Registrando..."** con spinner
5. Aparece toast verde de confirmación
6. Los campos se limpian automáticamente
7. El nuevo registro aparece en la tabla con badge verde **"Ingreso"** y monto en verde `+$25.00`

---

### 5.3 Registrar un Gasto Manual

1. En el campo **"Descripción"** escribir: `Pago de servicios básicos`
2. En el campo **"Monto ($)"** escribir: `-80`  (negativo = gasto)
3. Hacer clic en **"Registrar"**
4. El nuevo registro aparece con badge rojo **"Gasto"** y monto en rojo `-$80.00`

---

### 5.4 Verificar la Inmutabilidad

Los registros NO tienen botones de editar ni eliminar individuales. Esto es por diseño: los movimientos son inmutables para proteger la integridad financiera del negocio. El único modo de limpiar todos los registros es con **"Cerrar Periodo"**.

---

### 5.5 Exportar Movimientos a Excel

1. Hacer clic en el botón **"Exportar"** (verde, icono Excel)
2. Se descarga un archivo `.xlsx` con todos los movimientos
3. Útil para enviarlo al contador

---

### 5.6 Cerrar Periodo (si aplica)

El botón **"Cerrar Periodo"** solo aparece si el registro más antiguo tiene más de 30 días.

1. Hacer clic en **"Cerrar Periodo"** (solo aparece si aplica)
2. Aparece un popup de advertencia con texto: _"Se descargará el Excel de respaldo y luego se eliminarán todos los registros. Esta acción no se puede deshacer."_
3. Botones: **"Descargar y Cerrar"** (rojo) | **"Cancelar"** (gris)
4. Si se confirma: primero descarga el Excel de respaldo automáticamente, luego elimina todos los registros
5. La tabla queda vacía

---

## SECCION 6: REPORTES

Hacer clic en **"Reportes"** en el sidebar. Esta sección muestra estadísticas financieras detalladas con gráficos.

### 6.1 Encabezado y Selector de Periodo

- Tres botones pill: **Día** | **Semana** | **Mes** (mismo concepto que en el Resumen)
- Botón **"Refrescar"** (azul contorno, icono de flechas circulares)
- Al cambiar el periodo, todas las tarjetas y gráficos se actualizan

### 6.2 Las Tres Tarjetas Financieras de Reportes

| Tarjeta | Color | Descripción dinámica |
|---------|-------|---------------------|
| **Ingresos** | Azul degradado | "Ingresos del Día" / "de la Semana" / "del Mes" |
| **Gastos** | Rojo degradado | "Gastos del Día" / "de la Semana" / "del Mes" |
| **Ganancia Neta** | Verde degradado | "Ganancia Neta del Día" / "de la Semana" / "del Mes" |

### 6.3 Las Tres Tarjetas de Detalles

Debajo de las financieras principales:
- **Membresías 30 días (mes)** — suma de membresías cobradas en el mes (icono azul de calendario)
- **Clientes Diarios Hoy** — número de clientes con pase diario hoy (icono azul claro de persona caminando)
- **Ganancia del Mes** — ganancia neta del mes completo (icono verde de gráfica)

### 6.4 Los Cuatro Gráficos

**Gráfico 1 — Ingresos vs Gastos** (barras, verde y rojo): muestra la comparación por día/semana/mes según el periodo seleccionado.

**Gráfico 2 — Nuevos Clientes** (área, azul): evolución de altas de clientes en el periodo.

**Gráfico 3 — Resumen del Día/Semana/Mes** (donut): proporción entre ingresos y gastos. En el centro del donut muestra la **Ganancia Neta** en verde (si positiva) o rojo (si negativa). Si no hay datos muestra el mensaje "Sin datos para este periodo".

**Gráfico 4 — Usuarios Diarios** (tabla): lista de clientes con pase diario de hoy con nombre, teléfono y fecha. Si hay clientes diarios, aparece el botón **"Mensaje"** (verde, icono WhatsApp) para enviarles mensajes en bloque.

**Probar:** Cambiar entre Día, Semana y Mes y verificar que todos los valores y gráficos se actualizan.

### 6.5 WhatsApp Masivo a Clientes Diarios

Si hay clientes con pase diario, en el gráfico 4 aparece el botón verde **"Mensaje"** con icono de WhatsApp.

1. Hacer clic en **"Mensaje"**
2. Se abre el modal **"Enviar mensajes a clientes diarios"** (fondo verde)
3. El modal muestra **un cliente a la vez** con:
   - Contador: **"1 / 5"** (o cuántos sean)
   - Nombre y teléfono del cliente en tarjeta
   - Preview del mensaje: _"✅ Hola [Nombre], gracias por tu visita de hoy a [Negocio]. ¡Te esperamos pronto!"_
   - Botón verde grande: **"Abrir WhatsApp y enviar"**
4. Hacer clic en el botón verde — se abre WhatsApp en nueva pestaña con el mensaje pre-escrito
5. En el modal, hacer clic en **"Siguiente"** para pasar al siguiente cliente
6. Al terminar el último, el botón cambia a **"Finalizar y limpiar lista"** (verde)
7. Al finalizar: el modal muestra pantalla de éxito con cuántos clientes fueron procesados
8. Hacer clic en **"Finalizar y limpiar lista"**: se llama al sistema para marcar todos los clientes diarios como procesados (EsDiario = false) y la lista queda vacía para el día siguiente

---

## SECCION 7: NOTIFICACIONES

Hacer clic en **"Notificaciones"** en el sidebar.

### 7.1 Cómo Funciona el Sistema de Notificaciones

El sistema genera notificaciones automáticamente para clientes cuya membresía **vence en los próximos 3 días**. Las notificaciones también se generan al entrar al dashboard (en segundo plano).

La campana en la barra superior muestra un badge rojo con el número de notificaciones no leídas.

### 7.2 Preparar un Cliente que Venza Pronto

Para probar esta sección:

1. Ir a **Clientes** y crear un nuevo cliente con:
   - Nombre: `Cliente de Prueba Notif`
   - Fecha Inicio: hoy
   - Fecha Fin: hoy + 2 días (pasado mañana)
   - Precio: `30`
2. Guardar el cliente

### 7.3 Generar Notificaciones

1. Volver a **Notificaciones**
2. Hacer clic en el botón **"Generar Notificaciones"** (azul, icono de flechas circulares)
3. El botón muestra **"Generando..."** con spinner
4. Aparece toast verde con mensaje de éxito indicando cuántas notificaciones se generaron
5. En la lista aparece la notificación del cliente que vence en 2 días

**Formato del mensaje de notificación:** _"La membresía de [Nombre] vence en X días (vence el [Fecha])"_

Los íconos de las notificaciones:
- **Amarillo** (relleno) = no leída
- **Gris** = leída

### 7.4 Marcar una Notificación como Leída

1. En la lista, las notificaciones no leídas tienen un borde azul a la izquierda y están en negrita
2. Hacer clic en el botón pequeño azul **"✓"** (check) a la derecha de la notificación
3. La notificación cambia a gris y el borde azul desaparece
4. El badge rojo en la campana disminuye en 1

### 7.5 Marcar Todas como Leídas

1. Hacer clic en el botón **"Marcar todas leídas"** (azul contorno, icono de doble check)
2. Todas las notificaciones se marcan como leídas
3. El badge rojo desaparece de la campana

### 7.6 El Dropdown de Notificaciones (Campana)

1. Hacer clic en el ícono de campana en la barra superior
2. Se abre un dropdown con las últimas 10 notificaciones
3. Tiene un botón **"Marcar leídas"** en la cabecera del dropdown
4. Cada notificación muestra el mensaje y la fecha/hora

---

## SECCION 8: RECIBOS

Hacer clic en **"Recibos"** en el sidebar.

### 8.1 Cómo se Generan los Recibos

Los recibos se generan automáticamente en dos situaciones:
1. Cuando se **crea un cliente** que tiene email registrado
2. Cuando se **renueva la membresía** de un cliente que tiene email registrado

Cada recibo:
- Tiene un número secuencial (ej: `000001`, `000002`)
- Se guarda como HTML en la base de datos
- Se envía al email del cliente (si tiene email)
- Queda disponible para ver en esta sección

### 8.2 La Pantalla de Recibos — Qué se ve

**Banner de limpieza** (solo si aplica): si hay recibos de más de 30 días, aparece un banner amarillo con el botón **"Exportar y Limpiar"**.

**Encabezado:**
- Título: **"Recibos"**
- Barra de búsqueda por número: campo de texto con placeholder `# Recibo...` y ícono de lupa
- Botón **"Exportar"** (verde, icono Excel)

**Tabla de recibos** con columnas:
- **#Recibo** — número secuencial (ej: `000001`)
- **Destinatario** — email y nombre del cliente
- **Concepto** — descripción (ej: `Membresía x30 días`, `Renovación membresía x30 días`)
- **Monto** — en $
- **Fecha** — fecha y hora de generación
- **Acciones** — botón para ver el recibo

### 8.3 Ver un Recibo

1. En la tabla, hacer clic en el botón de acciones (ver) del recibo
2. Se abre el modal **"Recibo #000001"** (con el número correspondiente)
3. Dentro del modal hay un iframe que muestra el HTML del recibo tal como se envió al cliente
4. El recibo muestra: nombre del negocio, datos del cliente, concepto, monto, fecha, número de recibo
5. Hacer clic en **"Cerrar"** para cerrar el modal

### 8.4 Buscar un Recibo por Número

1. En la barra de búsqueda escribir el número del recibo (ej: `1` o `42`)
2. La tabla se filtra mostrando solo ese recibo

### 8.5 Exportar Recibos a Excel

1. Hacer clic en **"Exportar"** (verde, icono Excel)
2. Se descarga un archivo `.xlsx` con los recibos del mes y año actuales
3. Nombre del archivo: `Recibos_2026_02.xlsx`

---

## SECCION 9: IMPORTAR/EXPORTAR EXCEL (Clientes)

### 9.1 Exportar Lista de Clientes

1. Ir a **Clientes** en el sidebar
2. Hacer clic en el botón **"Exportar"** (verde, icono Excel)
3. El botón muestra **"Exportando..."** con spinner durante 3 segundos
4. Se descarga automáticamente el archivo `Clientes_20260220.xlsx` (con la fecha actual)
5. El Excel contiene todos los clientes con sus datos: nombre, apellido, email, teléfono, fechas, precio, estado

### 9.2 Importar Clientes desde Excel

Para importar, el archivo Excel debe tener al menos las columnas **Nombre** y **Apellido** (las demás son opcionales y se detectan automáticamente por el nombre de la columna).

1. Hacer clic en el botón **"Importar Excel"** (azul contorno, icono de subir)
2. Aparece la zona de importación: un recuadro con borde punteado y texto:
   - **"Arrastra tu archivo Excel aquí"**
   - _"O haz clic para seleccionar (solo Nombre y Apellido son obligatorios)"_
3. Dos formas de subir el archivo:
   - **Arrastrar y soltar** el archivo Excel sobre el recuadro
   - **Hacer clic** en el recuadro y seleccionar el archivo desde el explorador de archivos
4. El sistema acepta archivos `.xlsx` y `.xls` de hasta **10 MB**
5. Durante el proceso se muestra **"Importando clientes..."** con spinner
6. Al terminar aparece el modal **"Resultado de Importación"** con:
   - Badge verde: **"X clientes creados"**
   - Badge amarillo (si aplica): **"X omitidos (duplicados)"**
   - Badge rojo (si aplica): **"X errores"**
7. La tabla de clientes se actualiza automáticamente
8. Hacer clic en **"Cerrar"** para cerrar el modal de resultado

---

## SECCION 10: SUGERENCIAS

Hacer clic en **"Sugerencias"** en el sidebar.

### 10.1 Enviar una Sugerencia

Esta sección permite enviar comentarios, ideas o sugerencias directamente al equipo de desarrollo de My-Negocio.

1. En el área de texto grande (5 filas) escribir la sugerencia
   - Ejemplo: _"Sería útil poder enviar recordatorios automáticos por WhatsApp a todos los clientes que vencen esta semana"_
   - Máximo **1000 caracteres** (el contador de la esquina inferior derecha va subiendo en tiempo real)
2. Hacer clic en el botón **"Enviar Sugerencia"** (azul, icono de avión de papel)
3. El botón muestra **"Enviando..."** con spinner
4. Aparece un popup de confirmación (SweetAlert) con título **"Enviada"** y el mensaje de éxito
5. El área de texto se limpia y el contador vuelve a 0

---

## SECCION 11: TEMAS (PERSONALIZACIÓN VISUAL)

My-Negocio tiene 4 temas visuales que puedes cambiar en cualquier momento.

### 11.1 Cómo Cambiar el Tema

1. Hacer clic en el ícono de paleta de colores en la barra superior (topbar), a la derecha de la campana
2. Se abre un pequeño dropdown con las 4 opciones

### 11.2 Los 4 Temas Disponibles

| Tema | Muestra de color | Descripción |
|------|-----------------|-------------|
| **Claro** | Mitad blanco / mitad azul | Tema por defecto, fondo blanco y crema |
| **Oscuro** | Mitad azul oscuro / mitad azul claro | Fondo oscuro, texto claro |
| **Océano** | Mitad celeste claro / mitad azul cielo | Tonos azules suaves |
| **Atardecer** | Mitad naranja claro / mitad naranja | Tonos cálidos y anaranjados |

3. Hacer clic en cada opción y verificar que toda la interfaz cambia de color inmediatamente
4. El tema seleccionado muestra un ícono de check (✓) a la derecha
5. El tema se guarda automáticamente en el navegador (localStorage) y se mantiene aunque cierres y vuelvas a abrir el navegador

**Probar:** Seleccionar **"Oscuro"**, cerrar el navegador, volver a abrir y entrar al sistema — el tema oscuro debe estar activo sin tener que seleccionarlo nuevamente.

---

## PUNTOS CLAVE PARA DEMO A DUEÑOS DE GIMNASIO

Al presentar el sistema a un posible cliente dueño de gimnasio, enfatizar estos puntos:

1. **Control de membresías con fechas** — cada cliente tiene fecha de inicio y fin de su membresía. El sistema sabe en todo momento quiénes están activos y quiénes vencidos.

2. **Alerta automática 3 días antes del vencimiento** — el sistema notifica automáticamente cuando una membresía está por vencer, sin que el dueño tenga que revisar manualmente.

3. **Renovación con un clic** — desde la tabla de clientes, renovar una membresía toma 3 segundos: click en el icono amarillo, nueva fecha, precio, guardar.

4. **WhatsApp integrado** — cada cliente con teléfono registrado tiene un botón directo de WhatsApp que abre un mensaje pre-escrito personalizado. No necesitas copiar números ni escribir mensajes.

5. **Reportes financieros claros** — en cualquier momento puedes ver cuánto ingresó, cuánto gastaste y cuánta ganancia neta tuviste hoy, esta semana o este mes. Los gráficos lo muestran visualmente.

6. **Excel para el contador** — exportar todos los clientes, movimientos o recibos a Excel es un clic. El contador recibe el archivo listo para procesar.

7. **Recibos digitales automáticos** — si el cliente tiene email, el sistema envía automáticamente un recibo de pago profesional cada vez que se registra o renueva.

8. **Inventario integrado** — si el gimnasio vende bebidas, suplementos u otros productos, el inventario está integrado en el mismo sistema. Cada venta se registra automáticamente como ingreso.

---

## CHECKLIST FINAL — VERIFICACION COMPLETA

Marcar cada item al completarlo exitosamente:

### Login y Acceso
- [ ] Login con email y password funciona
- [ ] Redirige correctamente al Dashboard de Membresías (no a otro tipo)
- [ ] El nombre del negocio aparece en la barra superior
- [ ] El sidebar muestra exactamente 8 items de navegación

### Resumen (Dashboard Principal)
- [ ] Las tarjetas de Ingresos/Gastos/Ganancia Neta se cargan
- [ ] Los botones pill **Hoy/Semana/Mes** cambian los montos
- [ ] Las 4 tarjetas de clientes (Total/Activos/Vencidos/Nuevos Hoy) muestran datos
- [ ] El gráfico de Ingresos vs Gastos se renderiza
- [ ] La lista de Próximos a Vencer muestra clientes o el mensaje de "sin membresías"

### Clientes
- [ ] La tabla de clientes carga correctamente con todas sus columnas
- [ ] Crear cliente normal: modal abre, todos los campos funcionan, días se calcula automáticamente, guarda correctamente
- [ ] El cliente nuevo aparece en la tabla con badge verde de días restantes
- [ ] Editar cliente: modal carga datos pre-llenados, cambio se guarda, log se crea
- [ ] Renovar membresía: modal abre con fecha actual, nueva fecha calcula días, popup de WhatsApp aparece después de renovar
- [ ] La fecha de vencimiento del cliente cambió en la tabla después de renovar
- [ ] Crear cliente con "Es Diario" activado funciona
- [ ] El cliente diario aparece en Reportes en la tabla de "Usuarios Diarios"
- [ ] La búsqueda en la tabla filtra por nombre/apellido/teléfono
- [ ] El botón de WhatsApp (verde) abre WhatsApp con mensaje pre-escrito
- [ ] Eliminar cliente: popup de confirmación aparece, cliente desaparece de la tabla
- [ ] Los logs de auditoría se crean para cada acción (crear, editar, renovar, eliminar)

### Inventario
- [ ] Grid de productos carga correctamente (o muestra mensaje de vacío)
- [ ] Crear producto: modal con todos los campos, margen calculado en tiempo real, se guarda
- [ ] Tarjeta del producto aparece en el grid con nombre, precio y stock
- [ ] Flecha abajo (vender 1 unidad): el stock baja visualmente e inmediatamente
- [ ] Flecha abajo deshabilitada cuando stock = 0
- [ ] Flecha arriba > Devolver Producto: solicita cantidad y razón, stock sube, log creado
- [ ] Flecha arriba > Actualizar Inventario > Restock: stock sube, gasto registrado
- [ ] Flecha arriba > Actualizar Inventario > Ajuste Manual: stock cambia al valor ingresado
- [ ] Editar producto: modal abre con datos actuales, cambio se guarda
- [ ] Botón "Historial" muestra/oculta la tabla de movimientos
- [ ] La tabla de historial muestra los movimientos con tipos y colores correctos
- [ ] Eliminar producto: confirmación aparece, tarjeta desaparece
- [ ] Exportar inventario: se descarga el archivo .xlsx

### Movimientos (Logs)
- [ ] Registrar ingreso manual: descripción + monto positivo, aparece en tabla con badge verde
- [ ] Registrar gasto manual: descripción + monto negativo, aparece en tabla con badge rojo
- [ ] Los logs de acciones de clientes aparecen automáticamente (crear, editar, renovar, eliminar)
- [ ] Los logs de inventario aparecen (venta, devolución, restock, ajuste)
- [ ] No hay botones de editar ni eliminar en los registros individuales (son inmutables)
- [ ] Exportar logs: se descarga el archivo .xlsx

### Reportes (Ventas)
- [ ] Los botones pill Día/Semana/Mes cambian todos los valores
- [ ] Las 3 tarjetas financieras muestran los montos correctos del periodo
- [ ] Las 3 tarjetas de detalles (Membresías 30 días, Clientes Diarios, Ganancia Mes) muestran datos
- [ ] El gráfico de barras Ingresos vs Gastos se renderiza
- [ ] El gráfico de área Nuevos Clientes se renderiza
- [ ] El gráfico donut Resumen muestra la ganancia neta en el centro
- [ ] La tabla de Usuarios Diarios muestra los clientes diarios del día
- [ ] El botón "Refrescar" actualiza todos los datos

### Notificaciones
- [ ] El botón "Generar Notificaciones" crea notificaciones para clientes próximos a vencer
- [ ] Las notificaciones no leídas tienen borde azul y están en negrita
- [ ] El badge rojo en la campana muestra el conteo correcto
- [ ] Marcar una notificación como leída: el borde y negrita desaparecen, el badge baja
- [ ] "Marcar todas leídas" limpia todas las notificaciones y el badge desaparece
- [ ] El dropdown de la campana muestra las últimas 10 notificaciones

### Recibos
- [ ] La tabla de recibos carga (o muestra vacío si no se ha creado ningún cliente con email)
- [ ] Al crear un cliente con email se genera un recibo automático
- [ ] Al renovar un cliente con email se genera un recibo automático
- [ ] El recibo se puede ver en el modal con el iframe del HTML
- [ ] La búsqueda por número filtra correctamente
- [ ] Exportar recibos: se descarga el archivo .xlsx

### Excel (Clientes)
- [ ] Exportar lista de clientes: se descarga el archivo .xlsx con todos los clientes
- [ ] El botón "Importar Excel" muestra/oculta la zona de importación
- [ ] La zona de importación acepta arrastrar y soltar un archivo
- [ ] La zona de importación abre el selector de archivos al hacer clic
- [ ] La importación muestra el resultado (creados / omitidos / errores)
- [ ] Solo acepta archivos .xlsx y .xls (rechaza otros formatos)

### Sugerencias
- [ ] El área de texto acepta texto y el contador sube en tiempo real
- [ ] No se puede enviar sugerencia vacía (muestra error)
- [ ] Enviar sugerencia: popup de confirmación aparece, área se limpia

### Temas
- [ ] Tema **Claro**: interfaz en tonos blancos y azul
- [ ] Tema **Oscuro**: fondo oscuro, texto claro
- [ ] Tema **Océano**: tonos azul celeste
- [ ] Tema **Atardecer**: tonos naranja y cálidos
- [ ] El tema persiste al refrescar la página (guardado en localStorage)

---

_Guía preparada para My-Negocio — Modelo de Membresías_
_Versión: 20 de febrero de 2026_
