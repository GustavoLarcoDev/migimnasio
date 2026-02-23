// ============================================================
// My-Negocio — Tutorial Interactivo: Negocio Artesanal
// Barberías, Salones de Uñas, Spas — Negocios con citas
// ============================================================

(function () {
    'use strict';

    // Construir los pasos del tutorial
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
                    description: 'Este tutorial te guiará por todas las herramientas de tu panel artesanal, diseñado para barberías, salones de uñas, spas y negocios con citas. Aprenderás a gestionar tu agenda, empleados, servicios, clientes y mucho más. ¡Comencemos!',
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
                    description: 'Este es tu menú principal. Desde aquí accedes a todas las secciones de tu negocio: Agenda, Clientes, Servicios, Empleados, Movimientos, Reportes, Inventario, Recibos, Métodos de Pago, Notificaciones y Sugerencias.',
                    side: 'right',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    var tt = window._mgTutorial;
                    if (tt) tt.ensureSidebarVisible();
                }
            },

            // ───────────────────────────────────
            // 3–9. TAB AGENDA (default/active)
            // ───────────────────────────────────
            {
                element: '#statCitasHoy',
                popover: {
                    title: t.stepTitle('calendar-check', 'Estadísticas del Día', 'primary'),
                    description: 'Estas tarjetas muestran un resumen de tu día: citas programadas, ingresos generados, citas completadas y canceladas. Se actualizan automáticamente al cargar la agenda.',
                    side: 'bottom',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    var tt = window._mgTutorial;
                    if (tt) tt.closeSidebarMobile();
                    var link = document.querySelector('[data-mg-tab="agendaTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#employeeTabs',
                popover: {
                    title: t.stepTitle('people-fill', 'Filtro por Empleado', 'info'),
                    description: 'Filtra el calendario por empleado. La pestaña "Todos" muestra todas las citas. Cada empleado tiene un color único para identificar sus citas fácilmente. Los empleados que no trabajan hoy aparecen deshabilitados.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#btnNuevaCita',
                popover: {
                    title: t.stepTitle('plus-circle', 'Nueva Cita', 'success'),
                    description: 'Haz clic aquí para agendar una nueva cita. Selecciona el empleado, servicio, cliente, fecha y horario disponible. También puedes crear citas haciendo clic directamente en el calendario.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#calendarViewPills',
                popover: {
                    title: t.stepTitle('calendar3', 'Vistas del Calendario', 'info'),
                    description: 'Cambia entre vista de Día, Semana o Mes. La vista de día es ideal para ver la agenda detallada, mientras que la vista de mes te da una panorámica completa.',
                    side: 'bottom',
                    align: 'end'
                }
            },
            {
                element: '#calendario-citas',
                popover: {
                    title: t.stepTitle('calendar-week', 'Calendario de Citas', 'primary'),
                    description: 'Este es el corazón de tu agenda. Las citas se muestran con el color del empleado asignado. Haz clic en cualquier cita para ver sus detalles, cambiar su estado o registrar el pago.',
                    side: 'top',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 8–9. FLUJO DE CITAS (explicación sin modal)
            // ───────────────────────────────────
            {
                popover: {
                    title: t.stepTitle('diagram-3', 'Crear una Cita', 'success'),
                    description: 'Al crear una cita seleccionas: empleado, servicio, cliente (buscar existente o crear nuevo), fecha y horario disponible. El sistema calcula automáticamente los slots libres según la duración del servicio y el horario del empleado.',
                    side: 'over',
                    align: 'center'
                }
            },
            {
                popover: {
                    title: t.stepTitle('arrow-right-circle', 'Ciclo de Vida de una Cita', 'warning'),
                    description: 'Cada cita pasa por estados: Pendiente, Confirmada, En Progreso y finalmente Completada o Cancelada. Al completar una cita puedes registrar el pago con método de pago, cargos extra y propina. Todo se registra automáticamente en movimientos y recibos.',
                    side: 'over',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 10–12. TAB CLIENTES
            // ───────────────────────────────────
            {
                element: '#btnNuevoClienteArt',
                popover: {
                    title: t.stepTitle('person-plus-fill', 'Nuevo Cliente', 'success'),
                    description: 'Registra un nuevo cliente con nombre, apellido, teléfono, email y dirección. A diferencia de membresías, aquí no hay fechas de vencimiento: los clientes simplemente agendan citas.',
                    side: 'bottom',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="clientesTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#clientesArtTable',
                popover: {
                    title: t.stepTitle('table', 'Tabla de Clientes', 'primary'),
                    description: 'Aquí aparecen todos tus clientes con su teléfono, email y número de citas. Puedes buscar, ordenar y usar los botones de acción para ver historial, editar o eliminar.',
                    side: 'top',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 13–15. TAB SERVICIOS
            // ───────────────────────────────────
            {
                element: '#btnNuevoServicio',
                popover: {
                    title: t.stepTitle('plus-circle', 'Nuevo Servicio', 'success'),
                    description: 'Crea los servicios que ofrece tu negocio. Define nombre, descripción, precio, duración en minutos e ítems incluidos. También puedes marcarlo como combo para agrupar varios servicios.',
                    side: 'bottom',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="serviciosTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#serviciosGrid',
                popover: {
                    title: t.stepTitle('scissors', 'Tarjetas de Servicios', 'primary'),
                    description: 'Tus servicios aparecen como tarjetas con nombre, precio, duración e ítems incluidos. La duración es clave porque el sistema la usa para calcular los horarios disponibles al agendar citas.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                popover: {
                    title: t.stepTitle('lightbulb', 'Tip: Items y Combos', 'info'),
                    description: 'Usa el campo "Ítems Incluidos" para detallar lo que incluye el servicio (separados con |). Por ejemplo: Lavado|Corte|Secado. Activa "Es combo" si el servicio agrupa varios tratamientos con precio especial.',
                    side: 'over',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 16–18. TAB EMPLEADOS
            // ───────────────────────────────────
            {
                element: '#btnNuevoEmpleado',
                popover: {
                    title: t.stepTitle('person-badge', 'Nuevo Empleado', 'success'),
                    description: 'Registra a tu equipo de trabajo con nombre, teléfono, email y especialidad. Cada empleado tendrá su propia agenda y color en el calendario.',
                    side: 'bottom',
                    align: 'start'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="empleadosTab"]');
                    if (link && !link.classList.contains('active')) link.click();
                }
            },
            {
                element: '#empleadosGrid',
                popover: {
                    title: t.stepTitle('person-badge', 'Tarjetas de Empleados', 'primary'),
                    description: 'Tus empleados aparecen como tarjetas con su información y especialidad. Desde cada tarjeta puedes configurar su horario semanal, agregar excepciones (vacaciones, días libres) y editar sus datos.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                popover: {
                    title: t.stepTitle('clock', 'Horarios y Excepciones', 'warning'),
                    description: 'Cada empleado tiene un horario semanal configurable (lunes a domingo con hora de inicio y fin). Puedes agregar excepciones para días específicos como vacaciones o feriados. El sistema usa estos horarios para mostrar solo los slots disponibles al agendar citas.',
                    side: 'over',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 19–22. TAB MOVIMIENTOS (LOGS)
            // ───────────────────────────────────
            {
                element: '#logDescripcion',
                popover: {
                    title: t.stepTitle('journal-text', 'Registrar Movimiento', 'primary'),
                    description: 'Ingresa la descripción de tu movimiento financiero. Puede ser un ingreso (como venta de producto) o un gasto (como compra de insumos para el salón).',
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
                    description: 'Después de llenar la descripción y el monto, haz clic aquí para registrar. Usa valores positivos para ingresos y negativos para gastos. Los pagos de citas se registran automáticamente.',
                    side: 'left',
                    align: 'center'
                }
            },
            {
                element: '#logsTable',
                popover: {
                    title: t.stepTitle('list-ul', 'Historial de Movimientos', 'info'),
                    description: 'Todos tus movimientos aparecen aquí con descripción, tipo, cliente, monto y fecha. Los pagos de citas se generan automáticamente. Cada registro es inmutable para proteger la integridad de tus datos.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#btnExportLogsExcel',
                popover: {
                    title: t.stepTitle('file-earmark-excel', 'Exportar Movimientos', 'success'),
                    description: 'Exporta todos tus movimientos a Excel para respaldo o análisis contable. Perfecto para llevar control de tus finanzas fuera de la plataforma.',
                    side: 'bottom',
                    align: 'end'
                }
            },

            // ───────────────────────────────────
            // 23–26. TAB REPORTES (VENTAS)
            // ───────────────────────────────────
            {
                element: '#ventasPeriodPills',
                popover: {
                    title: t.stepTitle('funnel', 'Filtro de Reportes', 'info'),
                    description: 'Cambia entre Día, Semana o Mes para ver los reportes financieros del periodo que te interese. Las tarjetas y gráficos se actualizan automáticamente.',
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
                    description: 'Estas tarjetas con degradado muestran tus Ingresos, Gastos y Ganancia Neta del periodo seleccionado. Una visión rápida de la salud financiera de tu negocio.',
                    side: 'bottom',
                    align: 'start'
                }
            },
            {
                element: '#chartBarVentas',
                popover: {
                    title: t.stepTitle('bar-chart-fill', 'Gráfico de Ingresos vs Gastos', 'primary'),
                    description: 'Visualiza la comparación entre tus ingresos y gastos en un gráfico de barras interactivo. Pasa el cursor sobre las barras para ver los montos detallados.',
                    side: 'top',
                    align: 'center'
                }
            },
            {
                element: '#chartResumen',
                popover: {
                    title: t.stepTitle('pie-chart-fill', 'Gráfico de Resumen', 'info'),
                    description: 'Este gráfico de dona muestra la proporción entre ingresos y gastos, con la ganancia neta en el centro. Una forma visual de entender la rentabilidad de tu negocio.',
                    side: 'top',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 27–30. TAB INVENTARIO
            // ───────────────────────────────────
            {
                element: '#btnCrearProducto',
                popover: {
                    title: t.stepTitle('plus-circle', 'Nuevo Producto', 'success'),
                    description: 'Agrega productos a tu inventario (esmaltes, cremas, ceras, shampoos, etc.). Ingresa nombre, precio de venta, costo de compra y stock inicial. El sistema calcula tu margen de ganancia.',
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
                    description: 'Tus productos aparecen como tarjetas con nombre, precio y nivel de stock. Vende rápidamente con un clic, reabastece y ajusta stock. Los productos con stock bajo se resaltan con una alerta.',
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
                    description: 'Descarga tu inventario completo en formato Excel con todos los productos, precios, costos y niveles de stock actuales.',
                    side: 'bottom',
                    align: 'center'
                }
            },

            // ───────────────────────────────────
            // 31–32. TAB RECIBOS
            // ───────────────────────────────────
            {
                element: '#tablaRecibos',
                popover: {
                    title: t.stepTitle('receipt', 'Tabla de Recibos', 'primary'),
                    description: 'Aquí se guardan automáticamente los recibos de cada transacción: pagos de citas, ventas de productos y otros cobros. Haz clic en "Ver" para ver el detalle completo de cada recibo.',
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
                    description: 'Exporta todos tus recibos a Excel. Usa el campo de búsqueda para encontrar un recibo específico por su número.',
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
                    description: 'Configura los métodos de pago que aceptas en tu negocio. Las tarjetas superiores muestran cuántos métodos tienes activos, cuál es el predeterminado y si tienes códigos QR configurados.',
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
                    description: 'Cada método de pago aparece como tarjeta con nombre, cuenta y titular. Puedes agregar un código QR para que tus clientes escaneen y paguen. Al completar una cita, el sistema te pedirá seleccionar el método de pago utilizado.',
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

            // ───────────────────────────────────
            // TAB NOTIFICACIONES
            // ───────────────────────────────────
            {
                element: '#notificacionesList',
                popover: {
                    title: t.stepTitle('bell-fill', 'Lista de Notificaciones', 'warning'),
                    description: 'Todas tus notificaciones aparecen aquí. El sistema te avisa sobre citas próximas, cancelaciones y eventos importantes. Las no leídas se destacan con un borde azul.',
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
                    title: t.stepTitle('check-all', 'Marcar Todas como Leídas', 'info'),
                    description: 'Marca todas las notificaciones como leídas de un solo clic. Útil cuando ya revisaste los avisos pendientes y quieres limpiar la lista.',
                    side: 'bottom',
                    align: 'end'
                }
            },

            // ───────────────────────────────────
            // 35–36. TAB SUGERENCIAS
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
            // 37–39. TOPBAR — Elementos superiores
            // ───────────────────────────────────
            {
                element: '#bellBtn',
                popover: {
                    title: t.stepTitle('bell', 'Campana de Notificaciones', 'warning'),
                    description: 'Acceso rápido a tus notificaciones desde cualquier pestaña. El contador rojo indica cuántas sin leer tienes. Haz clic para ver un resumen sin salir de donde estés.',
                    side: 'bottom',
                    align: 'end'
                },
                onHighlightStarted: function () {
                    var link = document.querySelector('[data-mg-tab="agendaTab"]');
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
            // 40. DESPEDIDA (sin elemento, centrado)
            // ───────────────────────────────────
            {
                popover: {
                    title: t.stepTitle('trophy-fill', '¡Tutorial Completado!', 'success'),
                    description: '¡Felicidades! Ya conoces todas las herramientas de tu panel artesanal. Gestiona tu agenda, controla tus empleados, ofrece servicios increíbles y haz crecer tu negocio con My-Negocio. Si tienes dudas, repite este tutorial cuando quieras.',
                    side: 'over',
                    align: 'center'
                }
            }
        ];
    }

    // Registrar los pasos cuando el DOM esté listo
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            window._mgTutorialArtesanal = buildSteps();
        });
    } else {
        window._mgTutorialArtesanal = buildSteps();
    }
})();
