# GUIA DE TESTING #4 — TIENDA (Punto de Venta)
## Persona: [Nombre del Tester] | Rol: Dueno de Tienda / Negocio Retail

---

## OBJETIVO

Probar TODAS las funciones del sistema de tienda: crear categorias y productos con fotos, procesar ventas completas con carrito, IVA y descuentos, generar recibos, gestionar stock (venta, devolucion, restock, ajuste), catalog digital con 5 estilos, reportes financieros, proveedores, movimientos y configuraciones generales.

---

## ANTES DE EMPEZAR

- El admin ya creo tu negocio con tipo **"tienda"**
- Te dieron un **email** y una **password**
- Accede desde el navegador en el link que te compartieron
- Puedes usar celular o computadora — el sistema es completamente responsivo

---

## FLUJO RECOMENDADO (en este orden)

```
1. Login y revision del dashboard POS
2. Crear CATEGORIAS
3. Crear PRODUCTOS (con fotos)
4. Organizar con Drag & Drop
5. Usar el POS — procesar ventas
6. Operaciones de stock (fuera del POS)
7. Historial de movimientos
8. Catalogo digital
9. Proveedores
10. Movimientos financieros manuales
11. Reportes
12. Recibos
13. Notificaciones y Sugerencias
14. Temas visuales
```

---

## SECCION 1: LOGIN Y PRIMERA VISTA

### 1.1 — Login

1. Ir a la URL del sistema
2. Ingresar el **email** y **password** que te dieron
3. Click en "Iniciar Sesion"
4. Verificar que entras directamente al **tab POS** (la primera pantalla que ves es el punto de venta)

### 1.2 — Dashboard POS — Vista inicial

Al entrar deberia verse lo siguiente en la parte superior, 4 tarjetas de estadisticas:

| Tarjeta | Descripcion | Valor esperado al inicio |
|---|---|---|
| **Ventas Hoy** | Cantidad de ventas del dia | 0 |
| **Ingresos Hoy** | Dinero generado hoy | $0.00 |
| **Productos** | Total de productos en inventario | 0 |
| **Stock Bajo** | Productos por debajo del minimo | 0 |

Estas tarjetas se actualizan en tiempo real cada vez que procesas una venta.

### 1.3 — Sidebar (barra lateral izquierda)

La navegacion tiene exactamente 8 secciones para el tipo "tienda":

| Icono | Nombre | Funcion |
|---|---|---|
| Carrito con tilde | **POS** | Punto de venta y ventas recientes |
| Caja | **Inventario** | Productos, categorias, movimientos de stock |
| Camion | **Proveedores** | Lista de proveedores |
| Libro | **Catalogo** | Catalogo digital publico con 5 estilos |
| Diario | **Movimientos** | Ingresos y gastos manuales |
| Grafico | **Reportes** | Ingresos vs Gastos por periodo |
| Recibo | **Recibos** | Historial de recibos generados |
| Campana | **Notificaciones** | Alertas del sistema |
| Burbuja | **Sugerencias** | Enviar sugerencias al equipo My-Negocio |

> En celular el sidebar se abre con el boton de menu en la esquina superior izquierda.

---

## SECCION 2: CREAR CATEGORIAS

Las categorias organizan los productos en "carpetas" tanto en el inventario como en el POS. Se muestran como pestanas de filtro en ambos lugares.

### Pasos:

1. En el sidebar, hacer click en **"Inventario"**
2. Click en el boton **"Nueva Categoria"** (boton azul con icono de carpeta+)
3. Aparece un cuadro de dialogo pidiendo el nombre
4. Escribir el nombre de la categoria, por ejemplo: `Bebidas`
5. Click en **"Crear"**
6. Repetir para crear al menos **4 categorias** con nombres variados

### Categorias sugeridas para la demo:

- `Bebidas`
- `Lacteos`
- `Limpieza`
- `Snacks`
- `Panaderia`

### Que verificar:

- [ ] Cada categoria nueva aparece como una **carpeta azul** en la grilla de inventario
- [ ] La carpeta muestra el nombre y el conteo de productos (0 al inicio)
- [ ] Existe siempre la carpeta **"Todos"** que no se puede eliminar
- [ ] Si hay productos sin categoria asignada, aparece la carpeta **"Sin Categoria"** automaticamente
- [ ] Al pasar el cursor sobre una carpeta, aparece una **X pequena** en la esquina superior derecha para eliminarla
- [ ] Eliminar una categoria: click en la X, confirmar → los productos quedan en "Sin Categoria" (no se borran)

---

## SECCION 3: CREAR PRODUCTOS

### 3.1 — Formulario de nuevo producto

1. En **Inventario**, click en el boton **"Nuevo Producto"** (boton azul primario)
2. Se abre el modal **"Nuevo Producto"** con los siguientes campos:

| Campo | Descripcion | Ejemplo | Obligatorio |
|---|---|---|---|
| **Nombre del Producto** | Nombre completo del articulo | `Coca-Cola 500ml` | SI |
| **Precio de Venta ($)** | Precio que paga el cliente | `1.50` | SI |
| **Costo de Compra ($)** | Cuanto te costo a ti | `0.80` | SI |
| **Stock Inicial** | Cuantas unidades tienes ahora | `50` | SI |
| **Stock Minimo (alerta)** | Minimo antes de generar alerta | `5` | NO (default: 5) |
| **Categoria** | A que categoria pertenece | `Bebidas` | NO |
| **Foto del Producto** | Imagen JPG/PNG/WebP hasta 2MB | (opcional) | NO |

3. Al ingresar **Precio de Venta** y **Costo de Compra**, el sistema calcula automaticamente el **margen por unidad**:
   - Si precio = $1.50 y costo = $0.80 → muestra "Margen por unidad: **$0.70**" en verde
   - Si el margen es negativo, se muestra en rojo

4. Click en **"Guardar"**

### 3.2 — Subir foto al producto (camara)

Hay dos momentos para subir foto:

**Al crear:** En el modal "Nuevo Producto", campo "Foto del Producto" (acepta JPG, JPEG, PNG, WebP, maximo 2MB). Al seleccionar el archivo, se muestra una miniatura de preview.

**Despues de creado:** En la grilla de inventario, cada tarjeta de producto tiene 4 botones de accion. El boton con **icono de camara** abre un selector de archivo directo para cambiar la foto.

### 3.3 — Crear al menos 8-10 productos variados

Crear productos en diferentes categorias para poder probar todos los filtros del POS:

| Producto | Venta | Costo | Stock | Stock Min | Categoria |
|---|---|---|---|---|---|
| Coca-Cola 500ml | $1.50 | $0.80 | 50 | 5 | Bebidas |
| Agua Mineral 600ml | $0.75 | $0.35 | 100 | 10 | Bebidas |
| Leche Entera 1L | $1.20 | $0.85 | 30 | 5 | Lacteos |
| Queso Fresco 250g | $2.50 | $1.60 | 20 | 3 | Lacteos |
| Detergente Ariel | $3.00 | $1.80 | 40 | 5 | Limpieza |
| Limpiavidrios | $2.20 | $1.30 | 25 | 4 | Limpieza |
| Papas Fritas 50g | $0.60 | $0.30 | 80 | 10 | Snacks |
| Chocolate Kit Kat | $0.90 | $0.50 | 60 | 8 | Snacks |
| **Pan de Yema** | $0.25 | $0.12 | **3** | **5** | Panaderia |

> El ultimo producto (Pan de Yema) tiene stock=3 y minimo=5, lo que activa la alerta de stock bajo.

### 3.4 — Alerta de Stock Bajo

Cuando el stock de un producto es menor o igual al stock minimo:

- [ ] La tarjeta del producto muestra un texto **"Stock bajo"** en rojo con icono de triangulo
- [ ] El numero en la tarjeta de estadisticas **"Stock Bajo"** del dashboard se incrementa
- [ ] La misma alerta aparece en el POS (stock bajo en la cuadricula de productos)

### 3.5 — Drag & Drop para organizar productos

El inventario soporta arrastrar y soltar productos sobre carpetas de categoria.

1. Verificar que la grilla muestra las carpetas arriba y los productos abajo
2. Tomar una tarjeta de producto con el mouse (click y arrastrar)
3. Arrastrarla sobre una carpeta de categoria
4. La carpeta cambia de color a **verde con borde punteado** y hace una animacion de pulso mientras el producto esta encima
5. Soltar el producto sobre la carpeta
6. El producto se mueve a esa categoria
7. Verificar que el conteo de la carpeta actualiza

**Alternativa para celular (sin drag):** Cada tarjeta de producto tiene un boton con icono de **carpeta con flecha** (bi-folder-symlink). Click en ese boton → aparece un selector desplegable con todas las categorias → seleccionar → click "Mover".

### 3.6 — Editar un producto

En la grilla de inventario, cada tarjeta tiene 4 botones en la parte superior (visibles al pasar el cursor):

| Boton | Icono | Accion |
|---|---|---|
| **Editar** | Lapiz (bi-pencil) | Abre el modal para cambiar nombre, precio, costo, stock minimo, categoria |
| **Mover** | Carpeta con flecha (bi-folder-symlink) | Selector de categoria por desplegable |
| **Foto** | Camara (bi-camera) | Sube o reemplaza la imagen del producto |
| **Eliminar** | Basura (bi-trash) | Elimina el producto (baja logica, el historial se mantiene) |

Al **editar**, el campo "Stock Inicial" aparece **bloqueado** (readonly). El stock solo cambia mediante operaciones de inventario (venta, devolucion, restock, ajuste), no manualmente desde el formulario de edicion.

---

## SECCION 4: EL POS — PROCESAR UNA VENTA COMPLETA

### 4.1 — Estructura del POS

El POS tiene dos paneles lado a lado:

**Panel izquierdo — Productos:**
- Titulo "Productos" con barra de busqueda
- Pestanas de categorias (pills) para filtrar
- Cuadricula de tarjetas de productos clickeables

**Panel derecho — Factura:**
- Campo "Nombre del cliente (opcional)"
- Area del carrito con los items seleccionados
- Campos de IVA (%) y Descuento (%)
- Subtotal, IVA, Descuento y TOTAL
- Boton **"Cobrar"** (verde, grande, deshabilitado si el carrito esta vacio)

### 4.2 — Flujo paso a paso de una venta

**Paso 1 — Buscar producto:**
- Escribir en la barra **"Buscar producto..."** (ID: `posBuscar`) → la cuadricula filtra en tiempo real

**Paso 2 — Filtrar por categoria:**
- Click en cualquier pill de categoria (ej: "Bebidas") → solo aparecen productos de esa categoria
- El pill activo se pone azul con sombra
- Click en "Todos" para ver todos de nuevo

**Paso 3 — Agregar al carrito:**
- Click sobre la tarjeta de un producto → aparece en el panel de Factura
- Hacer click varias veces en el mismo producto → la cantidad incrementa
- Si el producto no tiene stock, la tarjeta aparece con opacidad reducida y texto **"Agotado"** (no es clickeable)

**Paso 4 — Ajustar cantidades en el carrito:**
- Boton **"+"** (bi-plus) → incrementa la cantidad del item
- Boton **"-"** (bi-dash) → decrementa (si llega a 0 se quitara el item)
- Si intentas agregar mas que el stock disponible → aparece un mensaje "Stock maximo: N" en naranja (toastr.warning)

**Paso 5 — Eliminar un item del carrito:**
- Click en el boton **"X"** (rojo) al lado del item
- Aparece una confirmacion: "Quitar [nombre] del carrito?" con botones "Si, quitar" / "Cancelar"

**Paso 6 — Nombre del cliente (opcional):**
- Escribir en el campo "Nombre del cliente (opcional)" (ID: `posNombreCliente`)
- Si se deja vacio, el sistema usa "Mostrador" como nombre predeterminado en el recibo

**Paso 7 — Configurar IVA:**
- En el campo **IVA** (ID: `posIva`), escribir el porcentaje (ej: `15`)
- El campo **IVA** al lado derecho se actualiza automaticamente con el monto en dolares

**Paso 8 — Configurar Descuento:**
- En el campo **Descuento** (ID: `posDescuento`), escribir el porcentaje (ej: `10`)
- El campo **Descuento** al lado derecho muestra el monto en rojo con signo menos

**Paso 9 — Verificar calculo de totales:**

La formula es:
```
Subtotal  = suma de (precio × cantidad) por cada item
IVA       = Subtotal × (IVA% / 100)
Descuento = Subtotal × (Descuento% / 100)
TOTAL     = Subtotal + IVA - Descuento
```

Ejemplo con 2 Coca-Colas ($1.50 c/u), IVA 15%, Descuento 10%:
```
Subtotal  = $3.00
IVA 15%   = $0.45
Desc 10%  = -$0.30
TOTAL     = $3.15
```

**Paso 10 — Cobrar:**
- Click en el boton verde **"Cobrar"** (ID: `btnCobrar`)
- Aparece modal de confirmacion SweetAlert2:
  - Muestra el total en grande en verde ($3.15)
  - Muestra la cantidad de productos (ej: "1 producto(s)")
  - Botones: **"Confirmar Pago"** (verde) y **"Cancelar"**
- Click en "Confirmar Pago"
- El boton muestra "Procesando..." con spinner mientras se envia al servidor

**Paso 11 — Resultado exitoso:**
- El carrito se vacia automaticamente
- El campo de nombre del cliente se limpia
- Aparece el **modal del recibo** con el recibo HTML generado
- El recibo tiene numero secuencial (#000001, #000002, etc.)
- Las tarjetas de estadisticas del dashboard se actualizan (Ventas Hoy +1, Ingresos Hoy +$X)
- La tabla "Ventas Recientes" debajo del POS se actualiza

### 4.3 — Acciones disponibles en el recibo post-venta

Cuando aparece el modal del recibo, el footer tiene 4 botones:

| Boton | Icono | Accion |
|---|---|---|
| **WhatsApp** | Verde bi-whatsapp | Pide numero de telefono y envia resumen por WhatsApp |
| **Email** | bi-envelope | Pide correo electronico y envia el recibo HTML completo |
| **Imprimir** | bi-printer | Abre ventana de impresion del navegador con el HTML del recibo |
| **Cerrar** | - | Cierra el modal |

Para enviar por WhatsApp: ingresar el numero sin codigo de pais (ej: `0989799891`), el sistema agrega +593 automaticamente.

### 4.4 — Prueba de stock insuficiente

1. Agregar al carrito un producto con stock = 3
2. Intentar incrementar la cantidad a 4 con el boton "+"
3. Verificar que la cantidad se queda en 3 y aparece "Stock maximo: 3" en naranja
4. Intentar hacer click sobre la tarjeta del producto en la cuadricula cuando ya esta al maximo → mensaje de advertencia

### 4.5 — Ventas Recientes (tabla debajo del POS)

Debajo del POS existe la tabla **"Ventas Recientes"** (ID: `ordenesTable`) con columnas:

| Columna | Descripcion |
|---|---|
| **#** | Numero de orden (badge azul, ej: #1) |
| **Cliente** | Nombre del cliente o "Mostrador" |
| **Items** | Cantidad de lineas de productos distintos |
| **Total** | Monto total en verde |
| **Fecha** | Fecha y hora en formato ecuatoriano |

La tabla se ordena de mas reciente a mas antiguo y muestra 5 registros por pagina.

---

## SECCION 5: OPERACIONES DE STOCK (fuera del POS)

En el tab **"Inventario"**, cada tarjeta de producto tiene dos flechas en la parte inferior:

```
[ FLECHA ARRIBA ]  [ NUMERO DE STOCK ]  [ FLECHA ABAJO ]
```

- **Flecha arriba** (bi-caret-up-fill, azul): Al hacer click abre un dialogo con DOS opciones:
  - **"Devolver Producto"** (amarillo): cliente devuelve producto
  - **"Actualizar Inventario"** (azul): ajuste de conteo fisico o restock

- **Flecha abajo** (bi-caret-down-fill, gris): Venta rapida de 1 unidad (sin pasar por el POS)

### 5.1 — Venta rapida (flecha abajo)

1. Click en la flecha abajo de cualquier producto con stock > 0
2. El numero de stock baja 1 inmediatamente en pantalla (actualizacion optimista)
3. Se registra una venta de 1 unidad en el historial de movimientos
4. Si el stock era 0, aparece "No hay stock disponible" (toastr.warning) y no se ejecuta

### 5.2 — Devolucion (flecha arriba → opcion amarilla)

1. Click en la flecha arriba de un producto
2. Click en **"Devolver Producto"** (boton amarillo con icono bi-arrow-return-left)
3. Aparece un formulario con dos campos:
   - **"Cantidad a devolver"**: numero positivo
   - **"Razon de la devolucion"**: texto OBLIGATORIO (ej: "Cliente cambio de opinion")
4. Click en "Confirmar Devolucion"
5. El stock SUBE la cantidad devuelta
6. Se registra un movimiento de tipo "Devolucion" en el historial

> La razon es obligatoria. Si se deja vacio, el sistema muestra "La razon es obligatoria" y no procede.

### 5.3 — Restock / Ajuste de inventario (flecha arriba → opcion azul)

1. Click en la flecha arriba de un producto
2. Click en **"Actualizar Inventario"** (boton azul con icono bi-pencil-square)
3. Aparece un formulario con:
   - Muestra el **stock actual en sistema** (ej: "Stock en sistema: 50")
   - **"Stock real (contado)"**: el numero que contaste fisicamente
   - **"Nota"**: descripcion del ajuste (OBLIGATORIA)
4. Al escribir el stock real, aparece un indicador de diferencia:
   - Si el real es MAYOR: "+X unidades" en verde
   - Si el real es MENOR: "-X unidades" en rojo (posible perdida)
   - Si es igual: "Sin cambio" (el boton no procede)
5. Click en "Actualizar Stock"
6. El stock se ajusta al numero real ingresado
7. Se registra un movimiento de tipo "Ajuste" (financieramente neutro)

> Esta operacion es para corregir diferencias entre el sistema y el conteo fisico. NO genera ingreso ni gasto en los reportes.

### 5.4 — Restock con costo (desde el endpoint directo)

El sistema soporta registrar un restock con el costo total pagado al proveedor. Esta operacion genera automaticamente un gasto en el registro de movimientos financieros. Se puede hacer desde el boton de ajuste de inventario indicando que se trata de mercaderia nueva con su costo.

---

## SECCION 6: HISTORIAL DE MOVIMIENTOS DE INVENTARIO

### Como acceder:

1. En el tab **"Inventario"**, click en el boton **"Historial"** (con icono bi-clock-history)
2. El texto del boton cambia a **"Ocultar Historial"** y aparece la tabla debajo de los productos
3. Click de nuevo para ocultar

### Estructura de la tabla de historial (`movimientosTable`):

| Columna | Descripcion |
|---|---|
| **Fecha** | Fecha y hora del movimiento |
| **Producto** | Nombre del producto |
| **Tipo** | Tipo de operacion con badge de color |
| **Cant.** | Cantidad de unidades |
| **Total** | Monto en dolares |
| **Stock** | Stock anterior → Stock nuevo (ej: "50 -> 45") |
| **Nota** | Razon o descripcion del movimiento |

### Tipos de movimiento y sus colores:

| Tipo | Badge | Significado |
|---|---|---|
| `Venta` | Verde | Venta directa (flecha abajo en inventario) |
| `Venta POS` | Verde | Venta procesada desde el POS |
| `Devolucion` | Amarillo | Producto devuelto por cliente |
| `Restock` | Azul | Mercaderia nueva recibida |
| `Ajuste` | Rojo | Correccion de conteo fisico |

La tabla se ordena de mas reciente a mas antiguo.

---

## SECCION 7: CATALOGO DIGITAL

El catalogo es una pagina publica que muestra todos los productos del negocio con precios. Se puede compartir por link, WhatsApp o email.

### Como acceder:

Click en **"Catalogo"** en el sidebar.

### 7.1 — Seleccionar estilo del catalogo

Hay 5 pills de estilo en la parte superior:

| Pill | Estilo |
|---|---|
| **Moderno** | Diseno contemporaneo (default) |
| **Elegante** | Estilo premium con tipografia serif |
| **Minimalista** | Limpio, mucho espacio en blanco |
| **Vibrante** | Colores llamativos y energicos |
| **Clasico** | Diseno tradicional |

Al hacer click en un estilo, el iframe de preview abajo se actualiza automaticamente para mostrar como queda el catalogo con ese estilo.

### 7.2 — Vista previa

El iframe de 600px de alto muestra el catalogo exactamente como lo verian los clientes.

### 7.3 — Acciones del catalogo

| Boton | Accion |
|---|---|
| **Descargar PDF** | Abre el catalogo en nueva pestana (para guardar como PDF desde el navegador) |
| **Abrir Link Publico** | Abre el link publico del catalogo en nueva pestana |
| **Enviar por WhatsApp** | Abre modal pidiendo numero de telefono → envia mensaje con el link |
| **Enviar por Email** | Abre modal pidiendo correo electronico → envia el catalogo por email |

### Para enviar por WhatsApp:

1. Click en **"Enviar por WhatsApp"** (boton verde)
2. Aparece modal "Enviar Catalogo"
3. El label dice "Numero de WhatsApp (+593 automatico)"
4. Ingresar el numero (ej: `0989799891`)
5. Click en **"Enviar"**

### Para enviar por Email:

1. Click en **"Enviar por Email"** (boton celeste)
2. Aparece modal "Enviar Catalogo"
3. El label dice "Correo Electronico"
4. Ingresar el email del cliente
5. Click en **"Enviar"**

---

## SECCION 8: PROVEEDORES

Los proveedores son los distribuidores o fabricantes de los que se compran los productos.

### Como acceder:

Click en **"Proveedores"** en el sidebar.

### Crear un proveedor:

1. Click en **"Nuevo Proveedor"** (boton azul primario)
2. Se abre el modal con los siguientes campos:

| Campo | Descripcion | Ejemplo | Obligatorio |
|---|---|---|---|
| **Compania** | Razon social o nombre del proveedor | `Distribuidora ABC` | SI |
| **Contacto** | Nombre de la persona de contacto | `Carlos Mendez` | NO |
| **Telefono** | Numero de telefono | `0991234567` | NO |
| **Email** | Correo del proveedor | `ventas@abc.com` | NO |

3. Click en **"Guardar"**

### Que verificar en la tabla de proveedores:

- [ ] La columna **Telefono** tiene un icono verde de WhatsApp al lado — al hacer click abre WhatsApp Web con ese numero
- [ ] La columna **Email** tiene el link de mailto: para abrir el cliente de correo
- [ ] Botones de edicion y eliminacion al final de cada fila

---

## SECCION 9: MOVIMIENTOS FINANCIEROS MANUALES

Esta seccion sirve para registrar ingresos y gastos que no pasan por el POS (por ejemplo: pago de arriendo, cobro de servicio extra, etc.).

### Como acceder:

Click en **"Movimientos"** en el sidebar.

### Registrar un movimiento:

El formulario esta en la parte superior de la pantalla:

| Campo | Descripcion | Ejemplo |
|---|---|---|
| **Descripcion** | Que fue el movimiento | `Compra de bolsas plasticas` |
| **Monto ($)** | Valor — positivo = ingreso, negativo = gasto | `-25` o `50` |

- Monto positivo (ej: `50`) → se registra como **ingreso**
- Monto negativo (ej: `-25`) → se registra como **gasto**
- Click en **"Registrar"**

Debajo del formulario hay una nota: "Usa valores positivos para ingresos y negativos para gastos"

### Tabla de movimientos:

Muestra todos los registros con columnas: Descripcion, Tipo, Cliente, Monto, Fecha.

Los montos positivos se muestran en verde, los negativos en rojo.

Los registros son **inmutables** (no se pueden editar ni borrar). Hay un mensaje en la esquina que lo indica con un icono de candado.

### Exportar:

Click en el boton **"Exportar"** (verde con icono Excel) → descarga un archivo .xlsx con todos los movimientos.

---

## SECCION 10: REPORTES

### Como acceder:

Click en **"Reportes"** en el sidebar.

### Filtros de periodo:

Tres pills en la parte superior:

| Pill | Periodo |
|---|---|
| **Dia** | Solo el dia de hoy |
| **Semana** | Ultimos 7 dias |
| **Mes** | El mes actual |

### Tarjetas de totales:

Tres tarjetas con degradado de color:

| Tarjeta | Color | Descripcion |
|---|---|---|
| **Ingresos del Dia/Semana/Mes** | Azul | Total de entradas de dinero |
| **Gastos del Dia/Semana/Mes** | Rojo | Total de salidas de dinero |
| **Ganancia Neta** | Verde | Ingresos menos Gastos |

### Graficos:

| Grafico | Tipo | Descripcion |
|---|---|---|
| **Ingresos vs Gastos** | Barras | Comparativa de los ultimos 6 meses |
| **Resumen del Dia/Semana/Mes** | Dona | Proporcion ingresos vs gastos del periodo seleccionado |

El grafico de dona muestra en el centro la **Ganancia Neta** calculada. Si no hay datos para el periodo, muestra un mensaje "Sin datos para este periodo".

---

## SECCION 11: RECIBOS

Los recibos se generan automaticamente con cada venta del POS. Tambien se pueden buscar, ver, exportar y limpiar.

### Como acceder:

Click en **"Recibos"** en el sidebar.

### Buscar un recibo:

En la parte superior hay un campo de busqueda con icono de lupa (ID: `buscarReciboInput`). Escribir el numero del recibo y presionar **Enter** → se abre el modal con ese recibo.

### Tabla de recibos (`tablaRecibos`):

| Columna | Descripcion |
|---|---|
| **#Recibo** | Numero secuencial (badge azul) |
| **Destinatario** | Nombre del cliente |
| **Concepto** | Descripcion de la venta (truncada a 40 caracteres) |
| **Monto** | Total en dolares |
| **Fecha** | Fecha de emision |
| **Acciones** | Boton ojo para ver el HTML completo |

### Ver un recibo:

Click en el boton con icono ojo (bi-eye) → se abre el modal con el HTML del recibo dentro de un iframe. El recibo es el documento HTML profesional exactamente como se envia por email.

### Exportar recibos:

Click en **"Exportar"** (boton verde con icono Excel) → descarga Excel con los recibos del mes actual.

### Banner de limpieza:

Si hay recibos con mas de 30 dias de antiguedad, aparece un banner amarillo de advertencia con el boton **"Exportar y Limpiar"**. Al hacer click:
1. Se descarga el Excel del mes actual
2. Despues de 2 segundos, se eliminan automaticamente los recibos de mas de 30 dias

---

## SECCION 12: NOTIFICACIONES

### Como acceder:

Click en **"Notificaciones"** en el sidebar.

Si hay notificaciones no leidas, aparece un **badge rojo** con el numero sobre el icono de campana en el sidebar.

### Ver notificaciones:

La pantalla muestra todas las notificaciones en tarjetas. Cada una tiene:
- Icono de campana (rellena si no leida, hueca si leida)
- Texto del mensaje
- Fecha
- Boton de tilde para marcar como leida (solo en las no leidas)

### Acciones:

| Accion | Boton |
|---|---|
| Marcar una como leida | Boton con icono de check al lado de cada notificacion |
| Marcar todas como leidas | Boton **"Marcar todas leidas"** en la parte superior |

---

## SECCION 13: SUGERENCIAS

### Como acceder:

Click en **"Sugerencias"** en el sidebar.

### Enviar una sugerencia:

1. Escribir en el area de texto (maximo 1000 caracteres — hay un contador)
2. Click en **"Enviar Sugerencia"** (boton azul con icono bi-send)
3. El sistema muestra confirmacion y limpia el campo

Las sugerencias llegan al equipo de My-Negocio directamente.

---

## SECCION 14: TEMAS VISUALES

My-Negocio tiene 4 temas de color que cambian toda la apariencia del sistema.

### Como cambiar de tema:

En la barra superior (topbar) hay un boton de paleta de colores. Click ahi.

### Los 4 temas disponibles:

| Tema | Descripcion |
|---|---|
| **Claro** (default) | Fondo blanco, estilo corporativo limpio |
| **Oscuro** | Fondo negro, ideal para ambientes con poca luz |
| **Oceano** | Azules y verdes tipo marino |
| **Puesta de Sol** | Tonos calidos, naranjas y purpuras |

El tema se guarda en `localStorage` del navegador, asi que persiste entre sesiones en el mismo dispositivo.

---

## PUNTOS CLAVE PARA LA DEMO A DUENOS DE TIENDA

Estas son las funciones mas impactantes para destacar durante una presentacion:

1. **POS completo desde el celular** — pueden cobrar parados en cualquier punto de la tienda, sin caja fisica

2. **Carrito inteligente** — el sistema impide vender mas del stock disponible, protege contra errores

3. **IVA y descuento por venta** — cada venta puede tener su propio porcentaje, flexible para promociones

4. **Categorias con drag & drop** — organizar el catalogo arrastrando productos a carpetas, muy visual

5. **Catalogo digital publico** — un link que los clientes pueden ver en su celular con precios en tiempo real, sin necesidad de app

6. **Compartir catalogo por WhatsApp** — un boton envia el link a cualquier numero

7. **Recibos profesionales automaticos** — cada venta genera un recibo HTML que se puede enviar por email o WhatsApp en segundos

8. **Alertas de stock bajo** — nunca mas quedarse sin mercaderia sin saberlo

9. **Historial de movimientos inmutable** — cada venta, devolucion y ajuste queda registrado para siempre

10. **Exportar todo a Excel** — inventario, movimientos y recibos para el contador con un click

---

## CHECKLIST FINAL — TODAS LAS FUNCIONES

Marca cada item despues de probarlo:

### Login y navegacion
- [ ] Login con email y password correcto
- [ ] Verificar que la vista inicial es el POS
- [ ] Navegar por todas las 8 secciones del sidebar (POS, Inventario, Proveedores, Catalogo, Movimientos, Reportes, Recibos, Notificaciones, Sugerencias)

### Dashboard POS — Estadisticas
- [ ] Ver las 4 tarjetas: Ventas Hoy, Ingresos Hoy, Productos, Stock Bajo
- [ ] Verificar que Ventas Hoy se incrementa despues de procesar una venta
- [ ] Verificar que Ingresos Hoy se actualiza con el total vendido

### Categorias
- [ ] Crear al menos 4 categorias con nombres distintos
- [ ] Verificar que aparecen como carpetas azules en el inventario
- [ ] Verificar que el conteo de productos en cada carpeta es correcto
- [ ] Eliminar una categoria y verificar que los productos pasan a "Sin Categoria"

### Productos
- [ ] Crear un producto con todos los campos (nombre, precio, costo, stock, minimo, categoria, foto)
- [ ] Verificar que el margen se calcula automaticamente al ingresar precio y costo
- [ ] Crear un producto con stock bajo (stock < minimo) y verificar la alerta
- [ ] Crear al menos 8 productos en distintas categorias
- [ ] Editar un producto (cambiar precio o nombre)
- [ ] Subir foto a un producto desde el boton de camara en la tarjeta
- [ ] Mover un producto a otra categoria con drag & drop
- [ ] Mover un producto a otra categoria con el boton de carpeta (para celular)
- [ ] Eliminar un producto y verificar que desaparece del inventario y del POS

### POS — Procesamiento de ventas
- [ ] Buscar un producto por nombre en la barra de busqueda
- [ ] Filtrar productos por categoria con los pills
- [ ] Agregar un producto al carrito haciendo click en su tarjeta
- [ ] Agregar el mismo producto varias veces y ver como incrementa la cantidad
- [ ] Usar el boton "+" en el carrito para incrementar cantidad
- [ ] Usar el boton "-" en el carrito para decrementar cantidad
- [ ] Eliminar un producto del carrito con el boton X (y confirmar en el dialogo)
- [ ] Ingresar un nombre de cliente opcional
- [ ] Configurar IVA del 15% y verificar el calculo
- [ ] Configurar Descuento del 10% y verificar el calculo
- [ ] Verificar que Subtotal + IVA - Descuento = TOTAL correctamente
- [ ] Procesar una venta completa con el boton "Cobrar"
- [ ] Confirmar el cobro en el dialogo de confirmacion
- [ ] Verificar que el recibo aparece automaticamente al finalizar la venta
- [ ] Intentar agregar mas unidades que el stock disponible — verificar mensaje de advertencia
- [ ] Verificar que los productos agotados aparecen con opacidad y no son clickeables

### Recibo post-venta
- [ ] Ver el recibo HTML generado en el modal
- [ ] Enviar el recibo por email (ingresar email de prueba)
- [ ] Enviar el recibo por WhatsApp (ingresar numero de prueba)
- [ ] Imprimir el recibo
- [ ] Verificar que la tabla "Ventas Recientes" se actualiza con la nueva venta

### Operaciones de stock (fuera del POS)
- [ ] Usar la flecha abajo en una tarjeta de producto (venta rapida de 1 unidad)
- [ ] Verificar que el stock baja 1
- [ ] Usar la flecha arriba → opcion "Devolver Producto"
- [ ] Ingresar cantidad y razon de devolucion (obligatoria)
- [ ] Verificar que el stock sube
- [ ] Usar la flecha arriba → opcion "Actualizar Inventario"
- [ ] Ingresar un stock real diferente al del sistema
- [ ] Verificar el indicador de diferencia (+X o -X)
- [ ] Confirmar el ajuste
- [ ] Verificar que el stock queda en el numero real ingresado

### Historial de movimientos de inventario
- [ ] Click en "Historial" para mostrar la tabla de movimientos
- [ ] Verificar que muestra todos los movimientos registrados (ventas, devoluciones, ajustes)
- [ ] Verificar las columnas: Fecha, Producto, Tipo, Cant., Total, Stock, Nota
- [ ] Verificar que el campo "Stock" muestra el estado anterior y el nuevo (ej: "50 -> 45")
- [ ] Click en "Ocultar Historial" para colapsar la tabla

### Catalogo digital
- [ ] Acceder al tab Catalogo
- [ ] Probar el estilo "Moderno" (default) y ver el preview en el iframe
- [ ] Probar el estilo "Elegante"
- [ ] Probar el estilo "Minimalista"
- [ ] Probar el estilo "Vibrante"
- [ ] Probar el estilo "Clasico"
- [ ] Abrir el link publico del catalogo en nueva pestana
- [ ] Enviar el catalogo por WhatsApp (ingresar numero)
- [ ] Enviar el catalogo por Email (ingresar correo)
- [ ] Descargar el catalogo como PDF

### Proveedores
- [ ] Crear un proveedor con nombre de compania, contacto, telefono y email
- [ ] Verificar que aparece en la tabla
- [ ] Verificar que el telefono tiene icono de WhatsApp clickeable
- [ ] Editar el proveedor
- [ ] Eliminar el proveedor

### Movimientos financieros manuales
- [ ] Registrar un ingreso manual (monto positivo, ej: $50)
- [ ] Registrar un gasto manual (monto negativo, ej: -$25)
- [ ] Verificar que ambos aparecen en la tabla con colores correctos (verde/rojo)
- [ ] Exportar los movimientos a Excel

### Reportes
- [ ] Ver los totales del dia (Ingresos, Gastos, Ganancia Neta)
- [ ] Cambiar al periodo "Semana" y verificar que los numeros cambian
- [ ] Cambiar al periodo "Mes" y verificar
- [ ] Ver el grafico de barras "Ingresos vs Gastos" de los ultimos 6 meses
- [ ] Ver el grafico de dona "Resumen del Dia/Semana/Mes"

### Recibos
- [ ] Acceder al tab Recibos
- [ ] Verificar que aparecen los recibos de las ventas que procesaste
- [ ] Buscar un recibo por numero (escribir en el campo y presionar Enter)
- [ ] Ver el contenido HTML de un recibo haciendo click en el icono ojo
- [ ] Exportar los recibos del mes a Excel

### Notificaciones
- [ ] Ver si hay notificaciones pendientes (badge rojo en el sidebar)
- [ ] Marcar una notificacion individual como leida
- [ ] Marcar todas como leidas

### Sugerencias
- [ ] Escribir una sugerencia de prueba
- [ ] Enviarla y verificar la confirmacion

### Temas visuales
- [ ] Probar el tema Oscuro
- [ ] Probar el tema Oceano
- [ ] Probar el tema Puesta de Sol
- [ ] Volver al tema Claro
- [ ] Verificar que el tema persiste al recargar la pagina

---

## NOTAS PARA EL TESTER

- Todos los mensajes de exito aparecen en la esquina superior derecha como **notificaciones verdes** (toastr)
- Los mensajes de error aparecen en **rojo**
- Las confirmaciones importantes usan el dialogo de **SweetAlert2** (modal centrado con botones de confirmacion/cancelar)
- Si algo no funciona como se espera, anotar: que seccion, que boton, que mensaje de error aparecio
- Probar en celular Y en computadora para verificar la responsividad

---

*Guia preparada para My-Negocio — Sistema de Gestion de Negocios*
*Version del sistema: ASP.NET Core 9.0 | Fecha de testing: 2026-02-20*
