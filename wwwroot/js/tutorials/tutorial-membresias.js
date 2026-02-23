// ============================================================
// My-Negocio — Tutorial Interactivo: Negocio de Membresías
// Gimnasios, Yoga, Clubs, etc. con membresías recurrentes
// ============================================================

(function () {
    'use strict';

    // Esperar a que el loader esté disponible
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
                    description: 'Este tutorial te guiará paso a paso por todas las herramientas de tu panel de membresías. Aprenderás a gestionar clientes, registrar movimientos, revisar reportes y mucho más. ¡Comencemos!',
                    side: 'over',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 2. SIDEBAR — Menú de navegación
            // ───────────────────────────────────
            {
                element: '#sidebar',
                popover: {
                    title: t.stepTitle('layout-sidebar', 'Menú de Navegación', 'primary'),
                    description: 'Este es tu menú principal. Desde aquí puedes acceder a todas las secciones: Resumen, Clientes, Movimientos, Reportes, Inventario, Recibos, Métodos de Pago, Notificaciones y Sugerencias.',
                    side: 'right',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    t.ensureSidebarVisible();
                }
            },

            // ───────────────────────────────────
            // 3–8. TAB RESUMEN
            // ───────────────────────────────────
            {
                element: '#resumenPeriodPills',
                popover: {
                    title: t.stepTitle('funnel', 'Filtro de Periodo', 'info'),
                    description: 'Filtra la información del resumen por Hoy, Semana o Mes. Los datos de ingresos, gastos y ganancias se actualizan automáticamente al cambiar de periodo.',
                    side: 'bottom',
                    align: 'center'
                },
                onHighlightStarted: function () {
                    t.closeSidebarMobile();
                    var link = document.querySelector('[data-mg-tab="resumenTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#resumenIngresos',
                popover: {
                    title: t.stepTitle('arrow-up-circle', 'Tarjetas de Finanzas', 'success'),
                    description: 'Aquí ves tus Ingresos, Gastos y Ganancia Neta del periodo seleccionado. Estas tarjetas con degradado te dan una visión rápida de la salud financiera de tu negocio.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#resumenTotalClientes',
                popover: {
                    title: t.stepTitle('people-fill', 'Estadísticas de Clientes', 'primary'),
                    description: 'Consulta de un vistazo cuántos clientes tienes en total, cuántos están activos, vencidos y los nuevos del día. Ideal para monitorear el crecimiento de tu negocio.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#chartResumenOverview',
                popover: {
                    title: t.stepTitle('bar-chart-fill', 'Gráfico de Ingresos vs Gastos', 'info'),
                    description: 'Este gráfico de barras compara tus ingresos contra tus gastos en el periodo seleccionado. Pasa el cursor sobre las barras para ver los montos exactos.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#proximosVencerList',
                popover: {
                    title: t.stepTitle('clock-fill', 'Próximos a Vencer', 'warning'),
                    description: 'Lista de clientes cuyas membresías están por expirar en los próximos días. Actúa a tiempo para renovarlos y evitar perder clientes activos.',
                    side: 'left',
                    align: 'start'
                }
            },

            // ───────────────────────────────────
            // 9–14. TAB CLIENTES
            // ───────────────────────────────────
            {
                element: '#btnCrearCliente',
                popover: {
                    title: t.stepTitle('person-plus-fill', 'Crear Nuevo Cliente', 'success'),
                    description: 'Haz clic aquí para registrar un nuevo cliente. Podrás ingresar nombre, teléfono, email, fecha de inicio, fecha de fin y precio de la membresía.',
                    side: 'bottom',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="clientesTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#totalClientes',
                popover: {
                    title: t.stepTitle('graph-up-arrow', 'Estadísticas de Clientes', 'primary'),
                    description: 'Estas tarjetas muestran el total de clientes, activos, vencidos y los ingresos del mes. Se actualizan cada vez que entras a esta pestaña.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#clientesTable',
                popover: {
                    title: t.stepTitle('table', 'Tabla de Clientes', 'primary'),
                    description: 'Aquí ves todos tus clientes con sus detalles: precio, fechas y días restantes. Puedes buscar, ordenar por columna, y usar los botones de acción para renovar, editar o eliminar.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#btnExportClientesExcel',
                popover: {
                    title: t.stepTitle('file-earmark-excel', 'Exportar a Excel', 'success'),
                    description: 'Descarga un archivo Excel con todos tus clientes y sus datos. Perfecto para respaldos, análisis externos o compartir información con tu equipo.',
                    side: 'bottom',
                    align: 'center'
                }
            },
            {
                element: '#btnImportarExcel',
                popover: {
                    title: t.stepTitle('upload', 'Importar desde Excel', 'info'),
                    description: 'Importa clientes de forma masiva desde un archivo Excel. Solo necesitas las columnas Nombre y Apellido como mínimo. Puedes arrastrar el archivo o seleccionarlo manualmente.',
                    side: 'bottom',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 15–18. TAB MOVIMIENTOS (LOGS)
            // ───────────────────────────────────
            {
                element: '#logDescripcion',
                popover: {
                    title: t.stepTitle('journal-text', 'Registrar Movimiento', 'primary'),
                    description: 'Ingresa la descripción de tu movimiento financiero. Puede ser un ingreso (como venta de producto) o un gasto (como pago de servicios).',
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
                    description: 'Después de llenar la descripción y el monto, haz clic aquí para registrar. Usa valores positivos para ingresos y negativos para gastos. Cada registro es inmutable para proteger la integridad de tus datos.',
                    side: 'left',
                    align: 'center'
                }
            },
            {
                element: '#logsTable',
                popover: {
                    title: t.stepTitle('list-ul', 'Historial de Movimientos', 'info'),
                    description: 'Todos tus movimientos registrados aparecen aquí con descripción, tipo, cliente asociado, monto y fecha. Los registros de clientes (creación, renovación) se generan automáticamente.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#btnExportLogsExcel',
                popover: {
                    title: t.stepTitle('file-earmark-excel', 'Exportar Movimientos', 'success'),
                    description: 'Exporta todos tus movimientos a Excel para respaldo o análisis. Si tienes registros con más de 30 días, también podrás cerrar el periodo descargando y limpiando la lista.',
                    side: 'bottom',
                    align: 'end'
                }
            },

            // ───────────────────────────────────
            // 19–22. TAB REPORTES (VENTAS)
            // ───────────────────────────────────
            {
                element: '#ventasPeriodPills',
                popover: {
                    title: t.stepTitle('funnel', 'Filtro de Reportes', 'info'),
                    description: 'Cambia entre Día, Semana o Mes para ver los reportes del periodo que te interese. Las tarjetas y gráficos se actualizan automáticamente.',
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
                    description: 'Estas tarjetas muestran los ingresos, gastos y ganancia neta del periodo seleccionado. También puedes ver datos adicionales como membresías del mes, clientes diarios y ganancia mensual.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#chartBarVentas',
                popover: {
                    title: t.stepTitle('bar-chart-fill', 'Gráfico de Ingresos vs Gastos', 'primary'),
                    description: 'Visualiza la comparación entre tus ingresos y gastos en un gráfico de barras interactivo. Pasa el cursor para ver los montos detallados de cada barra.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#chartResumen',
                popover: {
                    title: t.stepTitle('pie-chart-fill', 'Gráfico de Resumen', 'info'),
                    description: 'Este gráfico de dona muestra la proporción entre ingresos y gastos, con la ganancia neta en el centro. Una forma visual e intuitiva de entender la rentabilidad de tu negocio.',
                    side: 'top',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 23–26. TAB INVENTARIO
            // ───────────────────────────────────
            {
                element: '#btnCrearProducto',
                popover: {
                    title: t.stepTitle('plus-circle', 'Nuevo Producto', 'success'),
                    description: 'Agrega productos a tu inventario ingresando nombre, precio de venta, costo de compra y stock inicial. El sistema calcula automáticamente tu margen de ganancia.',
                    side: 'bottom',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="inventarioTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#productosGrid',
                popover: {
                    title: t.stepTitle('grid-3x3-gap', 'Tarjetas de Productos', 'primary'),
                    description: 'Tus productos aparecen como tarjetas con nombre, precio y stock. Usa la flecha hacia abajo para vender 1 unidad rápidamente y la flecha hacia arriba para devolver o reabastecer. Cada tarjeta tiene botones para editar y eliminar.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#btnToggleMovimientos',
                popover: {
                    title: t.stepTitle('clock-history', 'Historial de Inventario', 'info'),
                    description: 'Muestra u oculta el historial de movimientos del inventario: ventas, devoluciones, restock y ajustes. Todo queda registrado para auditoría y control.',
                    side: 'bottom',
                    align: 'center'
                }
            },
            {
                element: '#btnExportInventarioExcel',
                popover: {
                    title: t.stepTitle('file-earmark-excel', 'Exportar Inventario', 'success'),
                    description: 'Descarga tu inventario completo en formato Excel con todos los productos, precios, costos y niveles de stock.',
                    side: 'bottom',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 27–28. TAB RECIBOS
            // ───────────────────────────────────
            {
                element: '#tablaRecibos',
                popover: {
                    title: t.stepTitle('receipt', 'Tabla de Recibos', 'primary'),
                    description: 'Aquí se guardan automáticamente los recibos de cada transacción: membresías nuevas, renovaciones y ventas. Puedes ver el detalle de cada recibo haciendo clic en "Ver".',
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
                    description: 'Exporta todos tus recibos a Excel. También puedes buscar un recibo específico por su número usando el campo de búsqueda.',
                    side: 'bottom',
                    align: 'end'
                }
            },

            // ───────────────────────────────────
            // TAB METODOS DE PAGO
            // ───────────────────────────────────
            {
                element: '#metodosPagoStats',
                popover: {
                    title: t.stepTitle('credit-card', 'Métodos de Pago', 'primary'),
                    description: 'Aquí configuras los métodos de pago que aceptas en tu negocio. Las tarjetas superiores muestran cuántos métodos tienes activos, cuál es el predeterminado y si tienes códigos QR configurados.',
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
                    description: 'Cada método de pago aparece como una tarjeta con su nombre, cuenta bancaria y titular. Puedes agregar un código QR para que tus clientes escaneen y paguen fácilmente. Haz clic en el QR para verlo en grande.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                popover: {
                    title: t.stepTitle('qr-code', 'Zoom de Código QR', 'success'),
                    description: 'Cuando un método de pago tiene imagen QR, puedes hacer clic sobre ella para ampliarla a pantalla completa. Esto facilita que tus clientes la escaneen directamente desde su celular. Presiona Escape o haz clic afuera para cerrar.',
                    side: 'over',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // TAB NOTIFICACIONES
            // ───────────────────────────────────
            {
                element: '#notificacionesList',
                popover: {
                    title: t.stepTitle('bell-fill', 'Lista de Notificaciones', 'warning'),
                    description: 'Todas tus notificaciones aparecen aquí. El sistema te avisa cuando las membresías de tus clientes están por vencer (3 días antes). Las no leídas se destacan con un borde azul.',
                    side: 'top',
                    align: 'center'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="notificacionesTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#btnGenerarNotificaciones',
                popover: {
                    title: t.stepTitle('arrow-repeat', 'Generar Notificaciones', 'primary'),
                    description: 'Haz clic aquí para verificar manualmente si hay membresías próximas a vencer. Las notificaciones también se generan automáticamente al entrar al panel.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#btnMarcarTodasLeidasTab',
                popover: {
                    title: t.stepTitle('check-all', 'Marcar Todas como Leídas', 'info'),
                    description: 'Marca todas las notificaciones como leídas de un solo clic. Útil cuando ya revisaste los avisos pendientes.',
                    side: 'bottom',
                    align: 'end'
                }
            },

            // ───────────────────────────────────
            // 32–33. TAB SUGERENCIAS
            // ───────────────────────────────────
            {
                element: '#sugerenciaTexto',
                popover: {
                    title: t.stepTitle('chat-left-text', 'Enviar Sugerencia', 'info'),
                    description: 'Escribe aquí tus ideas, sugerencias o comentarios para el equipo de My-Negocio. Tu opinión nos ayuda a mejorar la plataforma. Máximo 1000 caracteres.',
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
                    description: 'Cuando termines de escribir tu sugerencia, haz clic aquí para enviarla. El equipo de desarrollo la recibirá y la tomará en cuenta para futuras mejoras.',
                    side: 'top',
                    align: 'start'
                }
            },

            // ───────────────────────────────────
            // 34–36. TOPBAR — Elementos superiores
            // ───────────────────────────────────
            {
                element: '#bellBtn',
                popover: {
                    title: t.stepTitle('bell', 'Campana de Notificaciones', 'warning'),
                    description: 'Acceso rápido a tus notificaciones desde cualquier pestaña. El contador rojo indica cuántas notificaciones sin leer tienes. Haz clic para ver un resumen sin salir de donde estés.',
                    side: 'bottom',
                    align: 'end'
                },
                onHighlightStarted: function () {
                    // Navegar al resumen para que el topbar esté visible
                    var link = document.querySelector('[data-mg-tab="resumenTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '.mg-theme-btn',
                popover: {
                    title: t.stepTitle('palette', 'Cambiar Tema Visual', 'info'),
                    description: 'Personaliza la apariencia de tu panel. Elige entre 4 temas: Claro, Oscuro, Océano y Atardecer. Tu selección se guarda automáticamente para tu próxima visita.',
                    side: 'bottom',
                    align: 'end'
                }
            },
            {
                element: '#btnTutorial',
                popover: {
                    title: t.stepTitle('mortarboard', 'Repetir Tutorial', 'primary'),
                    description: 'Si alguna vez necesitas recordar cómo funciona algo, haz clic en este botón para ejecutar este tutorial guiado nuevamente en cualquier momento.',
                    side: 'bottom',
                    align: 'end'
                }
            },

            // ───────────────────────────────────
            // 37. DESPEDIDA (sin elemento, centrado)
            // ───────────────────────────────────
            {
                popover: {
                    title: t.stepTitle('trophy-fill', '¡Tutorial Completado!', 'success'),
                    description: '¡Felicidades! Ya conoces todas las herramientas de tu panel de membresías. Gestiona tus clientes, controla tus finanzas y haz crecer tu negocio con My-Negocio. Si tienes dudas, repite este tutorial cuando quieras.',
                    side: 'over',
                    align: 'center'
                }
            }
        ];
    }

    // Registrar los pasos cuando el DOM esté listo
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            window._mgTutorialMembresias = buildSteps();
        });
    } else {
        window._mgTutorialMembresias = buildSteps();
    }
})();
