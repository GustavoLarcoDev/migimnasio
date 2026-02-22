// ============================================================
// My-Negocio — Tutorial Interactivo: Restaurante
// Restaurantes, establecimientos de comida con POS, mesas,
// menus, delivery y repartidores
// ============================================================

(function () {
    'use strict';

    // Esperar a que el loader este disponible
    function buildSteps() {
        var t = window._mgTutorial;
        if (!t) return [];

        return [
            // ───────────────────────────────────
            // 1. BIENVENIDA (sin elemento, centrado)
            // ───────────────────────────────────
            {
                popover: {
                    title: t.stepTitle('mortarboard-fill', 'Bienvenido a My-Negocio', 'primary'),
                    description: 'Este tutorial te guiara paso a paso por todas las herramientas de tu panel de restaurante. Aprenderas a gestionar pedidos, mesas, menus, inventario de platos, repartidores y mucho mas. ¡Comencemos!',
                    side: 'over',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 2. SIDEBAR — Menu de navegacion
            // ───────────────────────────────────
            {
                element: '#sidebar',
                popover: {
                    title: t.stepTitle('layout-sidebar', 'Menu de Navegacion', 'primary'),
                    description: 'Este es tu menu principal. Desde aqui puedes acceder a todas las secciones de tu restaurante: POS, Mesas, Menus, Inventario, Distribuidores, Repartidores, Movimientos, Reportes, Recibos, Notificaciones y Sugerencias.',
                    side: 'right',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    t.ensureSidebarVisible();
                }
            },

            // ───────────────────────────────────
            // 3–16. TAB POS (Punto de Venta)
            // ───────────────────────────────────
            {
                element: '#posVentasHoy',
                popover: {
                    title: t.stepTitle('cart-check', 'Ventas del Dia', 'primary'),
                    description: 'Muestra cuantos pedidos has procesado hoy. Esta tarjeta se actualiza en tiempo real cada vez que cobras una orden desde el POS.',
                    side: 'bottom',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    t.closeSidebarMobile();
                    var link = document.querySelector('[data-mg-tab="posTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#posIngresosHoy',
                popover: {
                    title: t.stepTitle('currency-dollar', 'Ingresos del Dia', 'success'),
                    description: 'El total de dinero recaudado hoy por todas tus ventas. Incluye pedidos locales, para llevar y delivery.',
                    side: 'bottom',
                    align: 'center'
                }
            },
            {
                element: '#posTotalProductos',
                popover: {
                    title: t.stepTitle('cup-hot', 'Total de Platos', 'info'),
                    description: 'Cantidad de platos registrados en tu inventario. Cada plato disponible aparece como tarjeta en la grilla del POS para agregarlo rapidamente a los pedidos.',
                    side: 'bottom',
                    align: 'center'
                }
            },
            {
                element: '#posStockBajo',
                popover: {
                    title: t.stepTitle('exclamation-triangle-fill', 'Stock Bajo', 'danger'),
                    description: 'Indica cuantos platos tienen stock por debajo del minimo configurado. Revisa tu inventario para reabastecer a tiempo y no quedarte sin ingredientes.',
                    side: 'bottom',
                    align: 'end'
                }
            },
            {
                element: '#tipoOrdenPills',
                popover: {
                    title: t.stepTitle('signpost-split', 'Tipo de Orden', 'warning'),
                    description: 'Selecciona como se entregara el pedido: Local (comer en el restaurante), Para Llevar (el cliente se lo lleva) o Delivery (envio a domicilio). Cada tipo activa opciones diferentes como mesa o repartidor.',
                    side: 'bottom',
                    align: 'center'
                }
            },
            {
                element: '#mesaSelector',
                popover: {
                    title: t.stepTitle('grid-3x3', 'Selector de Mesa', 'primary'),
                    description: 'Cuando el tipo de orden es "Local", asigna una mesa al pedido. Las mesas disponibles aparecen en el desplegable. Al asignar una mesa, su estado cambia a "Ocupada".',
                    side: 'bottom',
                    align: 'center'
                }
            },
            {
                element: '#mesaActivaIndicator',
                popover: {
                    title: t.stepTitle('geo-alt-fill', 'Mesa Activa', 'danger'),
                    description: 'Cuando seleccionas una mesa ocupada desde el tab Mesas, este indicador rojo muestra cual mesa estas atendiendo. Puedes cerrarla con la X para volver al modo normal.',
                    side: 'bottom',
                    align: 'center'
                }
            },
            {
                element: '#posBuscar',
                popover: {
                    title: t.stepTitle('search', 'Buscar Platos', 'info'),
                    description: 'Escribe el nombre de un plato para encontrarlo rapidamente en la grilla. Ideal cuando tienes muchos platos en el menu y necesitas agilizar la toma de pedidos.',
                    side: 'bottom',
                    align: 'center'
                }
            },
            {
                element: '#posCategoryTabs',
                popover: {
                    title: t.stepTitle('filter-circle', 'Categorias de Platos', 'primary'),
                    description: 'Filtra los platos por categoria: Entradas, Platos Fuertes, Bebidas, Postres, etc. Haz clic en "Todos" para ver la grilla completa. Las categorias se crean desde el tab Inventario.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#posProductsGrid',
                popover: {
                    title: t.stepTitle('grid', 'Grilla de Platos', 'success'),
                    description: 'Haz clic en cualquier plato para agregarlo al pedido actual. Cada tarjeta muestra la foto, nombre, precio y stock disponible. Los platos agotados aparecen deshabilitados.',
                    side: 'left',
                    align: 'start'
                }
            },
            {
                element: '#posCartItems',
                popover: {
                    title: t.stepTitle('receipt', 'Recibo del Pedido', 'primary'),
                    description: 'Aqui aparecen los platos agregados al pedido actual. Puedes modificar la cantidad con los botones + y -, o eliminar un item. El subtotal se calcula automaticamente.',
                    side: 'left',
                    align: 'start'
                }
            },
            {
                element: '#deliveryFields',
                popover: {
                    title: t.stepTitle('bicycle', 'Datos de Delivery', 'info'),
                    description: 'Estos campos aparecen cuando seleccionas "Delivery" como tipo de orden. Asigna un repartidor y escribe la direccion de entrega del cliente.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#posTotal',
                popover: {
                    title: t.stepTitle('cash-stack', 'Total del Pedido', 'success'),
                    description: 'El monto final a cobrar. Se calcula con el subtotal, mas el IVA configurado, menos el porcentaje de descuento. Puedes ajustar IVA y descuento justo arriba de este total.',
                    side: 'top',
                    align: 'end'
                }
            },
            {
                element: '#btnCobrar',
                popover: {
                    title: t.stepTitle('cash-coin', 'Cobrar Pedido', 'success'),
                    description: 'Cuando el pedido esta listo, haz clic aqui para cobrar. Se genera un recibo automaticamente, se actualiza el stock de los platos y la venta queda registrada en tus reportes.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#btnLlamarDelivery',
                popover: {
                    title: t.stepTitle('whatsapp', 'Llamar Delivery por WhatsApp', 'success'),
                    description: 'Envia un mensaje de WhatsApp al repartidor asignado con los detalles del pedido y la direccion de entrega. Aparece solo cuando el tipo de orden es "Delivery" y hay un repartidor seleccionado.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#btnCancelarOrden',
                popover: {
                    title: t.stepTitle('x-circle', 'Cancelar Orden', 'danger'),
                    description: 'Cancela el pedido actual de la mesa seleccionada. La mesa vuelve a estado "Libre" y los items del carrito se limpian. Util si el cliente cancela antes de pagar.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#ordenesTable',
                popover: {
                    title: t.stepTitle('clock-history', 'Ventas Recientes', 'info'),
                    description: 'Tabla con todas las ventas del dia. Muestra el numero de orden, cliente, tipo (Local, Para Llevar, Delivery), items vendidos, total y fecha. Util para revisar el historial rapidamente.',
                    side: 'top',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 17–19. TAB MESAS
            // ───────────────────────────────────
            {
                element: '#btnNuevaMesa',
                popover: {
                    title: t.stepTitle('plus-circle', 'Crear Nueva Mesa', 'success'),
                    description: 'Agrega mesas a tu restaurante indicando nombre (ej: "Mesa 1", "Terraza 2"), numero y capacidad de personas. Puedes crear tantas mesas como necesites.',
                    side: 'bottom',
                    align: 'end'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="mesasTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#mesasGrid',
                popover: {
                    title: t.stepTitle('grid-3x3', 'Distribucion de Mesas', 'primary'),
                    description: 'Vista visual de todas tus mesas con colores de estado: verde (libre), rojo (ocupada) y amarillo (reservada). Haz clic en una mesa ocupada para ver su pedido y editarlo en el POS.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                popover: {
                    title: t.stepTitle('arrow-left-right', 'Flujo Mesa-POS', 'info'),
                    description: 'El sistema conecta Mesas con el POS: al crear un pedido "Local" con mesa asignada, la mesa se marca como ocupada. Al cobrar, vuelve a "Libre". Desde el tab Mesas puedes hacer clic en una mesa ocupada para retomar su pedido en el POS.',
                    side: 'over',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 20–27. TAB MENUS
            // ───────────────────────────────────
            {
                element: '#btnNuevoMenu',
                popover: {
                    title: t.stepTitle('plus-circle', 'Crear Nuevo Menu', 'success'),
                    description: 'Abre el constructor de menus para disenar una carta personalizada. Podras agregar secciones, arrastrar platos, elegir un estilo visual y generar un menu listo para imprimir.',
                    side: 'bottom',
                    align: 'end'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="menusTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#menuBuilderSection',
                popover: {
                    title: t.stepTitle('tools', 'Constructor de Menu', 'primary'),
                    description: 'Este es el area de construccion de tu menu. A la izquierda estan los platos disponibles y a la derecha el constructor donde organizas las secciones y contenido de tu carta.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#menuPlatosDisponibles',
                popover: {
                    title: t.stepTitle('cup-hot', 'Platos Disponibles', 'info'),
                    description: 'Lista de todos tus platos registrados en el inventario. Arrastra los platos desde aqui hacia las secciones del constructor para agregarlos a tu menu. Usa el buscador para filtrar.',
                    side: 'right',
                    align: 'start'
                }
            },
            {
                element: '#menuNombre',
                popover: {
                    title: t.stepTitle('input-cursor-text', 'Nombre del Menu', 'primary'),
                    description: 'Dale un nombre descriptivo a tu menu, por ejemplo: "Menu del Dia", "Carta Principal" o "Menu Ejecutivo". Este nombre aparecera como titulo en la vista previa.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#menuTipoPills',
                popover: {
                    title: t.stepTitle('clock', 'Tipo de Menu', 'warning'),
                    description: 'Clasifica tu menu por momento del dia: General (todo el dia), Desayuno, Almuerzo o Cena. Esto te permite tener diferentes cartas segun el horario de atencion.',
                    side: 'bottom',
                    align: 'center'
                }
            },
            {
                element: '#btnGuardarMenu',
                popover: {
                    title: t.stepTitle('save', 'Guardar y Previsualizar', 'success'),
                    description: 'Guarda tu menu una vez que hayas agregado las secciones y platos. Tambien puedes usar "Vista Previa" para ver como se vera la carta impresa, y "Cancelar" para descartar los cambios.',
                    side: 'top',
                    align: 'start'
                }
            },
            {
                element: '.menuEstiloBtn',
                popover: {
                    title: t.stepTitle('palette', 'Estilo Visual del Menu', 'info'),
                    description: 'Elige entre 5 estilos para tu carta: Moderno, Elegante, Minimalista, Vibrante y Clasico. Cada estilo cambia los colores, tipografia y diseno de la vista previa imprimible.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#menusTable',
                popover: {
                    title: t.stepTitle('table', 'Menus Guardados', 'primary'),
                    description: 'Tabla con todos los menus que has creado. Puedes ver el nombre, tipo, precio, estilo y fecha. Desde las acciones puedes previsualizar, imprimir, editar o eliminar cada menu.',
                    side: 'top',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 28–31. TAB INVENTARIO (Platos)
            // ───────────────────────────────────
            {
                element: '#btnCrearProducto',
                popover: {
                    title: t.stepTitle('plus-circle', 'Nuevo Plato', 'success'),
                    description: 'Registra un nuevo plato en tu inventario. Ingresa nombre, precio de venta, costo, stock, categoria y opcionalmente una foto y receta/ingredientes.',
                    side: 'bottom',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="inventarioTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#btnCrearCategoria',
                popover: {
                    title: t.stepTitle('folder-plus', 'Nueva Categoria', 'primary'),
                    description: 'Crea categorias para organizar tus platos: Entradas, Sopas, Platos Fuertes, Bebidas, Postres, etc. Puedes arrastrar platos sobre las carpetas para moverlos de categoria.',
                    side: 'bottom',
                    align: 'center'
                }
            },
            {
                element: '#productosGrid',
                popover: {
                    title: t.stepTitle('grid-3x3-gap', 'Tarjetas de Platos', 'primary'),
                    description: 'Tus platos aparecen como tarjetas con foto, nombre, precio y stock. Usa los botones de cada tarjeta para vender, devolver, reabastecer, editar o eliminar. El stock se actualiza en tiempo real.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#btnToggleMovimientos',
                popover: {
                    title: t.stepTitle('clock-history', 'Historial de Inventario', 'info'),
                    description: 'Muestra u oculta el historial de movimientos del inventario: ventas, devoluciones, restock y ajustes. Cada operacion queda registrada para auditoria y control de ingredientes.',
                    side: 'bottom',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 32–33. TAB DISTRIBUIDORES
            // ───────────────────────────────────
            {
                element: '#btnNuevoProveedor',
                popover: {
                    title: t.stepTitle('plus-circle', 'Nuevo Distribuidor', 'success'),
                    description: 'Registra a tus proveedores de ingredientes y productos. Ingresa el nombre de la compania, contacto, telefono y email. Lleva un control de a quien le compras.',
                    side: 'bottom',
                    align: 'end'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="proveedoresTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#proveedoresTable',
                popover: {
                    title: t.stepTitle('table', 'Tabla de Distribuidores', 'primary'),
                    description: 'Lista de todos tus distribuidores con compania, contacto, telefono y email. Usa los botones de acciones para editar o eliminar. Tener tus proveedores organizados agiliza las compras.',
                    side: 'top',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 34–35. TAB REPARTIDORES
            // ───────────────────────────────────
            {
                element: '#btnNuevoRepartidor',
                popover: {
                    title: t.stepTitle('plus-circle', 'Nuevo Repartidor', 'success'),
                    description: 'Agrega repartidores a tu equipo de delivery. Registra nombre, apellido, telefono, email y tipo de vehiculo (Moto, Bicicleta, Auto o A pie).',
                    side: 'bottom',
                    align: 'end'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="repartidoresTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#repartidoresTable',
                popover: {
                    title: t.stepTitle('bicycle', 'Tabla de Repartidores', 'primary'),
                    description: 'Lista de tus repartidores con nombre, telefono, email y vehiculo. Los repartidores registrados aqui aparecen en el selector de delivery del POS para asignarlos a los pedidos.',
                    side: 'top',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 36–39. TAB MOVIMIENTOS (LOGS)
            // ───────────────────────────────────
            {
                element: '#logDescripcion',
                popover: {
                    title: t.stepTitle('journal-text', 'Registrar Movimiento', 'primary'),
                    description: 'Ingresa la descripcion de tu movimiento financiero. Puede ser un ingreso (como venta de catering) o un gasto (como compra de ingredientes, pago de servicios).',
                    side: 'bottom',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="logsTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#btnRegistrarLog',
                popover: {
                    title: t.stepTitle('check-lg', 'Guardar Movimiento', 'success'),
                    description: 'Despues de llenar la descripcion y el monto, haz clic aqui para registrar. Usa valores positivos para ingresos y negativos para gastos. Los registros son inmutables para proteger tus datos.',
                    side: 'left',
                    align: 'center'
                }
            },
            {
                element: '#logsTable',
                popover: {
                    title: t.stepTitle('list-ul', 'Historial de Movimientos', 'info'),
                    description: 'Todos tus movimientos registrados aparecen aqui: ingresos, gastos y las ventas del POS que se generan automaticamente. Puedes buscar y ordenar por cualquier columna.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#btnExportLogsExcel',
                popover: {
                    title: t.stepTitle('file-earmark-excel', 'Exportar Movimientos', 'success'),
                    description: 'Exporta todos tus movimientos a Excel para respaldo o analisis contable. Si tienes registros con mas de 30 dias, podras cerrar el periodo descargando y limpiando.',
                    side: 'bottom',
                    align: 'end'
                }
            },

            // ───────────────────────────────────
            // 40–44. TAB REPORTES (VENTAS)
            // ───────────────────────────────────
            {
                element: '#ventasPeriodPills',
                popover: {
                    title: t.stepTitle('funnel', 'Filtro de Reportes', 'info'),
                    description: 'Cambia entre Dia, Semana o Mes para ver los reportes del periodo que te interese. Las tarjetas de ingresos, gastos y ganancias se actualizan automaticamente.',
                    side: 'bottom',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="ventasTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#ventasIngresos',
                popover: {
                    title: t.stepTitle('cash-stack', 'Resumen Financiero', 'success'),
                    description: 'Tarjetas con degradado que muestran ingresos, gastos y ganancia neta del periodo seleccionado. Te dan una vision rapida de la salud financiera de tu restaurante.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#chartBarVentas',
                popover: {
                    title: t.stepTitle('bar-chart-fill', 'Grafico de Ingresos vs Gastos', 'primary'),
                    description: 'Grafico de barras interactivo que compara tus ingresos contra tus gastos. Pasa el cursor sobre las barras para ver los montos detallados de cada periodo.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#chartVentasCategoria',
                popover: {
                    title: t.stepTitle('pie-chart-fill', 'Ventas por Categoria', 'info'),
                    description: 'Grafico y tabla que desglosan las ventas por categoria de platos. Descubre cuales son tus categorias mas rentables: Entradas, Platos Fuertes, Bebidas, Postres, etc.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#chartResumen',
                popover: {
                    title: t.stepTitle('pie-chart', 'Resumen General', 'primary'),
                    description: 'Grafico de dona que muestra la proporcion entre ingresos y gastos, con la ganancia neta en el centro. Una forma visual e intuitiva de ver la rentabilidad de tu restaurante.',
                    side: 'top',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 45–46. TAB RECIBOS
            // ───────────────────────────────────
            {
                element: '#tablaRecibos',
                popover: {
                    title: t.stepTitle('receipt', 'Tabla de Recibos', 'primary'),
                    description: 'Aqui se guardan automaticamente los recibos de cada venta del POS. Puedes ver el detalle de cada recibo haciendo clic en "Ver" y tambien buscar por numero de recibo.',
                    side: 'top',
                    align: 'center'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="recibosTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#btnExportRecibos',
                popover: {
                    title: t.stepTitle('file-earmark-excel', 'Exportar Recibos', 'success'),
                    description: 'Exporta todos tus recibos a Excel. Perfecto para llevar un respaldo de las transacciones de tu restaurante o enviarlas al contador.',
                    side: 'bottom',
                    align: 'end'
                }
            },

            // ───────────────────────────────────
            // 47–48. TAB NOTIFICACIONES
            // ───────────────────────────────────
            {
                element: '#notificacionesList',
                popover: {
                    title: t.stepTitle('bell-fill', 'Lista de Notificaciones', 'warning'),
                    description: 'Todas tus notificaciones aparecen aqui. El sistema te avisa sobre stock bajo, pedidos pendientes y otros eventos importantes. Las no leidas se destacan con un borde azul.',
                    side: 'top',
                    align: 'center'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="notificacionesTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#btnMarcarTodasLeidasTab',
                popover: {
                    title: t.stepTitle('check-all', 'Marcar Todas como Leidas', 'info'),
                    description: 'Marca todas las notificaciones como leidas de un solo clic. Util cuando ya revisaste los avisos pendientes y quieres limpiar la lista.',
                    side: 'bottom',
                    align: 'end'
                }
            },

            // ───────────────────────────────────
            // 49–50. TAB SUGERENCIAS
            // ───────────────────────────────────
            {
                element: '#sugerenciaTexto',
                popover: {
                    title: t.stepTitle('chat-left-text', 'Enviar Sugerencia', 'info'),
                    description: 'Escribe aqui tus ideas, sugerencias o comentarios para el equipo de My-Negocio. Tu opinion nos ayuda a mejorar la plataforma. Maximo 1000 caracteres.',
                    side: 'top',
                    align: 'center'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="sugerenciasTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#btnEnviarSugerencia',
                popover: {
                    title: t.stepTitle('send', 'Enviar al Equipo', 'success'),
                    description: 'Cuando termines de escribir tu sugerencia, haz clic aqui para enviarla. El equipo de desarrollo la recibira y la tomara en cuenta para futuras mejoras.',
                    side: 'top',
                    align: 'start'
                }
            },

            // ───────────────────────────────────
            // 51–53. TOPBAR — Elementos superiores
            // ───────────────────────────────────
            {
                element: '#bellBtn',
                popover: {
                    title: t.stepTitle('bell', 'Campana de Notificaciones', 'warning'),
                    description: 'Acceso rapido a tus notificaciones desde cualquier pestana. El contador rojo indica cuantas notificaciones sin leer tienes. Haz clic para ver un resumen sin salir de donde estes.',
                    side: 'bottom',
                    align: 'end'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="posTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '.mg-theme-btn',
                popover: {
                    title: t.stepTitle('palette', 'Cambiar Tema Visual', 'info'),
                    description: 'Personaliza la apariencia de tu panel. Elige entre 4 temas: Claro, Oscuro, Oceano y Atardecer. Tu seleccion se guarda automaticamente para tu proxima visita.',
                    side: 'bottom',
                    align: 'end'
                }
            },
            {
                element: '#btnTutorial',
                popover: {
                    title: t.stepTitle('mortarboard', 'Repetir Tutorial', 'primary'),
                    description: 'Si alguna vez necesitas recordar como funciona algo, haz clic en este boton para ejecutar este tutorial guiado nuevamente en cualquier momento.',
                    side: 'bottom',
                    align: 'end'
                }
            },

            // ───────────────────────────────────
            // 54. DESPEDIDA (sin elemento, centrado)
            // ───────────────────────────────────
            {
                popover: {
                    title: t.stepTitle('trophy-fill', '¡Tutorial Completado!', 'success'),
                    description: '¡Felicidades! Ya conoces todas las herramientas de tu panel de restaurante. Gestiona tus pedidos, organiza tus mesas, diseña tus menus y controla las finanzas de tu negocio con My-Negocio. Si tienes dudas, repite este tutorial cuando quieras.',
                    side: 'over',
                    align: 'center'
                }
            }
        ];
    }

    // Registrar los pasos cuando el DOM este listo
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            window._mgTutorialRestaurante = buildSteps();
        });
    } else {
        window._mgTutorialRestaurante = buildSteps();
    }
})();
