# GUIA DE TESTING #5 — RESTAURANTE

## Persona: Carlos Mendoza | Rol: Dueño de Restaurante "El Rincón Criollo"

---

## OBJETIVO

Probar TODAS las funciones del modelo restaurante en My-Negocio:

- POS con los 3 tipos de orden: **Local** (con mesa), **Para Llevar**, **Delivery** (con repartidor y dirección)
- Gestión de mesas en tiempo real con colores visuales (Libre / Ocupada / Reservada)
- Menús digitales compartibles por WhatsApp y email con 5 estilos visuales
- Distribuidores (proveedores de ingredientes)
- Repartidores de delivery con contacto directo por WhatsApp
- Inventario de platos con fotos, recetas, categorías y drag & drop
- Reportes financieros, recibos, movimientos, notificaciones y sugerencias

---

## ANTES DE EMPEZAR

- El admin ya creó tu negocio con tipo "restaurante"
- Te dieron un email y password para ingresar
- URL del sistema: `http://localhost:5170` (o la URL de producción que te indicaron)
- Vas a necesitar: un celular con WhatsApp para probar el envío de menús y el botón "Llamar Delivery"

---

## FLUJO RECOMENDADO (orden lógico)

Para que la demo fluya sin problemas, seguir este orden:

```
1. Login y verificar dashboard POS
2. Crear MESAS (las necesitas en el POS para órdenes locales)
3. Crear CATEGORIAS de platos
4. Crear PLATOS en el inventario
5. Crear REPARTIDORES (para el tipo de orden Delivery)
6. Crear DISTRIBUIDORES (proveedores)
7. Probar POS con los 3 tipos de orden
8. Crear MENÚS digitales
9. Operaciones de stock, reportes, recibos, movimientos
10. Funciones adicionales: notificaciones, sugerencias, temas
```

> **Nota:** Si vas directo al POS sin crear platos, el grid estará vacío y no podrás agregar nada al carrito. Crea al menos 3 platos antes de probar el POS.

---

## SECCION 1: LOGIN Y PRIMER VISTAZO AL DASHBOARD

### 1.1 Ingresar al sistema

1. Ir a la URL del sistema
2. Ingresar email y password proporcionados por el admin
3. Click en **Iniciar Sesión**
4. Debes llegar directo al **POS** (Punto de Venta) — es la primera vista del restaurante

### 1.2 Verificar las 4 tarjetas de estadísticas (parte superior del POS)

Al entrar, verás 4 tarjetas en la parte superior:

| Tarjeta | Qué muestra | ID en pantalla |
|---|---|---|
| **Ventas Hoy** | Número de unidades vendidas hoy | `posVentasHoy` |
| **Ingresos Hoy** | Total en dólares cobrado hoy | `posIngresosHoy` |
| **Platos** | Total de platos en inventario | `posTotalProductos` |
| **Stock Bajo** | Platos con stock debajo del mínimo | `posStockBajo` (rojo) |

- Al inicio todo debería estar en 0 (sistema nuevo)
- Verificar que no haya errores de JavaScript en la consola del navegador (F12)

### 1.3 Verificar la barra lateral (Sidebar)

El sidebar tiene exactamente estas 11 opciones para restaurante:

```
POS            (icono carrito)     — vista por defecto
Mesas          (icono cuadricula)
Menus          (icono libro)
Inventario     (icono caja)
Distribuidores (icono camion)
Repartidores   (icono bicicleta)
Movimientos    (icono diario)
Reportes       (icono grafico)
Recibos        (icono recibo)
Notificaciones (icono campana)    — tiene badge rojo con contador
Sugerencias    (icono burbuja)
```

- Si ves "Periodo de prueba" en un banner amarillo en la parte superior, es normal — significa que el negocio está en modo trial.

---

## SECCION 2: CREAR MESAS

> Las mesas son necesarias para las órdenes tipo "Local" en el POS. Créalas primero.

### 2.1 Navegar a Mesas

- Click en **Mesas** en el sidebar
- Verás la pantalla de mesas con 3 leyendas de color:
  - Badge verde: **Libre**
  - Badge rojo: **Ocupada**
  - Badge amarillo: **Reservada**
- Si no hay mesas, verás un mensaje vacío con ícono de cuadrícula

### 2.2 Crear la primera mesa

1. Click en el botón azul **"Nueva Mesa"** (esquina superior derecha)
2. Se abre el modal **"Nueva Mesa"** con 3 campos:
   - **Nombre** (obligatorio): escribir `Mesa 1`
   - **Numero**: escribir `1`
   - **Capacidad**: escribir `4`
3. Click en **Guardar**
4. Debe aparecer un toast verde: *"Mesa creada exitosamente"*
5. La mesa aparece como una tarjeta verde (estado "libre") con el número grande

### 2.3 Crear las 6 mesas del restaurante

Repetir el proceso para crear estas mesas:

| Nombre | Numero | Capacidad |
|---|---|---|
| Mesa 2 | 2 | 4 |
| Mesa 3 | 3 | 4 |
| Mesa 4 | 4 | 6 |
| Terraza 1 | 5 | 2 |
| VIP | 6 | 8 |

> Al terminar debes ver 6 tarjetas de color verde en la pantalla.

### 2.4 Cambiar el estado de una mesa

1. Click sobre la tarjeta de **"Mesa 2"**
2. Se abre un popup (SweetAlert) con el nombre y opciones:
   - Botón **"Cambiar Estado"** (azul)
   - Botón **"Editar"** (gris)
   - Botón **"Cerrar"** (cancelar)
3. Click en **"Cambiar Estado"**
4. Aparece un selector con 3 opciones: `Libre`, `Ocupada`, `Reservada`
5. Seleccionar **"Ocupada"** → Click **"Cambiar"**
6. La tarjeta cambia a color **rojo**

7. Repetir con **Mesa 3**: cambiar a estado **"Reservada"**
8. La tarjeta de Mesa 3 debe quedar en color **amarillo** con texto oscuro

Verificar que los colores son:
- Verde = Libre
- Rojo = Ocupada
- Amarillo = Reservada

### 2.5 Editar una mesa

1. Click sobre **"Terraza 1"**
2. Click en **"Editar"** (botón gris en el popup)
3. El modal se abre con los datos actuales precargados:
   - Nombre: `Terraza 1`
   - Numero: `5`
   - Capacidad: `2`
4. Cambiar Capacidad a `3`
5. Click en **Guardar**
6. Toast verde: *"Mesa actualizada exitosamente"*
7. La tarjeta de Terraza 1 debe mostrar ahora `3` personas

### 2.6 Volver Mesa 2 a Libre (para usarla en el POS después)

1. Click sobre **Mesa 2** (roja)
2. Click **"Cambiar Estado"** → Seleccionar `Libre` → **"Cambiar"**
3. Vuelve a verde

---

## SECCION 3: CREAR CATEGORIAS Y PLATOS EN EL INVENTARIO

### 3.1 Navegar a Inventario

- Click en **Inventario** en el sidebar
- Verás los botones: **Nuevo Plato**, **Nueva Categoria**, **Exportar**, **Historial**
- Y 4 tarjetas de stats: Total Platos, Stock Bajo, Ventas Hoy, Ingresos Hoy
- Un texto de ayuda: *"Arrastra platos sobre las carpetas para organizarlos"*

### 3.2 Crear las 6 categorías

Para cada categoría:

1. Click en **"Nueva Categoria"** (botón azul con borde)
2. Se abre modal con campo **"Nombre de la categoria"**
3. Escribir el nombre → Click **Guardar**
4. Toast verde confirma la creación
5. Aparece una carpeta amarilla en la pantalla

Crear estas 6 categorías:

| Nombre |
|---|
| Entradas |
| Sopas |
| Platos Fuertes |
| Bebidas |
| Jugos |
| Postres |

> Cada carpeta muestra el nombre y un badge azul con el número de platos que tiene (empieza en 0).

### 3.3 Crear los platos del menú

Para cada plato:

1. Click en **"Nuevo Plato"**
2. Se abre el modal **"Nuevo Plato"** con estos campos:
   - **Nombre del plato** (obligatorio, máx. 200 caracteres)
   - **Precio Venta** (precio que paga el cliente)
   - **Costo** (costo de ingredientes, para calcular margen)
   - **Stock** (unidades disponibles hoy)
   - **Stock Minimo** (cuándo alertar de stock bajo, por defecto 5)
   - **Categoria** (dropdown con las categorías creadas)
   - **Foto del Plato** (opcional, JPG/PNG/WebP, máx. 2MB)
   - **Receta / Ingredientes** (sección colapsable, para uso interno del personal)
3. Click en **Guardar**

Crear estos platos:

| Nombre | Precio Venta | Costo | Stock | Stock Min | Categoria |
|---|---|---|---|---|---|
| Sopa del día | 4.50 | 1.20 | 20 | 5 | Sopas |
| Arroz con pollo | 7.50 | 2.50 | 15 | 3 | Platos Fuertes |
| Ceviche de camarón | 8.00 | 3.00 | 10 | 3 | Entradas |
| Jugo natural | 2.50 | 0.50 | 30 | 5 | Jugos |
| Tiramisu | 5.00 | 1.50 | 8 | 2 | Postres |
| Cola / Agua | 1.50 | 0.40 | 50 | 10 | Bebidas |
| Ensalada fresca | 4.00 | 1.00 | 12 | 3 | Entradas |
| Llapingachos | 6.50 | 2.00 | 8 | 2 | Platos Fuertes |

> Al crear el primer plato en cada categoría, el contador de la carpeta sube de 0 a 1.

### 3.4 Agregar una foto a un plato

1. Click en el botón de lápiz (editar) debajo de **Arroz con pollo**
2. Se abre el modal de edición con los datos precargados
3. En el campo **"Foto del Plato"** → seleccionar una imagen del computador
4. Aparece una **vista previa** de la imagen debajo del campo (antes de guardar)
5. Click en **Guardar**
6. En el grid de inventario, la tarjeta del plato muestra la foto en lugar del ícono por defecto

### 3.5 Agregar una receta a un plato

1. Click en editar de **Ceviche de camarón**
2. En la sección **"Receta / Ingredientes"** (viene colapsada, hacer click para expandir)
3. Escribir en el textarea:
   ```
   Ingredientes:
   - 500g camarones frescos
   - 1 cebolla paiteña
   - 3 limones
   - 2 tomates
   - Cilantro fresco
   - Sal, pimienta, ají

   Preparación: Mezclar camarones cocidos con limón...
   ```
4. Click en **Guardar**
5. Verificar que la receta se guardó (volver a editar el plato y expandir la sección)

> La receta es solo para uso interno del personal — no aparece en el menú digital público.

### 3.6 Probar drag & drop para organizar platos en carpetas

1. En el inventario, tomar la tarjeta de **Ensalada fresca** (que debería estar en Entradas)
2. Hacer click y arrastrar hacia la carpeta **"Sopas"**
3. Soltar sobre la carpeta
4. Toast: *"Plato movido"*
5. La tarjeta desaparece y reaparece con la categoría actualizada
6. El contador de Entradas baja 1, el de Sopas sube 1
7. Volver a arrastrar la ensalada de vuelta a Entradas

---

## SECCION 4: CREAR REPARTIDORES (para órdenes Delivery)

> Los repartidores son quienes llevan las órdenes a domicilio. El sistema reutiliza el modelo de "Empleado" del tipo artesanal, guardando el vehículo en el campo "especialidad".

### 4.1 Navegar a Repartidores

- Click en **Repartidores** en el sidebar
- Tabla vacía con columnas: Nombre, Apellido, Teléfono, Email, Vehículo, Acciones

### 4.2 Crear el primer repartidor

1. Click en **"Nuevo Repartidor"**
2. Se abre modal **"Nuevo Repartidor"** con estos campos:
   - **Nombre** (obligatorio)
   - **Apellido** (obligatorio)
   - **Teléfono** (para WhatsApp — muy importante para el botón "Llamar Delivery")
   - **Email** (opcional)
   - **Vehículo** (dropdown): `Moto`, `Bicicleta`, `Auto`, `A pie`
3. Llenar el formulario y click **Guardar**

Crear estos 2 repartidores:

| Nombre | Apellido | Teléfono | Email | Vehículo |
|---|---|---|---|---|
| Diego | Romero | 0987654321 | diego@reparto.com | Moto |
| Ana | Paredes | 0991234567 | ana@reparto.com | Bicicleta |

> **IMPORTANTE:** El número de teléfono debe ser válido con código de Ecuador (09xx) para que el botón "Llamar Delivery" abra WhatsApp correctamente. El sistema convierte automáticamente el `09xx` a `593xx`.

### 4.3 Verificar que aparecen en la tabla

- La tabla muestra ambos repartidores con sus datos
- La columna "Vehículo" muestra el tipo seleccionado

### 4.4 Editar un repartidor

1. Click en el ícono de lápiz de **Diego Romero**
2. El modal se abre con datos precargados
3. Cambiar el teléfono o cualquier campo
4. Click **Guardar**

---

## SECCION 5: CREAR DISTRIBUIDORES (proveedores de ingredientes)

> Los distribuidores son los proveedores que te surten de ingredientes. El sistema reutiliza el modelo de "Cliente Artesanal", usando el campo Nombre como nombre de la empresa y Apellido como nombre del contacto.

### 5.1 Navegar a Distribuidores

- Click en **Distribuidores** en el sidebar
- Tabla vacía con columnas: Compania, Contacto, Teléfono, Email, Acciones

### 5.2 Crear distribuidores

1. Click en **"Nuevo Distribuidor"**
2. Modal **"Nuevo Distribuidor"** con campos:
   - **Nombre de la compania** (obligatorio) — ej: "Distribuidora Lácteos del Norte"
   - **Contacto** — nombre de la persona de contacto
   - **Teléfono**
   - **Email**
3. Click **Guardar**

Crear estos 2 distribuidores:

| Compania | Contacto | Teléfono | Email |
|---|---|---|---|
| Mariscos del Pacífico S.A. | Roberto Lema | 0984567890 | ventas@mariscos.com |
| Distribuidora El Campo | María José Ríos | 0978901234 | mjrios@campo.ec |

### 5.3 Editar y eliminar

- Click en el lápiz de **Mariscos del Pacífico** → cambiar el teléfono → Guardar
- Click en el ícono de basurero de un distribuidor → confirmar eliminación

---

## SECCION 6: POS — ORDEN LOCAL (con mesa asignada)

> La orden "Local" es para clientes que comen en el restaurante. Se puede (opcionalmente) asignar una mesa.

### 6.1 Navegar al POS

- Click en **POS** en el sidebar
- Verás dos paneles:
  - **Panel izquierdo**: "Platos" — grid con todos los platos del inventario y filtros por categoría
  - **Panel derecho**: "Factura" — carrito de compra y opciones de pago

### 6.2 Familiarizarse con el panel de platos

- **Filtros de categoría** (pills azules en la parte superior): Todos, Entradas, Sopas, Platos Fuertes, etc.
- **Buscador** con ícono de lupa: buscar plato por nombre
- Cada tarjeta de plato muestra:
  - Foto (si tiene) o ícono de taza
  - Nombre del plato
  - Precio de venta en azul
  - Stock disponible
  - Si stock = 0: aparece "Agotado" en rojo y la tarjeta se ve diferente (no se puede agregar)

### 6.3 Configurar la orden Local

En el panel derecho "Factura":

1. **Tipo de orden**: Verificar que el pill **"Local"** está activo (azul)
   - Los 3 pills: `Local`, `Para Llevar`, `Delivery`
   - Al seleccionar "Local" → aparece el selector de mesa
   - Al seleccionar "Para Llevar" → desaparece el selector de mesa
   - Al seleccionar "Delivery" → aparece selector de repartidor y campo de dirección

2. **Selector de mesa** (dropdown): Seleccionar **"Mesa 1 (4 pers.)"**
   - Solo aparecen las mesas en estado "Libre"
   - Mesa 2 y Mesa 3 no aparecen porque las pusimos en Ocupada/Reservada

3. **Nombre del cliente** (campo de texto): escribir `Familia Suárez` (opcional)

### 6.4 Agregar platos al carrito

1. Click en la tarjeta de **"Sopa del día"**
   - Se agrega al carrito en el panel derecho
   - El botón **"Cobrar"** se activa (deja de estar en gris)

2. Click en **"Arroz con pollo"**
   - Aparece una segunda línea en el carrito

3. Click en **"Jugo natural"** (2 veces)
   - Primera vez: se agrega con cantidad 1
   - Segunda vez: la cantidad sube a 2

4. Verificar el carrito: debe mostrar 3 líneas con botones `—` cantidad `+` y el subtotal de cada ítem

5. Ajustar cantidad de "Sopa del día":
   - Click en el `+` para subir a 2
   - Click en el `—` para bajar a 1

6. Eliminar un ítem: Click en el ícono de `X` rojo junto a "Jugo natural" → desaparece

### 6.5 Aplicar IVA y Descuento

En la sección de totales del carrito:

- **IVA %**: cambiar de 0 a `12`
  - El monto de IVA se calcula automáticamente: `Subtotal × 0.12`

- **Descuento %**: escribir `10` (10% de descuento)
  - La línea "Descuento" muestra el monto negativo en rojo

- Verificar que el **TOTAL** refleja: `Subtotal + IVA - Descuento`

### 6.6 Procesar el cobro

1. Click en el botón verde **"Cobrar"** (parte inferior del carrito)
2. Aparece popup de confirmación (SweetAlert) con:
   - El total en verde en grande
   - Número de platos y tipo de orden
   - Botón **"Confirmar Pago"** (verde)
   - Botón cancelar
3. Click en **"Confirmar Pago"**
4. El botón "Cobrar" muestra spinner: *"Procesando..."*
5. Al completarse:
   - Se abre el **modal de Recibo** con el recibo en un iframe
   - El recibo muestra: número de recibo, nombre del restaurante, fecha/hora, detalle de platos, subtotal, IVA, descuento, total
   - El carrito se vacía
   - Las estadísticas se actualizan (Ventas Hoy +1, Ingresos Hoy + el total cobrado)
   - La tabla "Ventas Recientes" en la parte inferior se actualiza

### 6.7 Acciones del recibo

En el modal del recibo tienes 3 opciones:

1. **Botón "WhatsApp"** (verde): Pide un número de celular → envía el recibo por WhatsApp
2. **Botón "Email"** (azul claro): Pide un email → envía el recibo por correo
3. **Botón "Imprimir"** (azul): Abre una ventana nueva con el recibo listo para imprimir

Probar al menos una de estas opciones (enviar por WhatsApp o email).

### 6.8 Verificar la tabla "Ventas Recientes"

Al pie del POS, la tabla **"Ventas Recientes"** debe mostrar la orden recién creada con:
- Número de orden (ej: `#1`)
- Cliente: "Familia Suárez"
- Tipo: badge verde "Local"
- Items: número de platos
- Total: el monto cobrado
- Fecha: hora actual

---

## SECCION 7: POS — ORDEN PARA LLEVAR

### 7.1 Configurar la orden

1. En el panel "Factura", click en el pill **"Para Llevar"**
   - El selector de mesa **desaparece**
   - Los campos de delivery no aparecen

2. **Nombre del cliente**: escribir `Pedro Torres`

3. Agregar 2-3 platos al carrito:
   - **Ceviche de camarón**
   - **Tiramisu**

4. No aplicar IVA ni descuento esta vez (dejar en 0)

### 7.2 Cobrar

1. Click en **"Cobrar"**
2. El popup muestra: `[monto]`, *"2 plato(s) - para llevar"*
3. Confirmar
4. Se abre el recibo
5. Verificar que en la tabla "Ventas Recientes" aparece badge amarillo/naranja **"Para Llevar"**

---

## SECCION 8: POS — ORDEN DELIVERY (con repartidor y dirección)

> Este es el tipo más completo. Requiere haber creado repartidores previamente (Sección 4).

### 8.1 Configurar la orden

1. Click en el pill **"Delivery"** en el carrito
   - Aparece el **selector de repartidor** (dropdown)
   - Aparece el **campo de dirección** de entrega
   - Aparece el botón verde **"Llamar Delivery"** (WhatsApp)

2. **Selector de repartidor**: Click → seleccionar **"Diego Romero (Moto)"**

3. **Dirección de entrega**: escribir `Calle Bolívar 234 y Av. 10 de Agosto`

4. **Nombre del cliente**: escribir `Laura Vera`

5. Agregar platos:
   - **Arroz con pollo** × 2
   - **Cola / Agua** × 2

### 8.2 Probar el botón "Llamar Delivery"

1. Click en el botón verde **"Llamar Delivery"**
2. Se abre WhatsApp Web (o la app de WhatsApp) con:
   - El número de Diego Romero (`+593987654321`)
   - Un mensaje pre-escrito: *"Hola Diego Romero, necesito un servicio de delivery para [nombre restaurante], por favor venga a [dirección del restaurante]"*
3. No es necesario enviar el mensaje — verificar que abre WhatsApp correctamente
4. Si no hay teléfono configurado en el repartidor, aparece un toast de advertencia

### 8.3 Cobrar la orden Delivery

1. Click en **"Cobrar"**
2. Confirmar el pago
3. Recibo generado
4. En la tabla "Ventas Recientes" aparece badge azul **"Delivery"**

---

## SECCION 9: CREAR MENÚS DIGITALES

> Los menús digitales son el feature más diferenciador del restaurante. Se crean con un constructor visual, se guardan, y se comparten por WhatsApp o email. El cliente ve el menú bonito en su celular sin necesidad de instalar nada.

### 9.1 Navegar a Menús

- Click en **Menus** en el sidebar
- Verás la tabla de menús guardados (vacía al inicio)
- Botón **"Nuevo Menu"** en la esquina superior derecha

### 9.2 Crear el Menú del Almuerzo (con precio fijo)

1. Click en **"Nuevo Menu"**
2. El constructor de menú se abre, dividido en dos partes:
   - **Panel izquierdo**: "Platos Disponibles" — lista de todos tus platos con buscador
   - **Panel derecho**: "Constructor de Menu" — donde armas el menú

3. En el Constructor (panel derecho):

   a. **Nombre del menu**: escribir `Menú Ejecutivo del Almuerzo`

   b. **Tipo de menú** (pills): Click en **"Almuerzo"**
      - Al seleccionar Almuerzo o Cena, aparece el campo **"Precio fijo"**

   c. **Precio fijo**: escribir `3.50`
      - Este precio aplica para todo el menú (combo completo)
      - Los precios individuales de los platos NO aparecen en el menú público

   d. Click en **"Agregar Seccion"**
      - Aparece un popup que pide el nombre de la sección
      - Escribir `Sopas` → click **Agregar**
      - Aparece la carpeta "Sopas" en el constructor

   e. Crear más secciones del mismo modo:
      - `Segundo`
      - `Bebida`
      - `Postre`

   f. Agregar platos a cada sección desde el panel izquierdo:
      - Buscar **"Sopa del día"** → click en el botón `+` azul → se agrega a la última sección activa (Postre — la última creada)
      - **Importante**: los platos se agregan siempre a la ÚLTIMA sección. Crear las secciones en orden y agregar los platos después de crear cada sección.
      - Agregar a **Sopas**: "Sopa del día"
      - Agregar a **Segundo**: "Arroz con pollo", "Llapingachos"
      - Agregar a **Bebida**: "Jugo natural", "Cola / Agua"
      - Agregar a **Postre**: "Tiramisu"

      > Para agregar platos a una sección específica, el sistema agrega a la última sección. Si necesitas agregar a una sección anterior, arrástrala hacia arriba con el ícono de 6 puntos (grip).

   g. Verificar que el constructor muestra las 4 secciones con sus platos

4. **Seleccionar estilo visual**:
   - Verás 5 pills de estilo: `Moderno`, `Elegante`, `Minimalista`, `Vibrante`, `Clasico`
   - El pill activo (azul) indica el estilo seleccionado
   - Click en **"Elegante"** — perfecto para un menú de almuerzo ejecutivo

5. **Vista Previa**:
   - Click en el botón **"Vista Previa"**
   - Se abre el modal **"Vista Previa del Menu"** con el HTML del menú renderizado
   - El menú muestra: nombre del restaurante, nombre del menú, precio fijo del combo, secciones con platos
   - En el estilo Elegante: fondo oscuro (#2c3e50), fuente serif, letras cursivas
   - Verificar que el precio fijo aparece en el hero (encabezado)
   - Verificar que los platos NO tienen precios individuales (es precio de combo)
   - Click en **"Imprimir"** para ver el diálogo de impresión del navegador
   - Cerrar la vista previa

6. **Guardar el menú**:
   - Click en **"Guardar Menu"**
   - Toast: *"Menu creado exitosamente"*
   - El constructor desaparece y vuelves a la tabla de menús
   - El menú "Menú Ejecutivo del Almuerzo" aparece en la tabla con columnas: Nombre, Tipo (badge azul "almuerzo"), Precio ($3.50), Estilo (elegante), Fecha

### 9.3 Crear el Menú General (carta completa)

1. Click en **"Nuevo Menu"** nuevamente
2. **Nombre**: `Carta General`
3. **Tipo**: Click en **"General"** (el campo de precio fijo desaparece)
4. Agregar secciones:
   - `Entradas`
   - `Sopas`
   - `Platos Fuertes`
   - `Bebidas y Jugos`
   - `Postres`
5. Agregar todos los platos a sus secciones correspondientes
6. **Estilo**: Click en **"Moderno"**
7. Ver Vista Previa — verificar que cada plato muestra su precio individual
8. Click en **"Guardar Menu"**

### 9.4 Crear dos menús más (para probar los 5 estilos)

Opcional pero recomendado para la demo:

| Nombre | Tipo | Precio | Estilo |
|---|---|---|---|
| Desayuno Americano | Desayuno | — | Vibrante |
| Cena Romántica | Cena | 18.00 | Minimalista |

### 9.5 Compartir un menú por WhatsApp

1. En la tabla de menús, click en el ícono de **ojo** (vista previa) de "Carta General"
2. Se abre la vista previa del menú
3. Cerrar la vista previa
4. En la tabla, hay botones de acción:
   - Ojo: Vista previa
   - Lápiz: Editar
   - Basurero: Eliminar
5. Desde la vista previa, usar el botón **"Imprimir"** para imprimir el menú

**Para compartir el link por WhatsApp o Email** — esto se hace desde el modal de preview o desde el link público:
- La URL pública del menú es: `/Restaurante/VerMenu?negocioId=[ID]&menuId=[ID]`
- Esta URL no requiere login — cualquier persona puede verla en su celular
- Abrir esa URL en el celular para verificar que el menú se ve bien en pantalla pequeña

> **IMPORTANTE para la demo a clientes:** Mostrar que el menú abre perfectamente en el celular sin instalar ninguna app. El cliente escanea un QR o hace click en el link de WhatsApp y ve el menú completo con el estilo elegido.

### 9.6 Editar un menú existente

1. Click en el ícono de **lápiz** del "Menú Ejecutivo del Almuerzo"
2. El constructor se abre con todos los datos cargados:
   - Nombre relleno
   - Tipo seleccionado (Almuerzo)
   - Precio fijo relleno
   - Secciones y platos cargados
   - Estilo seleccionado (Elegante)
3. Cambiar el precio fijo de `3.50` a `4.00`
4. Click en **"Guardar Menu"**
5. Toast: *"Menu actualizado exitosamente"*

### 9.7 Eliminar un menú

1. Click en el ícono de **basurero** del menú "Desayuno Americano" (si lo creaste)
2. Popup de confirmación: *"Eliminar menu?"*
3. Click en **"Si, eliminar"** (rojo)
4. Toast: *"Menu eliminado exitosamente"*
5. El menú desaparece de la tabla

---

## SECCION 10: OPERACIONES DE STOCK (fuera del POS)

> Estas operaciones son para ajustes manuales de inventario, devoluciones y reabastecimientos.

### 10.1 Navegar a Inventario

- Click en **Inventario** en el sidebar

### 10.2 Vender manualmente (fuera del POS)

1. En la tarjeta de **Sopa del día**, click en el ícono de **carrito** (botón verde con ícono `cart-dash`)
2. Aparece popup con input numérico: *"Vender Sopa del día"*
3. Escribir `3` → click **"Vender"**
4. Toast: confirmación
5. El stock de la tarjeta baja de 20 a 17

### 10.3 Restock (reabastecer)

1. En la tarjeta de **Sopa del día**, click en el ícono de **caja con flecha** (botón azul claro, ícono `box-arrow-in-down`)
2. Popup: *"Restock"* con input numérico, valor por defecto `10`
3. Escribir `15` → click **"Agregar stock"**
4. Toast: confirmación
5. El stock sube de 17 a 32

### 10.4 Ver historial de movimientos del inventario

1. Click en el botón **"Historial"** (arriba, con ícono de reloj)
2. Se despliega la sección de movimientos con tabla:
   - Columnas: Fecha, Producto, Tipo, Cant., Total, Stock (anterior → nuevo), Nota
3. Verificar que aparecen los movimientos de:
   - Las ventas procesadas en el POS (Sección 6, 7, 8)
   - La venta manual (paso 10.2)
   - El restock (paso 10.3)
4. Los movimientos están ordenados del más reciente al más antiguo

### 10.5 Exportar inventario a Excel

1. Click en el botón **"Exportar"** (verde con ícono de Excel)
2. Se descarga automáticamente un archivo `Inventario_[fecha].xlsx`
3. Abrirlo en Excel/LibreOffice y verificar:
   - Hoja 1: "Productos" — lista de todos los platos con precio, costo, stock, categoría
   - Hoja 2: "Movimientos" — historial completo

---

## SECCION 11: MOVIMIENTOS FINANCIEROS (LOGS)

> Los movimientos son el registro inmutable de ingresos y gastos del restaurante, separado del inventario.

### 11.1 Navegar a Movimientos

- Click en **Movimientos** en el sidebar
- Verás un formulario para registrar movimientos y la tabla de registros anteriores

### 11.2 Registrar un ingreso manual

1. En el campo **"Descripcion"**: escribir `Venta de tortas especiales de encargo`
2. En el campo **"Monto ($)"**: escribir `45.00` (positivo = ingreso)
3. Click en **"Registrar"**
4. Toast: *"Movimiento registrado"*
5. Aparece en la tabla con tipo "ingreso" en badge verde

### 11.3 Registrar un gasto

1. **Descripcion**: `Compra de ingredientes - Mariscos del Pacífico`
2. **Monto ($)**: escribir `-30.00` (negativo = gasto)
3. Click en **"Registrar"**
4. Aparece en la tabla con tipo "gasto" en badge rojo

> El texto de ayuda dice: *"Usa valores positivos para ingresos y negativos para gastos"*

### 11.4 Exportar movimientos a Excel

- Click en el botón **"Exportar"** (verde)
- Se descarga archivo Excel con el historial completo de movimientos

---

## SECCION 12: REPORTES FINANCIEROS

### 12.1 Navegar a Reportes

- Click en **Reportes** en el sidebar
- 3 tarjetas con gradiente de color: Ingresos, Gastos, Ganancia Neta
- 2 gráficos: "Ingresos vs Gastos" (barras) y "Resumen" (donut)
- Pills de periodo: **Dia**, **Semana**, **Mes**

### 12.2 Probar los 3 periodos

1. Click en **"Dia"** (activo por defecto)
   - Las tarjetas muestran los números del día actual
   - Los gráficos se actualizan con datos del día

2. Click en **"Semana"**
   - Las tarjetas y gráficos cambian a datos de la semana

3. Click en **"Mes"**
   - Las tarjetas muestran totales del mes

### 12.3 Interpretar los gráficos

- **Gráfico de barras** (Ingresos vs Gastos): muestra evolución temporal — barras verdes (ingresos) y rojas (gastos)
- **Gráfico donut** (Resumen): proporciones — muestra ingresos totales, gastos totales, y en el centro la **Ganancia Neta**
- Si no hay datos del periodo, el donut muestra: *"Sin datos para este periodo"*

---

## SECCION 13: RECIBOS

### 13.1 Navegar a Recibos

- Click en **Recibos** en el sidebar
- Tabla con columnas: #Recibo, Destinatario, Concepto, Monto, Fecha, Acciones
- Los recibos de las ventas del POS (Secciones 6, 7, 8) ya deben estar aquí
- Banner de alerta amarillo: *"Tienes recibos con más de 30 días"* (si aplica)

### 13.2 Ver un recibo

1. Click en el botón **"Ver"** (ojo azul) de cualquier recibo
2. Se abre el modal con el recibo en un iframe
3. El recibo muestra:
   - Encabezado con nombre del restaurante y número de recibo
   - Detalle de platos vendidos
   - Subtotal, IVA, descuento, total
   - Tipo de orden y datos del cliente
4. Probar las acciones del recibo:
   - **WhatsApp**: enviar a un número de celular
   - **Email**: enviar a un correo
   - **Imprimir**: abrir diálogo de impresión

### 13.3 Buscar un recibo

- En el campo de búsqueda (parte superior): escribir el número de recibo
- La tabla filtra en tiempo real

### 13.4 Exportar recibos a Excel

- Click en botón **"Exportar"** (verde)
- Se descarga `Recibos_[fecha].xlsx`

---

## SECCION 14: NOTIFICACIONES

### 14.1 Navegar a Notificaciones

- Click en **Notificaciones** en el sidebar
- (Si el badge rojo tiene un número, hay notificaciones no leídas)
- Lista de notificaciones con mensaje y fecha

### 14.2 Marcar como leídas

- Click en el botón **"Marcar todas leidas"**
- Las notificaciones se marcan como leídas (aparecen con opacidad reducida)
- El badge rojo del sidebar desaparece

---

## SECCION 15: SUGERENCIAS

### 15.1 Enviar una sugerencia al equipo de My-Negocio

1. Click en **Sugerencias** en el sidebar
2. Escribir en el textarea (mínimo 10 caracteres, máximo 1000):
   ```
   Sería útil tener la opción de imprimir las órdenes directamente
   a una impresora de cocina (ticket de cocina) desde el POS.
   También me gustaría poder ver el historial de órdenes por mesa.
   ```
3. Verificar que el contador (abajo derecha) muestra el número de caracteres
4. Click en **"Enviar Sugerencia"**
5. Toast verde: *"Sugerencia enviada!"*
6. El campo se limpia automáticamente

---

## SECCION 16: TEMAS Y PERSONALIZACIÓN VISUAL

### 16.1 Cambiar el tema visual

En la esquina superior derecha del topbar hay un ícono de paleta de colores (o sol/luna). Click para ver los 4 temas:

1. **Light** (por defecto): Fondo blanco, colores azules
2. **Dark**: Fondo oscuro, ideal para uso nocturno
3. **Ocean**: Tonos azul/verde del mar
4. **Sunset**: Tonos cálidos naranja/morado

Probar cada uno:
- El tema cambia instantáneamente
- Se guarda en `localStorage` del navegador
- Si se recarga la página, el tema persiste

---

## PUNTOS CLAVE PARA LA DEMO A DUEÑOS DE RESTAURANTE

### Lo que hace diferente a My-Negocio para restaurantes:

1. **3 tipos de orden en un solo POS**
   - Local con mesa asignada (la mesa aparece en el selector solo si está "Libre")
   - Para llevar (sin mesa)
   - Delivery con repartidor específico y dirección de entrega

2. **Gestión de mesas en tiempo real**
   - Los colores Verde/Rojo/Amarillo son visualmente inmediatos
   - Un mesero puede cambiar el estado con 2 clicks
   - El POS solo muestra mesas "Libres" disponibles para asignar

3. **Menús digitales que se comparten por WhatsApp**
   - El cliente recibe el link por WhatsApp y ve el menú en su celular
   - No necesita instalar nada, no hay app, no hay código QR especial
   - El dueño puede actualizar el menú en segundos (agrega/quita platos)

4. **5 estilos de menú para cualquier tipo de restaurante**
   - Moderno (azul, Inter) — restaurante casual
   - Elegante (serif, fondo oscuro, dorado) — restaurante fino
   - Minimalista (blanco/negro, tipografía condensada) — café gourmet
   - Vibrante (gradiente fucsia, Poppins bold) — comida rápida, food trucks
   - Clásico (tonos marrones, Georgia) — restaurante familiar tradicional

5. **Menú Almuerzo/Cena con precio de combo**
   - Precio fijo para el set completo
   - Los platos individuales no muestran precio (es un combo)
   - Ideal para almuerzos ejecutivos de $3.50 o $4.00

6. **Contacto directo con repartidor por WhatsApp**
   - Un click abre WhatsApp con el número del repartidor
   - El mensaje ya viene pre-escrito con la dirección del restaurante
   - Ideal para coordinar deliveries sin salir del sistema

7. **Fotos y recetas de platos**
   - El POS muestra las fotos de los platos
   - Las recetas son solo para uso interno del personal
   - Drag & drop para organizar platos en categorías

---

## CHECKLIST FINAL — VERIFICACION COMPLETA

Marcar cada ítem al completarlo:

### Login y Dashboard
- [ ] Login exitoso con credenciales del restaurante
- [ ] El POS se muestra como primera vista
- [ ] Las 4 tarjetas de estadísticas cargan correctamente (Ventas, Ingresos, Platos, Stock Bajo)
- [ ] El sidebar muestra las 11 opciones del restaurante (POS, Mesas, Menus, Inventario, Distribuidores, Repartidores, Movimientos, Reportes, Recibos, Notificaciones, Sugerencias)

### Mesas
- [ ] Crear 6 mesas con diferentes nombres, números y capacidades
- [ ] Las mesas aparecen como tarjetas de color verde (Libre)
- [ ] Cambiar estado de una mesa a "Ocupada" → tarjeta se vuelve roja
- [ ] Cambiar estado de una mesa a "Reservada" → tarjeta se vuelve amarilla
- [ ] Editar el nombre/capacidad de una mesa
- [ ] Solo las mesas "Libres" aparecen en el selector del POS

### Categorias e Inventario
- [ ] Crear 6 categorías (Entradas, Sopas, Platos Fuertes, Bebidas, Jugos, Postres)
- [ ] Las categorías aparecen como carpetas amarillas con contador
- [ ] Crear al menos 8 platos con nombre, precio, costo, stock y categoría
- [ ] Subir una foto a un plato → la foto aparece en la tarjeta del inventario y en el POS
- [ ] Agregar receta/ingredientes a un plato (sección colapsable)
- [ ] Drag & drop: arrastrar un plato sobre una carpeta de categoría → plato se mueve
- [ ] Los platos aparecen en el POS con sus categorías y fotos

### Repartidores
- [ ] Crear 2 repartidores con teléfono y tipo de vehículo
- [ ] Aparecen en la tabla de repartidores
- [ ] Editar un repartidor
- [ ] Los repartidores aparecen en el selector del POS al elegir "Delivery"

### Distribuidores
- [ ] Crear 2 distribuidores con nombre de compañía y contacto
- [ ] Aparecen en la tabla de distribuidores
- [ ] Editar y eliminar un distribuidor

### POS — Orden Local
- [ ] Pill "Local" activo por defecto
- [ ] El selector de mesa muestra solo las mesas Libres
- [ ] Agregar platos al carrito haciendo click en sus tarjetas
- [ ] Ajustar cantidades con botones + y —
- [ ] Eliminar un ítem del carrito con el botón X rojo
- [ ] Aplicar IVA y verificar que el cálculo es correcto
- [ ] Aplicar descuento y verificar que el total es correcto
- [ ] Procesar cobro → recibo se genera automáticamente
- [ ] Recibo muestra datos correctos (platos, IVA, total, tipo de orden)
- [ ] Botón WhatsApp del recibo abre popup para ingresar número
- [ ] Botón Imprimir del recibo abre diálogo de impresión
- [ ] La tabla "Ventas Recientes" muestra la orden con badge "Local"

### POS — Orden Para Llevar
- [ ] Pill "Para Llevar" oculta el selector de mesa
- [ ] Procesar cobro → recibo generado
- [ ] Tabla "Ventas Recientes" muestra badge "Para Llevar"

### POS — Orden Delivery
- [ ] Pill "Delivery" muestra selector de repartidor y campo de dirección
- [ ] Botón "Llamar Delivery" aparece al seleccionar Delivery
- [ ] Al no seleccionar repartidor, el botón "Llamar Delivery" muestra advertencia
- [ ] Con repartidor seleccionado, el botón abre WhatsApp con mensaje pre-escrito
- [ ] Procesar cobro → recibo generado
- [ ] Tabla "Ventas Recientes" muestra badge "Delivery"

### Menús Digitales
- [ ] Constructor de menú carga los platos del inventario en el panel izquierdo
- [ ] Crear sección → aparece carpeta en el constructor
- [ ] Agregar plato a sección → plato aparece con nombre y precio
- [ ] Tipo "Almuerzo" muestra campo de precio fijo
- [ ] Tipo "General" oculta el campo de precio fijo
- [ ] Vista previa muestra el menú renderizado en HTML
- [ ] Estilo "Moderno": fondo azul degradado en el header
- [ ] Estilo "Elegante": fondo oscuro serif, texto dorado en precio fijo
- [ ] Estilo "Minimalista": fondo negro, tipografía sans condensada
- [ ] Estilo "Vibrante": gradiente fucsia/morado
- [ ] Estilo "Clásico": tonos marrones cálidos
- [ ] Guardar menú → aparece en la tabla de menús
- [ ] Botón ojo → vista previa del menú guardado
- [ ] Botón lápiz → constructor carga los datos del menú para editar
- [ ] Botón basurero → confirmar eliminación → menú desaparece de la tabla
- [ ] URL pública `/Restaurante/VerMenu?negocioId=...&menuId=...` funciona sin login
- [ ] El menú de Almuerzo muestra el precio fijo y NO los precios individuales
- [ ] El menú General muestra los precios individuales de cada plato

### Inventario — Operaciones de Stock
- [ ] Venta manual desde inventario → stock disminuye correctamente
- [ ] Restock → stock aumenta correctamente
- [ ] Historial de movimientos muestra todas las operaciones con Stock Anterior → Stock Nuevo
- [ ] Exportar a Excel descarga archivo .xlsx con hojas "Productos" y "Movimientos"

### Movimientos (Logs)
- [ ] Registrar ingreso con monto positivo → aparece con badge "ingreso" verde
- [ ] Registrar gasto con monto negativo → aparece con badge "gasto" rojo
- [ ] Los campos se limpian después de registrar
- [ ] Exportar movimientos a Excel funciona

### Reportes
- [ ] Tarjeta "Ingresos" muestra el total del periodo
- [ ] Tarjeta "Gastos" muestra el total del periodo
- [ ] Tarjeta "Ganancia Neta" = Ingresos - Gastos
- [ ] Pill "Dia" → datos del día actual
- [ ] Pill "Semana" → datos de la semana
- [ ] Pill "Mes" → datos del mes
- [ ] Gráfico de barras se renderiza sin errores
- [ ] Gráfico donut muestra proporciones correctas

### Recibos
- [ ] Los recibos de las ventas del POS aparecen en la tabla
- [ ] Botón "Ver" abre el recibo en un iframe dentro del modal
- [ ] Buscador filtra recibos por número en tiempo real
- [ ] Exportar recibos a Excel funciona

### Notificaciones
- [ ] Tab de notificaciones carga correctamente
- [ ] Botón "Marcar todas leidas" marca las notificaciones
- [ ] Badge del sidebar desaparece después de marcar todas

### Sugerencias
- [ ] Textarea acepta hasta 1000 caracteres
- [ ] Contador de caracteres se actualiza en tiempo real
- [ ] Enviar sugerencia con menos de 10 caracteres muestra error
- [ ] Enviar sugerencia válida → toast de éxito → campo se limpia

### Temas
- [ ] Cambiar a tema Dark → toda la interfaz cambia a fondo oscuro
- [ ] Cambiar a tema Ocean → colores azul/verde
- [ ] Cambiar a tema Sunset → colores cálidos
- [ ] Recargar la página → el tema seleccionado persiste

---

## PROBLEMAS COMUNES Y SOLUCIONES

| Problema | Causa probable | Solución |
|---|---|---|
| El POS muestra grid vacío | No hay platos en el inventario | Ir a Inventario → crear platos primero |
| El selector de mesas está vacío | No hay mesas "Libres" | Ir a Mesas → cambiar estado a "Libre" |
| El botón "Llamar Delivery" muestra advertencia | Repartidor sin teléfono registrado | Editar el repartidor y agregar teléfono |
| WhatsApp no abre al "Llamar Delivery" | El número no tiene el formato correcto | Verificar que sea un número ecuatoriano (09xx) |
| Vista previa del menú está en blanco | No se agregaron platos al menú | Agregar platos en el constructor antes de previsualizar |
| El precio fijo no aparece en el menú | El tipo no es "Almuerzo" ni "Cena" | Solo Almuerzo y Cena tienen precio de combo |
| Stock no cambia después de venta en POS | El plato tiene stock = 0 | No se puede vender un plato agotado desde el POS |
| El carrito no permite subir más cantidad | Se alcanzó el límite del stock disponible | Toast: "No hay más stock de [plato]" |
| Las gráficas no se muestran | No hay datos en el periodo seleccionado | Hacer algunas ventas primero y recargar Reportes |

---

*Guía preparada para My-Negocio — Modelo Restaurante*
*Versión 1.0 — Febrero 2026*
