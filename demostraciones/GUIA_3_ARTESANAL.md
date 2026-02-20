# GUIA DE TESTING #3 — ARTESANAL (Barbería / Spa / Salón de Belleza)

## Persona: Carlos Medina | Rol: Dueño de Barbería, Spa o Salón de Belleza

---

## OBJETIVO

Probar TODAS las funciones del modelo artesanal de My-Negocio: creación del catálogo de servicios, gestión de empleados, configuración de horarios semanales y excepciones, registro de clientes, agenda/calendario FullCalendar con ciclo de vida completo de citas (pendiente → confirmada → en progreso → completada → pago), inventario de productos, movimientos, reportes financieros, recibos, notificaciones y sugerencias.

---

## ANTES DE EMPEZAR

- El administrador ya creó tu negocio con tipo **"artesanal"**
- Te entregaron un **email** y una **password** para ingresar
- Tener listos al menos 3 datos de clientes de prueba (nombre, teléfono)
- Este flujo debe seguirse **en orden**: los servicios y empleados deben existir antes de crear citas

---

## FLUJO RECOMENDADO (el orden importa)

```
1. Login y verificar dashboard
2. Crear SERVICIOS (prerequisito para citas)
3. Crear EMPLEADOS (prerequisito para citas)
4. Configurar HORARIOS de cada empleado (prerequisito para ver slots)
5. Crear CLIENTES (recomendado antes de crear citas)
6. Usar la AGENDA y probar vistas del calendario
7. Crear CITAS (flujo normal, cliente nuevo, y click-en-calendario)
8. Ciclo de vida: confirmar → iniciar → completar → pagar
9. Cancelar una cita
10. Mover/reprogramar una cita
11. INVENTARIO: productos y operaciones de stock
12. MOVIMIENTOS: registrar ingresos y gastos manuales
13. REPORTES: ver gráficas financieras
14. RECIBOS: buscar y ver recibos generados
15. NOTIFICACIONES y SUGERENCIAS
16. Cambiar TEMA visual
```

---

## SECCION 1: LOGIN Y DASHBOARD

### Pasos

1. Ir a la URL del sistema (ej: `http://localhost:5170`)
2. Ingresar email y password del negocio artesanal
3. Click en **"Iniciar Sesion"**

### Verificar al entrar

- El dashboard que se abre es **DashboardArtesanal** (NO el de membresias)
- Se ven **4 tarjetas de estadísticas** en la parte superior:
  - **Citas Hoy** (numero de citas del dia, excluyendo canceladas)
  - **Ingresos Hoy** (suma de pagos registrados hoy, en formato `$0.00`)
  - **Completadas** (citas que llegaron al estado final exitoso)
  - **Canceladas** (citas que fueron canceladas hoy)
- El **calendario FullCalendar** se muestra por defecto en la vista Semana (en escritorio) o Dia (en móvil)
- La **barra de empleados** (tabs) aparece encima del calendario con un tab "Todos"
- Si el negocio está en periodo de prueba, aparece un **banner amarillo** con "Periodo de prueba" y un botón "Activar Plan"

### Sidebar (menú lateral izquierdo)

Verificar que el sidebar artesanal muestra exactamente estos items en este orden:

| Icono | Item |
|---|---|
| bi-calendar-week | **Agenda** (activo por defecto) |
| bi-people-fill | **Clientes** |
| bi-scissors | **Servicios** |
| bi-person-badge | **Empleados** |
| bi-journal-text | **Movimientos** |
| bi-graph-up | **Reportes** |
| bi-box-seam | **Inventario** |
| bi-receipt | **Recibos** |
| bi-bell-fill | **Notificaciones** (con badge rojo de conteo) |
| bi-chat-left-text | **Sugerencias** |

---

## SECCION 2: CREAR SERVICIOS

Los servicios son el catálogo de lo que ofrece el negocio. **Se necesitan antes de poder crear citas.**

### Acceder

- Click en **"Servicios"** en el sidebar izquierdo

### Crear el primer servicio

1. Click en el botón **"Nuevo Servicio"** (azul, arriba a la derecha)
2. Se abre el modal **"Nuevo Servicio"** con estos campos:
   - **Nombre** (requerido): escribir `Corte de Cabello`
   - **Descripcion** (opcional): escribir `Corte clásico con lavado`
   - **Precio ($)** (requerido): escribir `8`
   - **Duracion (min)** (requerido, por defecto `30`): dejar en `30`
   - **Items Incluidos** (opcional): escribir `Lavado|Corte|Peinado` (separados por `|`)
   - **Es un combo** (switch): dejar desactivado
3. Click en **"Guardar"**
4. Verificar que aparece un toast verde **"Servicio creado exitosamente"**
5. Verificar que el servicio aparece como una **tarjeta** en la grilla con:
   - Nombre en negrita: "Corte de Cabello"
   - Precio: `$8.00`
   - Duración: icono reloj + `30 min`
   - Los items "Lavado", "Corte", "Peinado" como badges azules

### Crear al menos 2 servicios adicionales

Repetir el proceso para crear:

**Servicio 2 — Manicure:**
- Nombre: `Manicure Clasica`
- Descripcion: `Limado, cuticula y esmaltado`
- Precio: `12`
- Duracion: `45`
- Items Incluidos: `Limado|Cuticula|Esmaltado`
- Es un combo: desactivado

**Servicio 3 — Tinte:**
- Nombre: `Tinte Completo`
- Descripcion: `Tinte raiz y puntas`
- Precio: `25`
- Duracion: `60`
- Items Incluidos: (vacío)
- Es un combo: desactivado

**Servicio 4 (opcional) — Paquete Novias (combo):**
- Nombre: `Paquete Novia`
- Descripcion: `Todo incluido para el gran dia`
- Precio: `80`
- Duracion: `120`
- Items Incluidos: `Peinado|Maquillaje|Manicure|Pedicure`
- Es un combo: **activado** (switch verde)
- Verificar que aparece el badge celeste **"Combo"** en la tarjeta

### Probar editar un servicio

1. En la tarjeta de "Corte de Cabello", click en el icono **tres puntos verticales** (⋮)
2. Click en **"Editar"**
3. El modal se abre en modo edición con el título **"Editar Servicio"** y los campos prellenados
4. Cambiar el precio de `8` a `10`
5. Click en **"Guardar"**
6. Verificar toast verde y que la tarjeta actualiza el precio a `$10.00`

### Probar eliminar (y que es soft-delete)

1. En la tarjeta de cualquier servicio, click en ⋮ → **"Eliminar"**
2. Aparece un modal de confirmación SweetAlert2: _"Se eliminara '[nombre]'"_ con botones "Eliminar" (rojo) y "Cancelar"
3. Click en **"Eliminar"**
4. Verificar toast verde y que la tarjeta desaparece de la grilla
5. Nota: el servicio no se borra de la base de datos (soft delete) — las citas pasadas siguen intactas

---

## SECCION 3: CREAR EMPLEADOS

Los empleados son quienes realizan los servicios. **Se necesitan antes de crear citas.**

### Acceder

- Click en **"Empleados"** en el sidebar

### Crear el primer empleado

1. Click en el botón **"Nuevo Empleado"** (azul, arriba a la derecha)
2. Se abre el modal **"Nuevo Empleado"** con estos campos:
   - **Nombre** (requerido): `Ana`
   - **Apellido** (requerido): `Torres`
   - **Telefono**: `0999123456`
   - **Email**: `ana@barberia.com`
   - **Especialidad**: `Colorista`
3. Click en **"Guardar"**
4. Verificar toast verde **"Empleado creado exitosamente"**
5. Verificar que aparece una tarjeta con:
   - Nombre: "Ana Torres"
   - Especialidad: "Colorista" (en texto gris pequeño)
   - Teléfono: con icono de teléfono
   - Menú ⋮ con opciones: **Editar**, **Horario**, **Eliminar**

### Crear un segundo empleado

Repetir el proceso:
- Nombre: `Juan`
- Apellido: `Perez`
- Telefono: `0988765432`
- Email: `juan@barberia.com`
- Especialidad: `Barbero`

### Verificar tabs de empleados en el calendario

- Ir a **"Agenda"** en el sidebar
- Verificar que ahora aparecen **tabs nuevos** en la barra superior del calendario:
  - Tab **"Todos"** (el primero, con icono de personas)
  - Tab **"Ana Torres"** con su especialidad "Colorista" y un **punto de color**
  - Tab **"Juan Perez"** con su especialidad "Barbero" y un **punto de color diferente**
- Cada empleado tiene un color asignado automáticamente de la paleta: azul (`#3E97FF`), verde (`#50CD89`), rojo (`#F1416C`), amarillo (`#FFC700`), morado (`#7239EA`), etc.
- El badge numérico (0) en cada tab muestra cuántas citas tiene ese empleado hoy

### Probar editar un empleado

1. En la tarjeta del empleado, click en ⋮ → **"Editar"**
2. Modal **"Editar Empleado"** se abre con campos prellenados
3. Cambiar especialidad de "Colorista" a "Colorista Senior"
4. Click en **"Guardar"**
5. Verificar toast y que la tarjeta actualiza la especialidad

---

## SECCION 4: CONFIGURAR HORARIOS DE EMPLEADOS

**Importante:** Al crear un empleado, el sistema automáticamente le asigna el horario por defecto: **Lunes a Sábado de 09:00 a 17:00, Domingo libre**. Este paso es para personalizar ese horario.

### Abrir el modal de horario

1. En la sección **"Empleados"**, en la tarjeta de "Ana Torres"
2. Click en el icono ⋮ → **"Horario"**
3. Se abre el modal **"Horario Semanal"** mostrando los **7 días de la semana**
4. Cada fila tiene:
   - Nombre del día (Domingo, Lunes, Martes, Miercoles, Jueves, Viernes, Sabado)
   - Un **switch** (toggle on/off) que indica si trabaja ese día
   - Campos de **hora inicio** y **hora fin** (tipo time picker)

### Configurar horario personalizado para Ana Torres

Configurar así:
- **Domingo**: switch OFF (libre)
- **Lunes**: switch ON, 08:00 a 18:00
- **Martes**: switch ON, 08:00 a 18:00
- **Miercoles**: switch ON, 08:00 a 18:00
- **Jueves**: switch ON, 08:00 a 18:00
- **Viernes**: switch ON, 08:00 a 18:00
- **Sabado**: switch ON, 09:00 a 14:00

Click en **"Guardar Horario"**. Verificar toast verde **"Horario guardado exitosamente"**.

### Verificar que el horario afecta los slots

1. Ir a **"Agenda"** → **"Nueva Cita"**
2. Seleccionar empleado: "Ana Torres"
3. Seleccionar servicio: "Corte de Cabello" (30 min)
4. Seleccionar fecha: un **domingo**
5. Verificar que aparece el mensaje **"El empleado no trabaja este día"** (sin slots)
6. Cambiar la fecha a un **lunes**
7. Verificar que aparecen slots desde las **08:00** hasta las **17:30** (cada 15 min, el último que cabe para un servicio de 30 min antes de las 18:00)
8. Cambiar la fecha a un **sábado**
9. Verificar que los slots van solo hasta las **13:30** (último que cabe antes de las 14:00)

### Configurar horario para Juan Perez

Repetir el mismo proceso para el segundo empleado con un horario diferente:
- Lunes a Viernes: 10:00 a 20:00
- Sábado: 10:00 a 16:00
- Domingo: OFF

---

## SECCION 5: CREAR CLIENTES ARTESANALES

Los clientes artesanales son un registro simple (nombre, teléfono, email, dirección). No tienen membresías ni fechas de vencimiento como en el modelo membresías.

### Acceder

- Click en **"Clientes"** en el sidebar

### Crear clientes

1. Click en el botón **"Nuevo Cliente"** (azul, arriba a la derecha)
2. Se abre el modal **"Nuevo Cliente"** con estos campos:
   - **Nombre** (requerido): `Maria`
   - **Apellido** (requerido): `Lopez`
   - **Email**: `maria@gmail.com`
   - **Telefono**: `0991234567`
   - **Direccion**: `Av. Principal 123`
3. Click en **"Guardar"**
4. Verificar que aparece en la tabla **clientesArtTable** con columnas: Cliente, Telefono, Email, Citas, Acciones
5. La columna "Citas" muestra `0` (aún no tiene citas)

Crear al menos 2 clientes más:
- `Pedro Sanchez`, tel: `0987654321`, email: `pedro@gmail.com`
- `Sofia Ruiz`, tel: `0976543210`, email: (vacío)

### Funciones adicionales de la tabla de clientes

- **WhatsApp**: si el cliente tiene teléfono, aparece un botón verde con icono de WhatsApp que abre `wa.me/{número}` en nueva pestaña
- **Editar** (icono lápiz azul): abre modal prellenado, permite modificar todos los datos
- **Eliminar** (icono papelera rojo): pide confirmación SweetAlert2 antes de eliminar

### Probar búsqueda en la tabla

- La tabla tiene búsqueda integrada de DataTables en la parte superior derecha
- Escribir "Maria" y verificar que filtra los resultados en tiempo real

---

## SECCION 6: LA AGENDA (CALENDARIO)

### Acceder

- Click en **"Agenda"** en el sidebar

### Vistas del calendario

El calendario usa **FullCalendar v6** con 3 vistas. Los botones de cambio de vista están arriba a la derecha del calendario:

| Botón | Vista |
|---|---|
| **Dia** | `timeGridDay` — columna de un solo día, franjas de 15 minutos |
| **Semana** | `timeGridWeek` — 7 columnas, una por día (vista por defecto en escritorio) |
| **Mes** | `dayGridMonth` — calendario mensual clásico |

**Probar cada vista:**
1. Click en **"Dia"** → verificar que muestra solo el día de hoy con líneas cada 15 min
2. Click en **"Semana"** → verificar que muestra los 7 días de la semana actual
3. Click en **"Mes"** → verificar que muestra el mes completo

### Navegación

- El calendario tiene flechas **< >** (izq/der) para ir al día/semana/mes anterior o siguiente
- Botón **"Hoy"** para volver a la fecha actual
- El título central muestra el rango de fechas visible

### Línea roja de hora actual

- En las vistas Día y Semana, verificar que hay una **línea roja horizontal** que indica la hora actual
- El calendario hace scroll automático para mostrar la hora actual al cargar

### Tabs de empleados

- Click en el tab **"Todos"**: muestra citas de todos los empleados
- Click en el tab **"Ana Torres"**: muestra solo las citas de Ana (con su color asignado)
- Click en el tab **"Juan Perez"**: muestra solo las citas de Juan
- Si un empleado **no trabaja hoy** (según su horario), su tab aparece atenuado (semi-transparente) y al hacer hover muestra tooltip: "No trabaja hoy"
- El número en el badge del tab muestra las citas agendadas para ese empleado en la fecha visible

### Rango de horas visible

El calendario muestra franjas desde las **07:00** hasta las **22:00** (no muestra fuera de ese rango).

---

## SECCION 7: CREAR CITAS

Esta es la función principal del modelo artesanal. Hay 3 formas de crear una cita.

---

### Test 7A — Cita normal (desde el botón "Nueva Cita")

1. Ir a la sección **"Agenda"**
2. Click en el botón **"Nueva Cita"** (azul, arriba a la izquierda)
3. Se abre el modal **"Nueva Cita"** con subtítulo dinámico (muestra el empleado si uno está seleccionado en el tab)

**Campos del modal:**

**Paso 1 — Empleado:**
- Si el tab activo es **"Todos"**, aparece el selector desplegable **"Seleccionar empleado..."** con los empleados que trabajan en la fecha seleccionada
- Si el tab activo es **"Ana Torres"**, el campo de empleado se oculta y el subtítulo muestra "Ana Torres - Colorista Senior"
- Seleccionar: `Ana Torres`

**Paso 2 — Servicio:**
- Desplegable **"Seleccionar servicio..."** con todos los servicios activos
- Cada opción muestra: `Nombre del Servicio - $precio (Xmin)`
- Seleccionar: `Corte de Cabello - $10.00 (30min)`

**Paso 3 — Cliente:**
Hay dos modos con botones:
- **"Buscar existente"** (botón azul activo por defecto): muestra un campo de búsqueda con autocompletado
- **"Crear nuevo"** (botón azul outline): muestra campos de formulario

**Modo "Buscar existente":**
- Escribir `Maria` en el campo de búsqueda
- Esperar 300ms (debounce) → aparece un dropdown con resultados
- Cada resultado muestra: avatar con iniciales, nombre completo, teléfono
- Click en "Maria Lopez" en el dropdown
- El campo de búsqueda desaparece y aparece una **tarjeta azul** con:
  - Icono de persona
  - Nombre: "Maria Lopez"
  - Teléfono: "0991234567"
  - Botón X para limpiar la selección

**Paso 4 — Fecha:**
- Selector de fecha: escribir o seleccionar **hoy** (fecha actual)
- Si el empleado tiene el domingo libre y hoy es domingo, la lista de slots estará vacía

**Paso 5 — Horario Disponible:**
- Al seleccionar fecha, se carga automáticamente la grilla de **slots disponibles**
- Los slots se muestran como **botones de hora** (`08:00`, `08:15`, `08:30`, etc.) en una grilla
- Los slots respetan el horario del empleado (08:00 - 18:00 para Ana en lunes a viernes)
- Los slots se generan cada **15 minutos**
- Los slots bloqueados por citas existentes NO aparecen
- Seleccionar el slot de las **10:00** → el botón se resalta en azul (clase `.active`)

**Resumen automático:**
- Aparece un alert azul informativo: `"Corte de Cabello - $10.00 (30min) con Ana Torres para Maria Lopez a las 10:00"`

**Agendar:**
- Click en **"Agendar Cita"** (botón azul, esquina inferior derecha del modal)
- El botón muestra spinner: "Agendando..."
- Al guardar exitosamente:
  - Toast verde: **"Cita creada exitosamente"**
  - Modal se cierra
  - El calendario se refresca y muestra el nuevo evento en color de Ana Torres (azul)
  - Las estadísticas "Citas Hoy" se actualiza
  - El badge del tab de Ana Torres incrementa

### Verificar el evento en el calendario

- En vista **Semana**, el evento muestra: `"Corte de Cabello - Maria Lopez"`
- El evento ocupa el bloque de tiempo 10:00 - 10:30
- El color de fondo es el color asignado a Ana Torres

---

### Test 7B — Cita con cliente NUEVO (crear al vuelo)

1. Click en **"Nueva Cita"**
2. Seleccionar empleado: `Juan Perez`
3. Seleccionar servicio: `Manicure Clasica` (45 min)
4. En la sección Cliente, click en **"Crear nuevo"** (el botón cambia a azul sólido)
5. Aparecen 4 campos en 2 columnas:
   - **Nombre**: `Roberto`
   - **Apellido**: `Mora`
   - **Telefono**: `0965432109` (requerido en modo crear nuevo)
   - **Email (opcional)**: dejar vacío
6. Seleccionar fecha: hoy (o mañana si Juan no trabaja hoy según su horario)
7. Ver que los slots se calculan con la duración de 45 minutos (cada slot bloquea 45 min)
8. Seleccionar un slot disponible
9. Click en **"Agendar Cita"**
10. Verificar toast verde
11. Ir a **"Clientes"** en el sidebar → verificar que "Roberto Mora" aparece en la tabla (fue creado automáticamente)

---

### Test 7C — Cita desde el calendario (click y arrastrar)

1. En la vista **"Dia"** del calendario
2. Click en cualquier **bloque horario vacío** (ej: las 14:00)
3. El sistema llama a `abrirModalCitaRapida` con la hora clickeada
4. El modal **"Nueva Cita"** se abre con:
   - La **fecha prellenada** con el día actual
   - Si hay un tab de empleado activo, el empleado ya está preseleccionado

   Alternativamente, en vista **Semana**, hacer click y **arrastrar** sobre un bloque vacío para seleccionar un rango de tiempo → el modal se abre con esa hora prellenada.

5. Seleccionar servicio: `Tinte Completo` (60 min)
6. Buscar y seleccionar cliente: `Pedro Sanchez`
7. Verificar que el slot seleccionado aparece resaltado
8. Click en **"Agendar Cita"**

---

### Errores esperados (validaciones)

Probar estos casos que DEBEN fallar:

| Caso | Error esperado |
|---|---|
| No seleccionar empleado | Toast amarillo: "Selecciona un empleado" |
| No seleccionar servicio | Toast amarillo: "Selecciona un servicio" |
| No seleccionar horario (slot) | Toast amarillo: "Selecciona un horario" |
| Modo "Buscar existente" sin seleccionar cliente | Toast: "Busca y selecciona un cliente, o cambia a modo Crear nuevo" |
| Modo "Crear nuevo" sin nombre | Toast: "El nombre del cliente es obligatorio" |
| Modo "Crear nuevo" sin teléfono | Toast: "El telefono del cliente es obligatorio" |
| Crear cita en fecha pasada | El servidor rechaza: "No se pueden crear citas en el pasado" |

---

## SECCION 8: CICLO DE VIDA COMPLETO DE UNA CITA

Esta es la máquina de estados del sistema artesanal. **Una cita pasa por estos estados en orden:**

```
pendiente → confirmada → en_progreso → completada → (registrar pago)
cualquier estado (excepto completada) → cancelada
```

### Abrir el detalle de una cita

- En el calendario, hacer **click** sobre cualquier evento de cita
- Se abre el modal **"Detalle de Cita"** mostrando:
  - Badge de estado (ej: "pendiente" en color según estado)
  - **Servicio**: nombre del servicio
  - **Cliente**: nombre del cliente
  - **Empleado**: nombre del empleado
  - **Fecha**: fecha y hora de inicio y fin (ej: `20/2/2026 10:00 - 10:30`)
  - **Precio**: precio del servicio al momento de la reserva

### Paso 1: pendiente → CONFIRMAR

1. Abrir la cita creada en el Test 7A (Corte de Cabello - Maria Lopez)
2. El modal muestra badge **"pendiente"** y botones en el footer:
   - **"Confirmar"** (azul)
   - **"Cancelar"** (rojo)
3. Click en **"Confirmar"**
4. Verificar toast verde: estado cambia a "confirmada"
5. El evento en el calendario cambia de color/estilo (clase CSS `cita-confirmada`)
6. Las estadísticas se actualizan

### Paso 2: confirmada → EN PROGRESO

1. Abrir la misma cita del calendar (ahora en estado "confirmada")
2. El modal muestra botones:
   - **"En Progreso"** (celeste)
   - **"Cancelar"** (rojo)
3. Click en **"En Progreso"**
4. Verificar toast verde
5. El evento en el calendario refleja el nuevo estado

### Paso 3: en_progreso → COMPLETAR Y PAGAR

1. Abrir la cita (ahora en estado "en_progreso")
2. El modal muestra en el footer:
   - **"Completar"** (verde)
   - Sin botón de cancelar (en_progreso solo puede ir a completada o cancelada, pero el sistema omite cancelar en este punto de la UI)
3. Click en **"Completar"**
4. El sistema realiza dos acciones en secuencia:
   a. Cambia el estado a "completada"
   b. Después de 500ms, **cierra el modal de detalle** y **abre automáticamente el modal de pago**

### Modal de Pago ("Registrar Pago")

El modal tiene estos campos:
- **Precio del Servicio**: campo de solo lectura con el precio prellenado (ej: `$10.00`)
- **Metodo de Pago**: desplegable con opciones:
  - `Efectivo`
  - `Tarjeta`
  - `Transferencia`
- **Cargos Extra ($)**: campo numérico (mínimo 0, por defecto 0)
- **Propina ($)**: campo numérico (mínimo 0, por defecto 0)
- **Detalle Extra** (aparece solo si Cargos Extra > 0): campo de texto para describir el cargo
- **"Es un regalo"** (switch): si se activa:
  - El campo de método de pago se deshabilita
  - Aparece campo **"Motivo del regalo"** (textarea, obligatorio)
  - El total se pone en $0.00
- **Total a cobrar**: alert verde con el total calculado en tiempo real: `$servicio + $extra + $propina`

**Probar pago normal:**
1. Método de pago: `Efectivo`
2. Cargos Extra: `2.50` (ej: tinte extra)
3. Detalle Extra aparece → escribir `Tinte extra`
4. Propina: `1.00`
5. Verificar que el Total muestra: **$13.50** ($10 + $2.50 + $1)
6. Click en **"Confirmar Pago"**
7. Verificar toast verde: **"Pago registrado exitosamente"**
8. Verificar que las estadísticas del dashboard actualizan:
   - **Ingresos Hoy**: ahora muestra `$13.50`
   - **Completadas**: se incrementa en 1
9. Verificar que en **"Recibos"** aparece un nuevo recibo

**Probar pago como regalo:**
1. Crear una cita nueva, llevarla a estado "completada"
2. En el modal de pago, activar el switch **"Es un regalo"**
3. Aparece el campo "Motivo del regalo" → escribir `Cortesia por cumpleanos`
4. El total baja a **$0.00** y el campo de método se deshabilita
5. Click en **"Confirmar Pago"**
6. Verificar que el pago se registra con total $0 y método "regalo"

### Paso 4: Verificar que no se puede pagar dos veces

1. Intentar abrir la cita ya pagada desde el calendario
2. El modal ya no muestra el botón "Registrar Pago" (porque `TienePago = true`)
3. Si se intentara manualmente vía el endpoint, el servidor rechaza: "Ya existe un pago registrado para esta cita"

---

## SECCION 9: CANCELAR UNA CITA

Las citas se pueden cancelar desde los estados: pendiente, confirmada, en_progreso.

1. Crear una cita nueva (estado: pendiente)
2. Abrir el detalle desde el calendario
3. Click en el botón **"Cancelar"** (rojo, en el footer del modal)
4. El modal de detalle se **cierra**
5. Se abre un modal diferente: **"Cancelar Cita"** con:
   - Texto informativo: "El motivo de cancelacion es obligatorio."
   - Campo **"Motivo"** (textarea, requerido)
   - Botones: **"Volver"** (gris) y **"Cancelar Cita"** (rojo)
6. Intentar click en "Cancelar Cita" **sin escribir motivo** → toast amarillo: "El motivo es obligatorio"
7. Escribir el motivo: `Cliente no se presentó`
8. Click en **"Cancelar Cita"**
9. Verificar toast verde: "Cita cancelada"
10. El evento en el calendario refleja el nuevo estado (clase `cita-cancelada`, color diferente)
11. Las estadísticas "Canceladas" se incrementa en 1

**Verificar que una cita completada NO se puede cancelar:**
- Abrir una cita en estado "completada"
- Verificar que el modal NO muestra botón "Cancelar" (solo muestra "Cerrar")

---

## SECCION 10: MOVER / REPROGRAMAR UNA CITA

Las citas pendientes, confirmadas y en_progreso se pueden mover. Las completadas y canceladas NO.

### Método 1: Drag & Drop en el calendario

1. Ir a la vista **Día** o **Semana** del calendario
2. Hacer **clic y arrastrar** un evento de cita a otro horario vacío en el mismo día o en otro día
3. Soltar el evento
4. El sistema envía la nueva hora al servidor
5. Si hay un conflicto (el empleado ya tiene otra cita en ese slot): toast rojo **"El empleado ya tiene una cita en ese horario"** y el evento **vuelve a su posición original** automáticamente
6. Si no hay conflicto: toast verde **"Cita movida exitosamente"** y el evento queda en la nueva posición
7. La duración del servicio se conserva (si duraba 30 min, sigue durando 30 min)

**Nota:** El drag & drop solo funciona en **escritorio** (ventana >= 576px de ancho). En móvil el calendario es de solo lectura.

### Método 2: Probar doble-booking (debe fallar)

1. Crear dos citas para el mismo empleado en el mismo horario (segunda intención)
2. Al intentar crear la segunda cita para la misma hora, el sistema rechaza: **"El empleado ya tiene una cita en ese horario"**
3. Intentar mover una cita a un horario ocupado → mismo error

---

## SECCION 11: HISTORIAL DE CLIENTE

Desde la tabla de clientes se puede ver el historial de citas de cada cliente.

1. Ir a **"Clientes"** en el sidebar
2. En la columna "Citas" de la tabla, verificar que el contador de "Maria Lopez" ya muestra el número de citas que se le agendaron (no solo las completadas, sino todas)

Para ver el historial detallado (si el endpoint `GetHistorialCliente` está expuesto en la UI futura), el servicio devuelve las últimas 50 citas del cliente ordenadas de más reciente a más antigua, con: nombre del servicio, empleado, fecha, duración, precio y estado.

---

## SECCION 12: INVENTARIO DE PRODUCTOS

El inventario es compartido entre todos los tipos de negocio. Para el artesanal es útil para rastrear productos usados en los servicios (tintes, esmaltes, champús, etc.).

### Acceder

- Click en **"Inventario"** en el sidebar

### Estadísticas

En la parte superior hay 4 tarjetas de estadísticas:
- **Total Productos**: cantidad total de productos activos
- **Stock Bajo**: productos cuyo stock actual es menor al stock mínimo definido (alerta amarilla)
- **Ventas Hoy**: unidades vendidas hoy
- **Ingresos Hoy**: ingresos por ventas de productos hoy (en $)

### Crear un producto

1. Click en **"Nuevo Producto"** (botón azul)
2. Modal **"Nuevo Producto"** con campos:
   - **Nombre del Producto** (requerido): `Shampoo Profesional`
   - **Precio de Venta ($)** (requerido): `8.50`
   - **Costo de Compra ($)** (requerido): `4.00`
   - Al escribir precio y costo, aparece **margen de ganancia** calculado debajo del campo
   - **Stock Inicial** (requerido): `20`
   - **Stock Minimo (alerta)** (por defecto 5): `5`
3. Click en **"Guardar"**
4. Verificar que la tarjeta del producto aparece en la grilla con:
   - Nombre: "Shampoo Profesional"
   - Precio: `$8.50`
   - Stock actual: `20` (en verde porque está por encima del mínimo)
   - Flecha arriba (↑) y flecha abajo (↓) a los lados del número de stock

### Crear al menos 2 productos más

- Producto 2: Tinte Negro, Precio: `12.00`, Costo: `5.00`, Stock: `30`, StockMin: `5`
- Producto 3: Esmalte Rojo, Precio: `3.50`, Costo: `1.50`, Stock: `50`, StockMin: `10`

### Operaciones de stock

**Venta rapida (flecha abajo ↓):**
1. En la tarjeta de "Shampoo Profesional", click en la **flecha abajo** (▼)
2. El stock disminuye en 1 inmediatamente (optimistic update visual)
3. El sistema envía `POST /Negocios/VenderProducto` con cantidad=1
4. Toast verde con el mensaje de confirmación
5. Las estadísticas "Ventas Hoy" e "Ingresos Hoy" se actualizan
6. Si el stock llega a 0, la flecha abajo aparece deshabilitada

**Devolver producto (flecha arriba ↑ → opción Devolver):**
1. Click en la **flecha arriba** (▲) del Shampoo Profesional
2. Aparece un SweetAlert2 con dos opciones:
   - **"Devolver Producto"** (amarillo): para registrar una devolución de cliente
   - **"Actualizar Inventario"** (azul): para hacer un ajuste manual de stock
3. Click en **"Devolver Producto"**
4. Aparece un segundo modal dentro del SweetAlert con:
   - Campo **"Cantidad a devolver"** (number, mínimo 1, por defecto 1)
   - **"Razon de la devolucion"** (textarea, obligatorio): `Cliente cambio de opinion`
5. Intentar confirmar sin escribir razón → error de validación
6. Escribir la razón y click en **"Confirmar Devolucion"**
7. El stock del producto aumenta
8. Toast verde de confirmación

**Ajuste de inventario (flecha arriba ↑ → opción Actualizar Inventario):**
1. Click en ▲ → click en **"Actualizar Inventario"** (azul)
2. Aparece modal con:
   - Stock actual mostrado: ej. "Stock en sistema: 19"
   - Campo **"Stock real (contado)"**: escribir `25`
   - Al escribir, aparece indicador de diferencia: `+6 unidades` (fondo verde) o diferencia negativa (fondo rojo)
   - **"Nota"** (textarea, obligatorio): `Reabastecimiento de proveedor`
3. Si se escribe el mismo número que el stock actual → error: "El stock es igual, no hay cambio"
4. Click en **"Actualizar Stock"**
5. El stock del producto se ajusta al nuevo valor
6. Toast verde

**Editar y eliminar producto:**
- En la tarjeta, botones pequeños en la esquina superior derecha (aparecen al hacer hover):
  - **Icono lápiz**: abre modal "Nuevo Producto" prellenado para edición
  - **Icono papelera rojo**: pide confirmación → elimina (soft delete)

### Ver historial de movimientos

1. Click en el botón **"Historial"** (outline azul, con icono de reloj)
2. Aparece una tabla debajo de los productos con columnas: Fecha, Producto, Tipo, Cant., Total, Stock, Nota
3. Los tipos de movimiento tienen badges de color:
   - `venta`: verde
   - `devolucion`: amarillo
   - `restock`: celeste
   - `ajuste`: rojo
4. Click nuevamente en **"Historial"** para ocultar la tabla

### Exportar a Excel

- Click en el botón **"Exportar"** (verde, icono de Excel)
- El navegador descarga un archivo `.xlsx` con el inventario completo

---

## SECCION 13: MOVIMIENTOS (Ingresos y Gastos Manuales)

Esta sección permite registrar movimientos financieros que NO vienen de citas ni de ventas de inventario (ej: compra de suministros, pago de servicios, otros ingresos).

### Acceder

- Click en **"Movimientos"** en el sidebar

### Registrar un movimiento

El formulario está en la parte superior de la sección:
- **Descripcion**: campo de texto, ej: `Compra de esmaltes` (obligatorio)
- **Monto ($)**: número positivo para ingresos, negativo para gastos (obligatorio)
  - Ejemplo ingreso: `50` → aparece en verde en la tabla
  - Ejemplo gasto: `-30` → aparece en rojo con signo negativo
- Click en **"Registrar"**

**Registrar 2 movimientos de prueba:**
1. Descripción: `Venta de productos adicionales`, Monto: `15` → click Registrar
2. Descripción: `Compra de tintes y champus`, Monto: `-45` → click Registrar

### Tabla de movimientos

La tabla tiene columnas: **Descripcion, Tipo, Cliente, Monto, Fecha**
- Los movimientos de citas completadas aparecen automáticamente con tipo `cita_completada`
- Los movimientos manuales aparecen con tipo `ingreso` o `gasto` según el signo del monto
- Los montos positivos se muestran en **verde** (`$15.00`)
- Los montos negativos se muestran en **rojo** (`-$45.00`)
- La tabla soporta búsqueda y ordenamiento por columna
- **Nota importante**: los registros son **inmutables** (el texto "Los registros son inmutables" aparece en gris con icono de candado) — no hay botón de eliminar

### Exportar movimientos

- Click en **"Exportar"** (botón verde, icono de Excel) → descarga `.xlsx` con todos los movimientos

---

## SECCION 14: REPORTES FINANCIEROS

### Acceder

- Click en **"Reportes"** en el sidebar

### Filtrar por periodo

Tres botones (pills) para filtrar:
- **"Dia"** (activo por defecto): muestra datos del día de hoy
- **"Semana"**: muestra datos de los últimos 7 días
- **"Mes"**: muestra datos del mes actual

### Tarjetas de resumen

Al entrar, se ven 3 tarjetas con fondo de gradiente:
- **Ingresos del Dia** (fondo azul): suma de todos los movimientos positivos del periodo seleccionado
- **Gastos del Dia** (fondo rojo): suma de todos los movimientos negativos del periodo
- **Ganancia Neta** (fondo verde): Ingresos - Gastos

Los labels de las tarjetas cambian según el periodo: "del Dia", "de la Semana", "del Mes"

**Probar cambio de periodo:**
1. Click en **"Semana"** → los números cambian y los labels dicen "de la Semana"
2. Click en **"Mes"** → los números cambian y los labels dicen "del Mes"
3. Click en **"Dia"** → volver al estado inicial

### Gráficas

Debajo de las tarjetas hay 2 gráficas con **ApexCharts**:

**Ingresos vs Gastos (barra):**
- Muestra los últimos 6 meses
- Dos series: Ingresos (verde) y Gastos (rojo)
- Barras con bordes redondeados
- El eje X muestra el nombre del mes abreviado

**Resumen del Dia (dona/donut):**
- Muestra la proporción de ingresos vs gastos del periodo seleccionado
- El centro de la dona muestra la "Ganancia" en $
- Si no hay datos para el periodo, muestra: "Sin datos para este periodo"
- El gráfico de dona se actualiza al cambiar el filtro de periodo

---

## SECCION 15: RECIBOS

Los recibos se generan automáticamente cuando se registra el pago de una cita completada.

### Acceder

- Click en **"Recibos"** en el sidebar

### Tabla de recibos

Columnas: **#Recibo, Destinatario, Concepto, Monto, Fecha, Acciones**

### Buscar un recibo

- Usar el campo de búsqueda **"# Recibo..."** en la parte superior derecha
- Escribir el número del recibo para filtrar

### Ver el recibo

- En la columna Acciones de cualquier recibo, click en el botón correspondiente
- Se abre un modal **"Recibo #[numero]"** con un `<iframe>` que renderiza el recibo en HTML

### Exportar recibos a Excel

- Click en el botón **"Exportar"** (verde, icono Excel) → descarga `.xlsx`

### Banner de limpieza

- Si hay recibos con más de 30 días de antigüedad, aparece un banner amarillo:
  _"Tienes recibos con más de 30 días."_ con botón **"Exportar y Limpiar"**
- Al hacer click, exporta todos los recibos antiguos a Excel y luego los elimina del sistema

---

## SECCION 16: NOTIFICACIONES

### Acceder

- Click en **"Notificaciones"** en el sidebar

### Badge de conteo

- En el sidebar, el item "Notificaciones" tiene un **badge rojo** que muestra el número de notificaciones no leídas
- Al ingresar a la sección, la lista muestra las notificaciones del negocio

### Acciones

- **"Marcar todas leidas"** (botón outline azul, arriba a la derecha): marca todas las notificaciones como leídas y el badge desaparece
- Click en una notificación individual para marcarla como leída

---

## SECCION 17: SUGERENCIAS

### Acceder

- Click en **"Sugerencias"** en el sidebar

### Enviar una sugerencia

1. Escribir en el textarea (máximo 1000 caracteres): `Me gustaria que el sistema envie recordatorios por WhatsApp a los clientes antes de su cita`
2. El contador en tiempo real muestra: `85/1000`
3. Click en **"Enviar Sugerencia"**
4. Verificar toast verde de confirmación

---

## SECCION 18: CAMBIAR TEMA VISUAL

El sistema tiene 4 temas visuales disponibles. El botón de cambio de tema está en la **topbar** (barra superior).

### Probar los 4 temas

1. Click en el icono de paleta de colores / selector de tema en la topbar
2. Probar cada tema y verificar que toda la interfaz cambia:
   - **Light** (por defecto): fondo blanco, textos oscuros
   - **Dark**: fondo oscuro (#1E1E2D), textos claros
   - **Ocean**: tonos azul marino
   - **Sunset**: tonos naranja/rojo cálidos
3. Verificar que el tema se guarda en `localStorage('mg-theme')` — al recargar la página, el tema permanece

---

## PUNTOS CLAVE PARA DEMO A DUEÑOS DE BARBERIA / SPA

- **El calendario es la pantalla principal**: se ve todo de un vistazo, por empleado, por día/semana/mes
- **Los slots se calculan solos**: según la duración del servicio, el horario del empleado y las citas existentes — no hay que calcular manualmente
- **Sin doble reserva**: el sistema previene automáticamente que un empleado tenga dos citas al mismo tiempo
- **Ciclo completo en la misma pantalla**: confirmación, inicio, completado y cobro sin salir del calendario
- **El pago acepta propina y cargos extra**: ideal para salones donde el precio puede variar con productos adicionales
- **Regalo sin costo**: se puede marcar una cita como "regalo" y el total queda en $0 con registro del motivo
- **Cada empleado tiene su color**: fácil identificar visualmente quién tiene qué cita en el calendario
- **Horarios flexibles**: se puede configurar un horario distinto para cada día de la semana por empleado, más excepciones (vacaciones, feriados)
- **Historial inmutable**: todos los movimientos financieros y pagos quedan registrados permanentemente para auditoría
- **Inventario integrado**: los productos del salón (tintes, esmaltes, etc.) se gestionan en el mismo sistema

---

## CHECKLIST FINAL

Marcar cada item al completarlo durante la prueba:

### LOGIN Y ACCESO
- [ ] Login exitoso con credenciales del negocio artesanal
- [ ] Dashboard artesanal se abre correctamente (NO el de membresias)
- [ ] Las 4 tarjetas de estadísticas muestran datos (Citas Hoy, Ingresos Hoy, Completadas, Canceladas)
- [ ] El sidebar muestra los 10 items correctos: Agenda, Clientes, Servicios, Empleados, Movimientos, Reportes, Inventario, Recibos, Notificaciones, Sugerencias
- [ ] El calendario FullCalendar carga correctamente en vista Semana (escritorio)

### SERVICIOS
- [ ] Crear "Corte de Cabello" ($10, 30 min) con items incluidos
- [ ] Crear "Manicure Clasica" ($12, 45 min)
- [ ] Crear "Tinte Completo" ($25, 60 min)
- [ ] Crear un servicio combo con switch "Es un combo" activado → badge "Combo" visible
- [ ] Editar un servicio (cambiar precio) → cambio reflejado en tarjeta
- [ ] Eliminar un servicio → desaparece de la grilla

### EMPLEADOS
- [ ] Crear empleado "Ana Torres" (Colorista) con email y teléfono
- [ ] Crear empleado "Juan Perez" (Barbero)
- [ ] Verificar que ambos aparecen como tabs en el calendario con colores distintos
- [ ] Editar un empleado → cambio reflejado

### HORARIOS
- [ ] Abrir horario de Ana Torres → modal muestra los 7 días con toggle y campos de hora
- [ ] Configurar horario personalizado (Lun-Vie 08:00-18:00, Sáb 09:00-14:00, Dom libre)
- [ ] Guardar horario → toast verde
- [ ] Verificar en "Nueva Cita" que un domingo no muestra slots para Ana
- [ ] Verificar que un lunes muestra slots desde las 08:00 para Ana
- [ ] Configurar horario diferente para Juan Perez

### CLIENTES
- [ ] Crear "Maria Lopez" con teléfono y email
- [ ] Crear "Pedro Sanchez" y "Sofia Ruiz"
- [ ] Verificar tabla con columnas: Cliente, Telefono, Email, Citas, Acciones
- [ ] Botón de WhatsApp aparece para clientes con teléfono
- [ ] Editar un cliente → cambio reflejado en tabla
- [ ] Búsqueda en tabla funciona correctamente

### AGENDA — VISTAS DEL CALENDARIO
- [ ] Vista Dia funciona (una columna, líneas cada 15 min)
- [ ] Vista Semana funciona (7 columnas)
- [ ] Vista Mes funciona (cuadrícula mensual)
- [ ] Navegación con flechas < > y botón "Hoy" funciona
- [ ] Línea roja de hora actual visible en vistas Dia y Semana
- [ ] Tab "Todos" muestra citas de todos los empleados
- [ ] Tab de empleado individual filtra el calendario por ese empleado
- [ ] Tab de empleado que no trabaja hoy aparece atenuado con tooltip "No trabaja hoy"

### CREAR CITAS
- [ ] Cita normal (botón "Nueva Cita"): empleado + servicio + buscar cliente existente + slots + agendar
- [ ] El autocompletado de búsqueda de cliente funciona (mínimo 2 caracteres, debounce 300ms)
- [ ] Al seleccionar cliente, aparece tarjeta azul con nombre y teléfono + botón X para limpiar
- [ ] Los slots disponibles se cargan automáticamente al seleccionar empleado + servicio + fecha
- [ ] Los slots respetan el horario del empleado (no aparecen fuera del horario)
- [ ] El resumen automático se muestra en el alert azul
- [ ] Cita con "Crear nuevo" cliente: se crea el cliente on-the-fly y aparece en la tabla de clientes
- [ ] Click en bloque vacío del calendario abre el modal con hora prellenada
- [ ] Validaciones: error si falta empleado, servicio, o slot

### CICLO DE VIDA DE CITA
- [ ] pendiente → Confirmar → toast verde + estado actualizado en calendario
- [ ] confirmada → En Progreso → toast verde + estado actualizado
- [ ] en_progreso → Completar → estado cambia a completada Y se abre automáticamente el modal de pago
- [ ] Pago normal: precio prellenado, método efectivo, cargo extra con detalle, propina, total calculado en tiempo real
- [ ] Confirmar Pago → toast verde + Ingresos Hoy actualizado + recibo generado
- [ ] Pago como regalo: switch activado, motivo obligatorio, total = $0
- [ ] No se puede pagar dos veces la misma cita

### CANCELAR CITA
- [ ] pendiente → Cancelar → modal "Cancelar Cita" se abre
- [ ] Intentar cancelar sin motivo → error "El motivo es obligatorio"
- [ ] Cancelar con motivo → toast verde + estado cancelada en calendario + contador Canceladas +1
- [ ] Cita completada NO muestra botón Cancelar

### MOVER CITA
- [ ] Drag & drop en vista Semana mueve la cita correctamente → toast verde
- [ ] Drag & drop a horario ocupado → toast rojo + cita vuelve a su posición original
- [ ] La duración del servicio se conserva al mover

### INVENTARIO
- [ ] Crear "Shampoo Profesional" (stock 20, precio $8.50, costo $4.00)
- [ ] Crear al menos 2 productos más
- [ ] Tarjetas de stats actualizan correctamente
- [ ] Flecha abajo (venta rápida): stock -1, toast verde, stats actualizan
- [ ] Flecha arriba → "Devolver Producto": motivo obligatorio, stock +N
- [ ] Flecha arriba → "Actualizar Inventario": indicador de diferencia funciona, nota obligatoria, stock se ajusta
- [ ] Producto con stock por debajo del mínimo muestra badge "Stock bajo" en rojo y aparece en stat "Stock Bajo"
- [ ] Exportar Excel descarga el archivo
- [ ] Botón "Historial" muestra/oculta la tabla de movimientos
- [ ] Tabla de movimientos muestra los 4 tipos con badges de color correctos

### MOVIMIENTOS
- [ ] Registrar ingreso positivo ($15) → aparece en verde en la tabla
- [ ] Registrar gasto negativo (-$45) → aparece en rojo en la tabla
- [ ] Los pagos de citas aparecen automáticamente como movimientos tipo "cita_completada"
- [ ] Nota "Los registros son inmutables" visible
- [ ] Exportar a Excel funciona

### REPORTES
- [ ] Tarjetas de Ingresos, Gastos, Ganancia Neta muestran datos del dia actual
- [ ] Cambiar a "Semana" → labels y datos cambian correctamente
- [ ] Cambiar a "Mes" → labels y datos cambian correctamente
- [ ] Gráfica de barras (Ingresos vs Gastos, últimos 6 meses) se renderiza
- [ ] Gráfica de dona (Resumen) se renderiza y muestra ganancia en el centro
- [ ] Si no hay datos en el periodo → mensaje "Sin datos para este periodo"

### RECIBOS
- [ ] Recibos generados por pagos de citas aparecen en la tabla
- [ ] Búsqueda por número de recibo funciona
- [ ] Click en acción → modal con iframe del recibo se abre
- [ ] Exportar a Excel funciona

### NOTIFICACIONES
- [ ] Badge rojo en sidebar muestra el conteo de no leídas
- [ ] "Marcar todas leidas" funciona → badge desaparece

### SUGERENCIAS
- [ ] Contador de caracteres funciona en tiempo real (máx 1000)
- [ ] Enviar sugerencia → toast verde de confirmación

### TEMAS
- [ ] Cambiar a tema **Dark** → toda la interfaz cambia a fondo oscuro
- [ ] Cambiar a tema **Ocean** → tonos azul marino
- [ ] Cambiar a tema **Sunset** → tonos cálidos
- [ ] Recargar la página → el tema seleccionado persiste (guardado en localStorage)

---

## NOTAS TECNICAS PARA EL TESTER

| Comportamiento | Explicacion |
|---|---|
| Los slots se generan cada 15 minutos | El servidor itera de 15 en 15 min desde la apertura del empleado |
| El slot más tarde visible para un servicio de 30 min con cierre a las 18:00 es 17:30 | `inicio + duración <= fin` |
| Los slots de días pasados no aparecen | El servidor valida `FechaHoraInicio >= TimeHelper.Now` |
| Al eliminar un servicio, las citas pasadas siguen intactas | Soft delete: `IsActive = false`, las citas guardan copia del nombre y precio |
| Al eliminar un empleado con citas pendientes, el sistema falla | El servidor verifica citas activas antes de la baja lógica |
| Los pagos de citas se reflejan en la tabla de Movimientos automáticamente | El `CitaService` llama a `_logService.CreateLogAsync` al registrar el pago |
| El historial de movimientos y pagos es inmutable | No hay endpoint de eliminación de logs individuales |
| Los colores de los tabs de empleados son fijos y cíclicos | Paleta: azul, verde, rojo, amarillo, morado, celeste, gris, negro oscuro |

---

*Guia generada para My-Negocio — Modelo Artesanal (Barbería / Spa / Salón de Belleza)*
*Fecha de referencia: 2026-02-20*
