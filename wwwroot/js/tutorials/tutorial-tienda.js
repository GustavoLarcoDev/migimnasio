// ============================================================
// My-Negocio — Tutorial Interactivo: Tienda / Retail
// Tiendas de ropa, productos, abarrotes, con sistema POS
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
                    description: 'Este tutorial te guiara paso a paso por todas las herramientas de tu panel de tienda. Aprenderas a usar el Punto de Venta (POS), gestionar inventario, proveedores, catalogo online y mucho mas. ¡Comencemos!',
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
                    description: 'Este es tu menu principal. Desde aqui accedes a todas las secciones: POS, Inventario, Proveedores, Catalogo, Movimientos, Reportes, Recibos, Metodos de Pago, Notificaciones, Perfil y Sugerencias.',
                    side: 'right',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    t.ensureSidebarVisible();
                }
            },

            // ═══════════════════════════════════
            // 3–13. TAB POS (Punto de Venta)
            // ═══════════════════════════════════
            {
                element: '#posVentasHoy',
                popover: {
                    title: t.stepTitle('cart-check', 'Ventas de Hoy', 'primary'),
                    description: 'Muestra cuantas ventas has realizado hoy desde el POS. Este contador se actualiza en tiempo real con cada venta que completes.',
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
                    title: t.stepTitle('currency-dollar', 'Ingresos de Hoy', 'success'),
                    description: 'El total de dinero generado por las ventas del dia. Junto con las tarjetas de Productos y Stock Bajo, te dan una vision rapida del estado de tu tienda.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#posStockBajo',
                popover: {
                    title: t.stepTitle('exclamation-triangle-fill', 'Alerta de Stock Bajo', 'danger'),
                    description: 'Indica cuantos productos tienen stock por debajo del minimo. Revisalo frecuentemente para reabastecer a tiempo y no perder ventas.',
                    side: 'bottom',
                    align: 'end'
                }
            },
            {
                element: '#posBuscar',
                popover: {
                    title: t.stepTitle('search', 'Buscar Productos', 'info'),
                    description: 'Escribe el nombre de un producto para encontrarlo rapidamente en la grilla. Ideal cuando tienes muchos productos y necesitas agilizar el cobro.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#posCategoryTabs',
                popover: {
                    title: t.stepTitle('tags', 'Filtro por Categorias', 'info'),
                    description: 'Filtra los productos por categoria para encontrarlos mas rapido. Las categorias se crean desde la pestana de Inventario y aparecen aqui automaticamente.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#posProductsGrid',
                popover: {
                    title: t.stepTitle('grid-3x3-gap', 'Grilla de Productos', 'primary'),
                    description: 'Todos tus productos aparecen aqui como tarjetas con imagen, nombre, precio y stock. Haz clic en cualquier producto para agregarlo al carrito de la venta actual.',
                    side: 'left',
                    align: 'start'
                }
            },
            {
                element: '#posCartItems',
                popover: {
                    title: t.stepTitle('receipt', 'Carrito / Recibo', 'warning'),
                    description: 'Aqui aparecen los productos que vas agregando a la venta. Puedes ajustar cantidades con los botones + y -, o eliminar un producto del carrito.',
                    side: 'left',
                    align: 'start'
                }
            },
            {
                element: '#posNombreCliente',
                popover: {
                    title: t.stepTitle('person', 'Nombre del Cliente', 'info'),
                    description: 'Campo opcional para identificar al cliente en el recibo. Util para llevar registro de quien compro, especialmente para clientes frecuentes.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#btnAgregarCargoExtra',
                popover: {
                    title: t.stepTitle('plus-square', 'Cargos Extra', 'warning'),
                    description: 'Agrega cargos adicionales a la venta actual: envio, empaque, servicio especial, etc. Cada cargo tiene descripcion y monto, y se suma al total del recibo. Puedes agregar multiples cargos y eliminarlos individualmente.',
                    side: 'left',
                    align: 'center'
                }
            },
            {
                element: '#posCargosExtraContainer',
                popover: {
                    title: t.stepTitle('list-check', 'Lista de Cargos Extra', 'info'),
                    description: 'Aqui aparecen los cargos extra que has agregado a la venta. Cada uno muestra su descripcion, monto y un boton para eliminarlo. El subtotal de cargos se muestra al final de la lista.',
                    side: 'left',
                    align: 'center'
                }
            },
            {
                element: '#posIva',
                popover: {
                    title: t.stepTitle('percent', 'IVA y Descuento', 'warning'),
                    description: 'Ajusta el porcentaje de IVA y descuento para esta venta. El IVA se suma al subtotal y el descuento se resta. Ambos se reflejan en el total final automaticamente.',
                    side: 'left',
                    align: 'center'
                }
            },
            {
                element: '#posTotal',
                popover: {
                    title: t.stepTitle('cash-stack', 'Total de la Venta', 'success'),
                    description: 'El monto total a cobrar incluyendo IVA y descuentos aplicados. Este es el valor final que el cliente debe pagar.',
                    side: 'left',
                    align: 'center'
                }
            },
            {
                element: '#btnCobrar',
                popover: {
                    title: t.stepTitle('cash-coin', 'Boton Cobrar', 'success'),
                    description: 'Presiona para completar la venta. Se generara un recibo automatico, se descontara el stock y se registrara el ingreso. ¡El corazon de tu POS!',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#ordenesTable',
                popover: {
                    title: t.stepTitle('clock-history', 'Ventas Recientes', 'primary'),
                    description: 'Historial de las ventas realizadas hoy. Muestra el numero de orden, cliente, cantidad de items, total y fecha. Puedes consultar rapidamente las ultimas transacciones.',
                    side: 'top',
                    align: 'center'
                }
            },

            // ═══════════════════════════════════
            // 14–19. TAB INVENTARIO
            // ═══════════════════════════════════
            {
                element: '#btnCrearProducto',
                popover: {
                    title: t.stepTitle('plus-circle', 'Nuevo Producto', 'success'),
                    description: 'Agrega productos a tu inventario con nombre, imagen, precio de venta, costo de compra, stock inicial y categoria. El sistema calcula tu margen de ganancia automaticamente.',
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
                    description: 'Crea categorias para organizar tus productos (Ej: Bebidas, Ropa, Electrónica). Las categorias aparecen como carpetas y tambien como filtros en el POS.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#invCategoryFolders',
                popover: {
                    title: t.stepTitle('folder-fill', 'Carpetas de Categorias', 'info'),
                    description: 'Tus categorias se muestran como carpetas. Haz clic en una para ver solo sus productos. Puedes arrastrar productos sobre las carpetas para organizarlos rapidamente.',
                    side: 'bottom',
                    align: 'center'
                }
            },
            {
                element: '#productosGrid',
                popover: {
                    title: t.stepTitle('grid-3x3-gap', 'Tarjetas de Productos', 'primary'),
                    description: 'Cada producto muestra su imagen, nombre, precio y stock. Desde aqui puedes vender, devolver, reabastecer o ajustar stock con los botones de cada tarjeta. Tambien puedes editar o eliminar productos.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#btnToggleMovimientos',
                popover: {
                    title: t.stepTitle('clock-history', 'Historial de Movimientos', 'info'),
                    description: 'Muestra u oculta la tabla de movimientos del inventario: ventas, devoluciones, restock y ajustes. Todo queda registrado con fecha, cantidad y nota para auditoria.',
                    side: 'bottom',
                    align: 'center'
                }
            },
            {
                element: '#btnExportInventarioExcel',
                popover: {
                    title: t.stepTitle('file-earmark-excel', 'Exportar Inventario', 'success'),
                    description: 'Descarga tu inventario completo en formato Excel con todos los productos, precios, costos, stock y categorias. Perfecto para respaldos o analisis externo.',
                    side: 'bottom',
                    align: 'center'
                }
            },

            // ═══════════════════════════════════
            // 20–21. TAB PROVEEDORES
            // ═══════════════════════════════════
            {
                element: '#btnNuevoProveedor',
                popover: {
                    title: t.stepTitle('truck', 'Nuevo Proveedor', 'success'),
                    description: 'Registra a tus proveedores con nombre de compania, persona de contacto, telefono y email. Manten organizada tu cadena de suministro.',
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
                    title: t.stepTitle('table', 'Tabla de Proveedores', 'primary'),
                    description: 'Lista de todos tus proveedores con sus datos de contacto. Puedes editar o eliminar cada proveedor desde los botones de accion.',
                    side: 'top',
                    align: 'center'
                }
            },

            // ═══════════════════════════════════
            // 22–26. TAB CATALOGO
            // ═══════════════════════════════════
            {
                element: '.catEstiloBtn',
                popover: {
                    title: t.stepTitle('palette', 'Estilo del Catalogo', 'info'),
                    description: 'Elige entre 5 estilos visuales para tu catalogo: Moderno, Elegante, Minimalista, Vibrante y Clasico. La vista previa se actualiza al instante para que veas como queda.',
                    side: 'bottom',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="catalogoTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#catPreviewIframe',
                popover: {
                    title: t.stepTitle('eye', 'Vista Previa del Catalogo', 'primary'),
                    description: 'Aqui ves en tiempo real como luce tu catalogo online con los productos de tu inventario. Cambia el estilo arriba y la vista se actualiza automaticamente.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#catDescargarPdf',
                popover: {
                    title: t.stepTitle('download', 'Descargar PDF', 'primary'),
                    description: 'Descarga tu catalogo como archivo PDF listo para imprimir o enviar. Incluye todos tus productos con imagenes, precios y categorias.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#catAbrirLink',
                popover: {
                    title: t.stepTitle('link-45deg', 'Link Publico', 'info'),
                    description: 'Abre tu catalogo online en una nueva pestana. Este enlace es publico y puedes compartirlo con tus clientes para que vean tus productos desde cualquier dispositivo.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#btnEnviarCatWhatsapp',
                popover: {
                    title: t.stepTitle('whatsapp', 'Compartir Catalogo', 'success'),
                    description: 'Envia tu catalogo directamente por WhatsApp o Email a tus clientes. Una forma rapida de promocionar tus productos y generar ventas desde cualquier lugar.',
                    side: 'bottom',
                    align: 'start'
                }
            },

            // ═══════════════════════════════════
            // 27–29. TAB MOVIMIENTOS (LOGS)
            // ═══════════════════════════════════
            {
                element: '#logDescripcion',
                popover: {
                    title: t.stepTitle('journal-text', 'Registrar Movimiento', 'primary'),
                    description: 'Ingresa movimientos financieros manuales como compra de mercaderia, pago de alquiler, etc. Los ingresos por ventas POS se registran automaticamente.',
                    side: 'bottom',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="logsTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#logsTable',
                popover: {
                    title: t.stepTitle('list-ul', 'Historial de Movimientos', 'info'),
                    description: 'Todos tus movimientos financieros: manuales y automaticos (ventas POS, devoluciones). Muestra descripcion, tipo, monto y fecha. Los registros son inmutables para proteger tu contabilidad.',
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

            // ═══════════════════════════════════
            // 30–34. TAB REPORTES (VENTAS)
            // ═══════════════════════════════════
            {
                element: '#ventasPeriodPills',
                popover: {
                    title: t.stepTitle('funnel', 'Filtro de Periodo', 'info'),
                    description: 'Cambia entre Dia, Semana o Mes para ver los reportes del periodo que te interese. Todas las tarjetas y graficos se actualizan automaticamente.',
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
                    title: t.stepTitle('arrow-up-circle', 'Tarjetas Financieras', 'success'),
                    description: 'Tres tarjetas con degradado muestran Ingresos, Gastos y Ganancia Neta del periodo seleccionado. Una vision rapida de la salud financiera de tu tienda.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#chartVentasCategoria',
                popover: {
                    title: t.stepTitle('pie-chart-fill', 'Ventas por Categoria', 'info'),
                    description: 'Grafico circular que muestra que categorias de productos generan mas ventas. Identifica tus lineas mas rentables y enfoca tu inventario en lo que mas se vende.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#chartProductosMasVendidos',
                popover: {
                    title: t.stepTitle('trophy', 'Productos Mas Vendidos', 'warning'),
                    description: 'Ranking de tus productos estrella. Conoce cuales son los mas vendidos para asegurarte de nunca quedarte sin stock de tus mejores articulos.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#chartBarVentas',
                popover: {
                    title: t.stepTitle('bar-chart-fill', 'Ingresos vs Gastos', 'primary'),
                    description: 'Grafico de barras comparando ingresos y gastos en el periodo. Pasa el cursor sobre las barras para ver los montos exactos de cada concepto.',
                    side: 'top',
                    align: 'center'
                }
            },

            // ═══════════════════════════════════
            // 35–36. TAB RECIBOS
            // ═══════════════════════════════════
            {
                element: '#tablaRecibos',
                popover: {
                    title: t.stepTitle('receipt', 'Tabla de Recibos', 'primary'),
                    description: 'Todos los recibos generados automaticamente por cada venta POS y movimiento. Los recibos incluyen el logo de tu negocio si lo tienes configurado, los cargos extra y el metodo de pago utilizado. Haz clic en "Ver" para ver el detalle completo.',
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
                    description: 'Exporta todos tus recibos a Excel. Usa el campo de busqueda para encontrar un recibo especifico por su numero.',
                    side: 'bottom',
                    align: 'end'
                }
            },

            // ═══════════════════════════════════
            // TAB METODOS DE PAGO
            // ═══════════════════════════════════
            {
                element: '#metodosPagoStats',
                popover: {
                    title: t.stepTitle('credit-card', 'Métodos de Pago', 'primary'),
                    description: 'Configura los métodos de pago que aceptas en tu tienda. Las tarjetas superiores muestran cuántos métodos tienes activos, cuál es el predeterminado y si tienes códigos QR configurados.',
                    side: 'bottom',
                    align: 'center'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="metodosPagoTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#metodosPagoGrid',
                popover: {
                    title: t.stepTitle('wallet2', 'Tarjetas de Métodos', 'info'),
                    description: 'Cada método de pago aparece como tarjeta con nombre, cuenta y titular. Puedes agregar un código QR para que tus clientes escaneen y paguen. Al cobrar en el POS, puedes seleccionar el método de pago utilizado.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                popover: {
                    title: t.stepTitle('qr-code', 'Zoom de Código QR', 'success'),
                    description: 'Cuando un método de pago tiene imagen QR, haz clic sobre ella para ampliarla a pantalla completa. Tus clientes pueden escanearla directamente desde su celular. Presiona Escape o haz clic afuera para cerrar.',
                    side: 'over',
                    align: 'center'
                }
            },

            // ═══════════════════════════════════
            // TAB NOTIFICACIONES
            // ═══════════════════════════════════
            {
                element: '#notificacionesList',
                popover: {
                    title: t.stepTitle('bell-fill', 'Lista de Notificaciones', 'warning'),
                    description: 'Todas tus notificaciones aparecen aqui. El sistema te avisa sobre stock bajo, ventas importantes y otros eventos relevantes. Las no leidas se destacan con un borde azul.',
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
                    description: 'Marca todas las notificaciones como leidas de un solo clic. Util cuando ya revisaste todos los avisos pendientes.',
                    side: 'bottom',
                    align: 'end'
                }
            },

            // ═══════════════════════════════════
            // 39–40. TAB SUGERENCIAS
            // ═══════════════════════════════════
            {
                element: '#sugerenciaTexto',
                popover: {
                    title: t.stepTitle('chat-left-text', 'Enviar Sugerencia', 'info'),
                    description: 'Escribe tus ideas, sugerencias o comentarios para el equipo de My-Negocio. Tu opinion nos ayuda a mejorar la plataforma. Maximo 1000 caracteres.',
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

            // ═══════════════════════════════════
            // TAB PERFIL
            // ═══════════════════════════════════
            {
                element: '#perfilLogoImg, #perfilLogoPlaceholder',
                popover: {
                    title: t.stepTitle('image', 'Logo de tu Negocio', 'primary'),
                    description: 'Sube el logo de tu tienda haciendo clic sobre la imagen. Acepta JPG, PNG y WebP (maximo 2 MB). Tu logo aparecera en la barra superior del panel y en todos los recibos de ventas POS.',
                    side: 'right',
                    align: 'center'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="perfilTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#perfilEmail',
                popover: {
                    title: t.stepTitle('envelope', 'Datos de Contacto', 'info'),
                    description: 'Actualiza el email, telefono y direccion de tu tienda. Esta informacion se usa para los recibos y las comunicaciones automaticas con tus clientes.',
                    side: 'left',
                    align: 'start'
                }
            },
            {
                element: '#perfilPassActual',
                popover: {
                    title: t.stepTitle('shield-lock', 'Cambiar Contrasena', 'warning'),
                    description: 'Cambia tu contrasena de acceso ingresando la contrasena actual y la nueva dos veces. Recomendamos cambiarla periodicamente para mantener tu cuenta segura.',
                    side: 'left',
                    align: 'center'
                }
            },
            {
                element: '#perfilDiasRestantes',
                popover: {
                    title: t.stepTitle('calendar-check', 'Info de Suscripcion', 'success'),
                    description: 'Aqui ves los dias restantes de tu suscripcion a My-Negocio, la fecha de registro y el tipo de negocio. Si estas en periodo de prueba, aparecera una etiqueta indicandolo.',
                    side: 'left',
                    align: 'center'
                }
            },

            // ═══════════════════════════════════
            // TOPBAR — Elementos superiores
            // ═══════════════════════════════════
            {
                element: '#topbarLogoContainer',
                popover: {
                    title: t.stepTitle('shop', 'Logo en la Barra Superior', 'primary'),
                    description: 'Si subiste un logo desde el tab Perfil, aparecera aqui en la barra superior. Haz clic sobre el para ir directamente a la configuracion de tu perfil.',
                    side: 'bottom',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="posTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#bellBtn',
                popover: {
                    title: t.stepTitle('bell', 'Campana de Notificaciones', 'warning'),
                    description: 'Acceso rapido a tus notificaciones desde cualquier pestana. El contador rojo indica cuantas notificaciones sin leer tienes.',
                    side: 'bottom',
                    align: 'end'
                }
            },
            {
                element: '.mg-theme-btn',
                popover: {
                    title: t.stepTitle('palette', 'Cambiar Tema Visual', 'info'),
                    description: 'Personaliza la apariencia de tu panel. Elige entre 4 temas: Claro, Oscuro, Oceano y Atardecer. Tu seleccion se guarda automaticamente.',
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
            // 44. DESPEDIDA (sin elemento, centrado)
            // ───────────────────────────────────
            {
                popover: {
                    title: t.stepTitle('trophy-fill', '¡Tutorial Completado!', 'success'),
                    description: '¡Felicidades! Ya conoces todas las herramientas de tu tienda. Usa el POS para cobrar rapido, gestiona tu inventario, comparte tu catalogo y analiza tus reportes. ¡Haz crecer tu negocio con My-Negocio!',
                    side: 'over',
                    align: 'center'
                }
            }
        ];
    }

    // Registrar los pasos cuando el DOM este listo
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            window._mgTutorialTienda = buildSteps();
        });
    } else {
        window._mgTutorialTienda = buildSteps();
    }
})();
