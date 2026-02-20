using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Gimnasio.Helpers;

/// <summary>
/// Genera documentos Word (.docx) de guia para negocios y vendedores.
/// Los documentos se generan en memoria (MemoryStream) y se retornan como byte[]
/// para adjuntarlos directamente a los emails de bienvenida via MimeKit.
/// Usa el SDK Open XML de Microsoft (sin dependencia de Word instalado).
/// Cada tipo de negocio tiene su propia guia especializada.
/// </summary>
public static class GuideDocumentGenerator
{
    /// <summary>
    /// Genera la guia correcta segun el tipo de negocio.
    /// Dispatcher principal que selecciona el metodo especializado.
    /// </summary>
    public static byte[] GenerarGuiaNegocio(string nombreNegocio, string nombreDueno, string email, string telefono, string tipoNegocio = "membresias")
    {
        return tipoNegocio switch
        {
            "artesanal" => GenerarGuiaArtesanal(nombreNegocio, nombreDueno, email, telefono),
            "tienda" => GenerarGuiaTienda(nombreNegocio, nombreDueno, email, telefono),
            "restaurante" => GenerarGuiaRestaurante(nombreNegocio, nombreDueno, email, telefono),
            _ => GenerarGuiaMembresias(nombreNegocio, nombreDueno, email, telefono),
        };
    }

    // ═══════════════════════════════════════════════════════════
    // GUIA: MEMBRESIAS (gimnasios, clubes, negocios con membresias)
    // ═══════════════════════════════════════════════════════════

    private static byte[] GenerarGuiaMembresias(string nombreNegocio, string nombreDueno, string email, string telefono)
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            // Titulo principal
            AddHeading(body, "Guia de Inicio — My-Negocio Membresias", 28, true);
            AddParagraph(body, "Su guia completa para empezar a gestionar su negocio con confianza");
            AddParagraph(body, "");

            // Diagrama del dashboard
            AddHeading(body, "Vista General del Dashboard", 16, true);
            AddParagraph(body, "+------------------------------------------------------------------+");
            AddParagraph(body, "|  MY-NEGOCIO                    [Topbar: Nombre / Campana / Tema] |");
            AddParagraph(body, "+----------------+-------------------------------------------------+");
            AddParagraph(body, "|  BARRA LATERAL |   AREA DE CONTENIDO PRINCIPAL                   |");
            AddParagraph(body, "|                |   [ Hoy ] [ Semana ] [ Mes ]  <- Periodos       |");
            AddParagraph(body, "|  [=] Resumen   |   +----------+ +----------+ +----------+        |");
            AddParagraph(body, "|  [P] Clientes  |   | INGRESOS | |  GASTOS  | | GANANCIA |        |");
            AddParagraph(body, "|  [J] Movimient.|   |  $0.00   | |  $0.00   | |  $0.00   |        |");
            AddParagraph(body, "|  [G] Reportes  |   +----------+ +----------+ +----------+        |");
            AddParagraph(body, "|  [B] Inventario|   +--------+ +--------+ +--------+ +--------+   |");
            AddParagraph(body, "|  [R] Recibos   |   | Total  | | Activos| |Vencidos| | Nuevos |   |");
            AddParagraph(body, "|  [!] Notific.  |   +--------+ +--------+ +--------+ +--------+   |");
            AddParagraph(body, "|  [?] Sugerenc. |   +---------------------------+ +-------------+  |");
            AddParagraph(body, "|                |   |  Grafico Ingresos/Gastos  | | Proximos a  |  |");
            AddParagraph(body, "|  [->] Cerrar   |   |     (barras)              | |   Vencer    |  |");
            AddParagraph(body, "+----------------+-------------------------------------------------+");
            AddParagraph(body, "");

            // Bienvenida
            AddHeading(body, "1. Bienvenida", 20, true);
            AddHeading(body, $"Bienvenido, {nombreDueno}!", 18, false);
            AddParagraph(body, $"Le damos la mas cordial bienvenida a My-Negocio, la plataforma digital para administrar {nombreNegocio} desde cualquier dispositivo.");
            AddParagraph(body, "");
            AddParagraph(body, "Con My-Negocio usted puede:");
            AddParagraph(body, "- Controlar todos sus clientes en un solo lugar: activos, por vencer y vencidos");
            AddParagraph(body, "- Registrar ingresos y gastos con un solo clic");
            AddParagraph(body, "- Gestionar su inventario de productos (bebidas, suplementos, accesorios)");
            AddParagraph(body, "- Ver reportes y graficos de ventas por dia, semana o mes");
            AddParagraph(body, "- Recibir alertas automaticas cuando un cliente esta por vencer su membresia");
            AddParagraph(body, "- Exportar su informacion a Excel con un solo clic");
            AddParagraph(body, "- Enviar mensajes de WhatsApp directamente desde la plataforma");
            AddParagraph(body, "- Emitir recibos digitales que se envian automaticamente por correo");
            AddParagraph(body, "");

            // Credenciales
            AddHeading(body, "2. Primeros Pasos — Como Ingresar", 20, true);
            AddParagraph(body, $"Email: {email}");
            AddParagraph(body, $"Telefono: {telefono}");
            AddParagraph(body, "Puede iniciar sesion con su email O su numero de telefono.");
            AddParagraph(body, "");
            AddParagraph(body, "1. Abra el navegador de su dispositivo (Chrome, Safari, Firefox)");
            AddParagraph(body, "2. Ingrese la direccion web de My-Negocio");
            AddParagraph(body, "3. Escriba su email o telefono y su contrasena");
            AddParagraph(body, "4. Haga clic en 'Iniciar Sesion'");
            AddParagraph(body, "");
            AddParagraph(body, "IMPORTANTE: Cambie su contrasena despues del primer inicio de sesion por seguridad.");
            AddParagraph(body, "Si accede desde un dispositivo compartido, cierre sesion al terminar.");
            AddParagraph(body, "");

            // Dashboard
            AddHeading(body, "3. Su Panel de Control (Dashboard)", 20, true);
            AddParagraph(body, "Al ingresar vera el Resumen general con toda la informacion importante de un vistazo.");
            AddParagraph(body, "");
            AddParagraph(body, "BARRA LATERAL IZQUIERDA — Menu de navegacion:");
            AddParagraph(body, "  - Resumen: Pantalla de inicio con estadisticas generales");
            AddParagraph(body, "  - Clientes: Administrar clientes y membresias");
            AddParagraph(body, "  - Movimientos: Registro de ingresos y gastos");
            AddParagraph(body, "  - Reportes: Graficos y estadisticas de ventas");
            AddParagraph(body, "  - Inventario: Gestion de productos");
            AddParagraph(body, "  - Recibos: Historial de recibos generados");
            AddParagraph(body, "  - Notificaciones: Alertas de membresias por vencer");
            AddParagraph(body, "  - Sugerencias: Canal directo para enviar ideas al equipo");
            AddParagraph(body, "");
            AddParagraph(body, "TARJETAS DE RESUMEN FINANCIERO (parte superior):");
            AddParagraph(body, "  - Ingresos (verde): Total de dinero que ha entrado");
            AddParagraph(body, "  - Gastos (rojo): Total de dinero que ha salido");
            AddParagraph(body, "  - Ganancia Neta (azul): Ingresos menos Gastos");
            AddParagraph(body, "");
            AddParagraph(body, "BOTONES DE PERIODO: Hoy / Semana / Mes — Cambian los numeros al instante.");
            AddParagraph(body, "");
            AddParagraph(body, "TARJETAS DE CLIENTES:");
            AddParagraph(body, "  - Total Clientes / Activos / Vencidos / Nuevos Hoy");
            AddParagraph(body, "");

            // Gestion de clientes
            AddHeading(body, "4. Gestion de Clientes", 20, true);
            AddParagraph(body, "Esta es la seccion mas utilizada. Aqui registra a cada persona que paga una membresia.");
            AddParagraph(body, "");
            AddParagraph(body, "AGREGAR UN CLIENTE NUEVO:");
            AddParagraph(body, "1. Haga clic en el boton verde 'Nuevo Cliente'");
            AddParagraph(body, "2. Complete: Nombre, Apellido, Telefono (obligatorios), Email, Direccion (opcionales)");
            AddParagraph(body, "3. Seleccione Fecha Inicio, Fecha Fin y Precio de la membresia");
            AddParagraph(body, "4. El sistema calcula automaticamente los dias restantes");
            AddParagraph(body, "5. Haga clic en 'Guardar'");
            AddParagraph(body, "6. Si tiene email, recibira un recibo digital automaticamente");
            AddParagraph(body, "");
            AddParagraph(body, "RENOVAR UNA MEMBRESIA:");
            AddParagraph(body, "1. En la tabla de clientes, haga clic en el boton naranja de renovar");
            AddParagraph(body, "2. Seleccione la nueva fecha de fin y el precio");
            AddParagraph(body, "3. Haga clic en 'Guardar'");
            AddParagraph(body, "4. El sistema le ofrecera enviar WhatsApp de confirmacion");
            AddParagraph(body, "");
            AddParagraph(body, "PASE DIARIO: Active el interruptor 'Es Diario' al crear un cliente.");
            AddParagraph(body, "");
            AddParagraph(body, "IMPORTAR DESDE EXCEL: Boton 'Importar Excel' para cargar clientes masivamente.");
            AddParagraph(body, "EXPORTAR A EXCEL: Boton 'Exportar' para descargar la lista completa.");
            AddParagraph(body, "");
            AddParagraph(body, "INDICADOR DE VENCIMIENTO:");
            AddParagraph(body, "  - Verde: mas de 3 dias restantes");
            AddParagraph(body, "  - Amarillo: 3 dias o menos");
            AddParagraph(body, "  - Rojo ('Vencido'): membresia expirada");
            AddParagraph(body, "");

            // Inventario
            AddHeading(body, "5. Inventario de Productos", 20, true);
            AddParagraph(body, "Si vende productos fisicos (bebidas, suplementos, accesorios), esta seccion controla el stock.");
            AddParagraph(body, "");
            AddParagraph(body, "AGREGAR PRODUCTO:");
            AddParagraph(body, "1. Haga clic en 'Nuevo Producto'");
            AddParagraph(body, "2. Complete: Nombre, Precio de Venta, Costo de Compra, Stock Inicial, Stock Minimo");
            AddParagraph(body, "3. El sistema calcula el margen de ganancia automaticamente");
            AddParagraph(body, "");
            AddParagraph(body, "OPERACIONES DE STOCK:");
            AddParagraph(body, "  - Vender: Reduce stock y registra ingreso");
            AddParagraph(body, "  - Devolucion: Aumenta stock y registra gasto");
            AddParagraph(body, "  - Restock: Aumenta stock (compra a proveedor)");
            AddParagraph(body, "  - Ajuste: Corrige stock por conteo fisico (neutro financieramente)");
            AddParagraph(body, "");

            // Logs
            AddHeading(body, "6. Movimientos (Ingresos y Gastos)", 20, true);
            AddParagraph(body, "El sistema crea registros automaticos al agregar clientes, renovar o vender productos.");
            AddParagraph(body, "Para gastos e ingresos adicionales (luz, arriendo, ventas externas):");
            AddParagraph(body, "");
            AddParagraph(body, "1. Vaya a 'Movimientos' en la barra lateral");
            AddParagraph(body, "2. Escriba la Descripcion y el Monto (positivo = ingreso, negativo = gasto)");
            AddParagraph(body, "3. Haga clic en 'Registrar'");
            AddParagraph(body, "");
            AddParagraph(body, "Los registros son inmutables para proteger la integridad financiera.");
            AddParagraph(body, "");

            // Notificaciones
            AddHeading(body, "7. Notificaciones Automaticas", 20, true);
            AddParagraph(body, "El sistema revisa automaticamente cuales clientes vencen en los proximos 3 dias.");
            AddParagraph(body, "Vera un numero rojo en la campana del sidebar cuando haya notificaciones pendientes.");
            AddParagraph(body, "");
            AddParagraph(body, "- Haga clic en una notificacion para marcarla como leida");
            AddParagraph(body, "- Use 'Marcar todas leidas' para limpiar todas a la vez");
            AddParagraph(body, "- Boton 'Generar Notificaciones' para verificar en cualquier momento");
            AddParagraph(body, "");

            // Reportes
            AddHeading(body, "8. Reportes y Graficos", 20, true);
            AddParagraph(body, "Vaya a 'Reportes' para ver el desempeno financiero con mas detalle.");
            AddParagraph(body, "Graficos disponibles: Ingresos vs Gastos (barras), Nuevos Clientes (lineas), Resumen (dona).");
            AddParagraph(body, "");

            // Temas
            AddHeading(body, "9. Temas y Personalizacion", 20, true);
            AddParagraph(body, "Cuatro temas disponibles: Claro, Oscuro, Oceano y Atardecer.");
            AddParagraph(body, "Cambie el tema desde el icono en la esquina superior derecha.");
            AddParagraph(body, "El sistema recuerda su preferencia automaticamente.");
            AddParagraph(body, "");

            // Soporte
            AddHeading(body, "10. Soporte y Ayuda", 20, true);
            AddParagraph(body, "Si tiene alguna pregunta o necesita ayuda:");
            AddParagraph(body, "- WhatsApp: La forma mas rapida de contactarnos (recomendado)");
            AddParagraph(body, "- Email: Respuesta en maximo 24 horas habiles");
            AddParagraph(body, "- Sugerencias: Use el buzon dentro de su panel para enviar ideas");
            AddParagraph(body, "");

            AddParagraph(body, "Gracias por confiar en My-Negocio. Estamos aqui para ayudarle a crecer!");
            AddParagraph(body, "Bienvenido a la familia My-Negocio.");
            AddParagraph(body, "");
            AddParagraph(body, "-- El equipo de My-Negocio");

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    // ═══════════════════════════════════════════════════════════
    // GUIA: ARTESANAL (barberias, spas, salones de belleza)
    // ═══════════════════════════════════════════════════════════

    private static byte[] GenerarGuiaArtesanal(string nombreNegocio, string nombreDueno, string email, string telefono)
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            AddHeading(body, "Guia de Inicio — My-Negocio Artesanal", 28, true);
            AddParagraph(body, "Para Barberias, Spas y Salones de Belleza");
            AddParagraph(body, "");

            // Diagrama del dashboard
            AddHeading(body, "Vista General del Dashboard", 16, true);
            AddParagraph(body, "+------------------------------------------------------------------+");
            AddParagraph(body, "|  MY-NEGOCIO                    [Topbar: Nombre / Campana / Tema] |");
            AddParagraph(body, "+----------------+-------------------------------------------------+");
            AddParagraph(body, "|  BARRA LATERAL |   AREA DE CONTENIDO PRINCIPAL                   |");
            AddParagraph(body, "|                |   [Citas Hoy: 8] [Ingresos: $120] [Compl.: 6]  |");
            AddParagraph(body, "|  > Agenda      |                                                 |");
            AddParagraph(body, "|    Clientes    |   TABS: [Todos] [Ana L.] [Juan M.] [Sofia R.]   |");
            AddParagraph(body, "|    Servicios   |   [Nueva Cita]       [Dia] [Semana] [Mes]       |");
            AddParagraph(body, "|    Empleados   |   +---------------------------------------------+|");
            AddParagraph(body, "|    Inventario  |   |            CALENDARIO FullCalendar          ||");
            AddParagraph(body, "|    Reportes    |   |  08:00  [Corte - Ana]                       ||");
            AddParagraph(body, "|    Recibos     |   |  09:00  [Manicure - Sofia]                  ||");
            AddParagraph(body, "|    Notific.    |   |  10:00  [Tinte - Juan]                      ||");
            AddParagraph(body, "|    Sugerencias |   |  ------- linea roja (hora actual) --------- ||");
            AddParagraph(body, "|                |   |  11:00  (disponible)                        ||");
            AddParagraph(body, "|  [Cerrar Ses.] |   +---------------------------------------------+|");
            AddParagraph(body, "+----------------+-------------------------------------------------+");
            AddParagraph(body, "");

            // Bienvenida
            AddHeading(body, "1. Bienvenida", 20, true);
            AddHeading(body, $"Bienvenido, {nombreDueno}!", 18, false);
            AddParagraph(body, $"Le damos la mas cordial bienvenida a My-Negocio Artesanal, disenado para barberias, spas, salones de belleza y negocios de servicios con citas.");
            AddParagraph(body, "");
            AddParagraph(body, "Con My-Negocio Artesanal usted puede:");
            AddParagraph(body, "- Gestionar la agenda de citas de todo su equipo en un calendario visual");
            AddParagraph(body, "- Crear citas en segundos para clientes registrados o citas rapidas sin previa reserva");
            AddParagraph(body, "- Configurar horarios de trabajo de cada empleado con excepciones (vacaciones, dias libres)");
            AddParagraph(body, "- Mantener un catalogo de servicios con precio y duracion");
            AddParagraph(body, "- Registrar pagos con soporte para efectivo, tarjeta y transferencia, con propinas");
            AddParagraph(body, "- Controlar inventario de productos (shampoos, tintes, cremas)");
            AddParagraph(body, "- Ver reportes financieros: ingresos, gastos y ganancia neta");
            AddParagraph(body, "");

            // Credenciales
            AddHeading(body, "2. Primeros Pasos", 20, true);
            AddParagraph(body, $"Email: {email}");
            AddParagraph(body, $"Telefono: {telefono}");
            AddParagraph(body, "Puede iniciar sesion con su email O su numero de telefono.");
            AddParagraph(body, "IMPORTANTE: Cambie su contrasena despues del primer inicio de sesion.");
            AddParagraph(body, "");

            // Servicios
            AddHeading(body, "3. Configurar sus Servicios", 20, true);
            AddParagraph(body, "ANTES de crear citas, configure los servicios que ofrece:");
            AddParagraph(body, "1. Vaya a 'Servicios' en la barra lateral");
            AddParagraph(body, "2. Haga clic en 'Nuevo Servicio'");
            AddParagraph(body, "3. Complete: Nombre, Precio, Duracion (minutos), Descripcion");
            AddParagraph(body, "   Ejemplo: 'Corte de Cabello', $8.00, 30 minutos");
            AddParagraph(body, "4. La DURACION es MUY IMPORTANTE: el sistema la usa para calcular horarios disponibles");
            AddParagraph(body, "5. Puede activar/desactivar servicios sin eliminarlos");
            AddParagraph(body, "");

            // Empleados
            AddHeading(body, "4. Registrar sus Empleados", 20, true);
            AddParagraph(body, "Cada cita se asigna a un empleado especifico:");
            AddParagraph(body, "1. Vaya a 'Empleados' en la barra lateral");
            AddParagraph(body, "2. Haga clic en 'Nuevo Empleado'");
            AddParagraph(body, "3. Complete: Nombre, Apellido, Telefono, Email, Especialidad");
            AddParagraph(body, "4. El sistema crea horario por defecto: Lunes a Sabado 09:00-17:00, Domingo libre");
            AddParagraph(body, "5. Cada empleado tiene un color unico en el calendario");
            AddParagraph(body, "");

            // Horarios
            AddHeading(body, "5. Configurar Horarios de Trabajo", 20, true);
            AddParagraph(body, "Los horarios controlan cuando cada empleado puede recibir citas:");
            AddParagraph(body, "1. En la tarjeta del empleado, haga clic en el icono de reloj");
            AddParagraph(body, "2. Active/desactive cada dia de la semana");
            AddParagraph(body, "3. Configure hora de inicio y fin para cada dia (formato 24h)");
            AddParagraph(body, "4. Haga clic en 'Guardar Horario'");
            AddParagraph(body, "");
            AddParagraph(body, "EXCEPCIONES (vacaciones, dias libres puntuales):");
            AddParagraph(body, "- Busque el boton de excepciones en la tarjeta del empleado");
            AddParagraph(body, "- Seleccione la fecha y marque como 'Dia libre' u 'Horario especial'");
            AddParagraph(body, "- Las excepciones tienen prioridad sobre el horario semanal");
            AddParagraph(body, "");

            // Clientes
            AddHeading(body, "6. Gestion de Clientes", 20, true);
            AddParagraph(body, "Registrar clientes le permite agendar mas rapido y ver su historial:");
            AddParagraph(body, "1. Vaya a 'Clientes' en la barra lateral");
            AddParagraph(body, "2. Haga clic en 'Nuevo Cliente'");
            AddParagraph(body, "3. Complete: Nombre, Apellido, Telefono, Email, Direccion");
            AddParagraph(body, "4. Puede buscar clientes por nombre o telefono en la tabla");
            AddParagraph(body, "");

            // Agenda
            AddHeading(body, "7. La Agenda (Calendario de Citas)", 20, true);
            AddParagraph(body, "La Agenda es la funcion principal. Es un calendario visual con todas las citas.");
            AddParagraph(body, "");
            AddParagraph(body, "VISTAS: Dia (detallada), Semana (panoramica), Mes (planificacion)");
            AddParagraph(body, "PESTANAS DE EMPLEADOS: 'Todos' muestra todas las citas; cada empleado tiene su pestana.");
            AddParagraph(body, "LINEA ROJA: Indica la hora actual en tiempo real.");
            AddParagraph(body, "");
            AddParagraph(body, "COLORES DE ESTADO:");
            AddParagraph(body, "  - Pendiente: cita nueva, aun no confirmada");
            AddParagraph(body, "  - Confirmada: confirmada por el negocio o cliente");
            AddParagraph(body, "  - En Progreso: el servicio se esta realizando");
            AddParagraph(body, "  - Completada: servicio terminado exitosamente");
            AddParagraph(body, "  - Cancelada: cita cancelada (requiere motivo)");
            AddParagraph(body, "");

            // Citas
            AddHeading(body, "8. Como Crear y Gestionar Citas", 20, true);
            AddParagraph(body, "CREAR CITA:");
            AddParagraph(body, "1. Haga clic en 'Nueva Cita'");
            AddParagraph(body, "2. Seleccione: Empleado, Servicio, Cliente (buscar existente o crear nuevo), Fecha");
            AddParagraph(body, "3. El sistema calcula automaticamente los turnos disponibles");
            AddParagraph(body, "4. Seleccione el horario deseado y haga clic en 'Agendar Cita'");
            AddParagraph(body, "");
            AddParagraph(body, "CITA RAPIDA (cliente sin reserva):");
            AddParagraph(body, "- Haga clic y arrastre en un bloque horario libre del calendario");
            AddParagraph(body, "- El formulario se abre con la hora preseleccionada");
            AddParagraph(body, "");
            AddParagraph(body, "CICLO DE VIDA:");
            AddParagraph(body, "  Pendiente -> Confirmada -> En Progreso -> Completada -> Registrar Pago");
            AddParagraph(body, "");
            AddParagraph(body, "REGISTRAR PAGO (al completar cita):");
            AddParagraph(body, "1. Haga clic en la cita completada");
            AddParagraph(body, "2. Seleccione metodo de pago: Efectivo, Tarjeta o Transferencia");
            AddParagraph(body, "3. Agregue cargos extra, propina o marque como regalo si aplica");
            AddParagraph(body, "4. El sistema calcula el total automaticamente");
            AddParagraph(body, "");
            AddParagraph(body, "REPROGRAMAR: Arrastre la cita a otro horario en el calendario.");
            AddParagraph(body, "CANCELAR: Haga clic en la cita y seleccione 'Cancelar' (motivo obligatorio).");
            AddParagraph(body, "");

            // Inventario
            AddHeading(body, "9. Inventario de Productos", 20, true);
            AddParagraph(body, "Controle los productos que usa y vende: shampoos, tintes, cremas, esmaltes.");
            AddParagraph(body, "Operaciones: Vender, Devolucion, Restock, Ajuste de Stock.");
            AddParagraph(body, "Exporte a Excel con el boton 'Exportar'.");
            AddParagraph(body, "");

            // Reportes
            AddHeading(body, "10. Reportes Financieros", 20, true);
            AddParagraph(body, "Vaya a 'Reportes' para ver: Ingresos, Gastos y Ganancia Neta.");
            AddParagraph(body, "Filtre por Dia, Semana o Mes. Graficos de barras y dona incluidos.");
            AddParagraph(body, "");

            // Temas
            AddHeading(body, "11. Temas Visuales", 20, true);
            AddParagraph(body, "Cuatro temas: Claro, Oscuro, Oceano, Atardecer.");
            AddParagraph(body, "Cambie desde el icono en la barra superior. Se guarda automaticamente.");
            AddParagraph(body, "");

            // Guia rapida
            AddHeading(body, "12. Guia Rapida de Referencia", 20, true);
            AddParagraph(body, "Flujo recomendado para comenzar:");
            AddParagraph(body, "  1. Crear empleados");
            AddParagraph(body, "  2. Crear servicios con duracion correcta");
            AddParagraph(body, "  3. Configurar horarios");
            AddParagraph(body, "  4. Registrar clientes frecuentes");
            AddParagraph(body, "  5. Empezar a agendar citas");
            AddParagraph(body, "");

            // Soporte
            AddHeading(body, "13. Soporte", 20, true);
            AddParagraph(body, "- WhatsApp: Respuesta rapida (recomendado)");
            AddParagraph(body, "- Email: Respuesta en maximo 24 horas");
            AddParagraph(body, "- Sugerencias: Buzon dentro del sistema");
            AddParagraph(body, "");

            AddParagraph(body, "Gracias por elegir My-Negocio Artesanal. Estamos aqui para usted.");
            AddParagraph(body, "");
            AddParagraph(body, "-- El equipo de My-Negocio");

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    // ═══════════════════════════════════════════════════════════
    // GUIA: TIENDA (retail, tiendas con POS)
    // ═══════════════════════════════════════════════════════════

    private static byte[] GenerarGuiaTienda(string nombreNegocio, string nombreDueno, string email, string telefono)
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            AddHeading(body, "Guia de Inicio — My-Negocio Tienda", 28, true);
            AddParagraph(body, "Su sistema completo de Punto de Venta e Inventario");
            AddParagraph(body, "");

            // Diagrama del POS
            AddHeading(body, "Vista General del Punto de Venta (POS)", 16, true);
            AddParagraph(body, "+------------------------------------------------------------------+");
            AddParagraph(body, "|  MY-NEGOCIO                    [Topbar: Nombre / Campana / Tema] |");
            AddParagraph(body, "+----------------+------------------------+------------------------+");
            AddParagraph(body, "|  BARRA LATERAL | PRODUCTOS              | FACTURA                |");
            AddParagraph(body, "|                | [Buscar producto...  ] | Cliente: [nombre]      |");
            AddParagraph(body, "|  > POS         | [Todos][Bebidas]       |                        |");
            AddParagraph(body, "|    Inventario  | [Lacteos][Licores]     | Leche x2       $3.00   |");
            AddParagraph(body, "|    Proveedores |                        | Queso x1       $2.25   |");
            AddParagraph(body, "|    Catalogo    | +------+ +------+      |                        |");
            AddParagraph(body, "|    Movimientos | |Leche | |Queso |      | Subtotal:      $5.25   |");
            AddParagraph(body, "|    Reportes    | |$1.50 | |$2.25 |      | IVA [0]%:      $0.00   |");
            AddParagraph(body, "|    Recibos     | |Stk:8 | |Stk:5 |      | Descuento:    -$0.00   |");
            AddParagraph(body, "|    Notific.    | +------+ +------+      |                        |");
            AddParagraph(body, "|    Sugerencias |                        | TOTAL:         $5.25   |");
            AddParagraph(body, "|                |                        | [    COBRAR    ]       |");
            AddParagraph(body, "+----------------+------------------------+------------------------+");
            AddParagraph(body, "");

            // Bienvenida
            AddHeading(body, "1. Bienvenida", 20, true);
            AddHeading(body, $"Bienvenido, {nombreDueno}!", 18, false);
            AddParagraph(body, $"Bienvenido a My-Negocio, su sistema inteligente de gestion comercial para {nombreNegocio}.");
            AddParagraph(body, "");
            AddParagraph(body, "Con My-Negocio Tienda usted puede:");
            AddParagraph(body, "- Punto de Venta (POS) completo: procese ventas desde cualquier dispositivo");
            AddParagraph(body, "- Control de inventario en tiempo real con alertas de stock bajo");
            AddParagraph(body, "- Catalogo digital compartible por WhatsApp, email o enlace publico");
            AddParagraph(body, "- Reportes financieros con graficos de ingresos, gastos y ganancia");
            AddParagraph(body, "- Recibos digitales generados automaticamente en cada venta");
            AddParagraph(body, "- Exportacion a Excel de inventario y movimientos");
            AddParagraph(body, "- Gestion de categorias con Drag & Drop (arrastrar y soltar)");
            AddParagraph(body, "");

            // Credenciales
            AddHeading(body, "2. Primeros Pasos", 20, true);
            AddParagraph(body, $"Email: {email}");
            AddParagraph(body, $"Telefono: {telefono}");
            AddParagraph(body, "Puede iniciar sesion con su email O su numero de telefono.");
            AddParagraph(body, "IMPORTANTE: Cambie su contrasena despues del primer inicio de sesion.");
            AddParagraph(body, "");

            // Dashboard
            AddHeading(body, "3. Su Dashboard", 20, true);
            AddParagraph(body, "BARRA LATERAL — Secciones disponibles:");
            AddParagraph(body, "  - POS: El punto de venta para procesar ventas");
            AddParagraph(body, "  - Inventario: Gestion de productos, categorias y stock");
            AddParagraph(body, "  - Proveedores: Registro de sus proveedores");
            AddParagraph(body, "  - Catalogo: Su catalogo digital compartible");
            AddParagraph(body, "  - Movimientos: Registro de ingresos y gastos manuales");
            AddParagraph(body, "  - Reportes: Estadisticas financieras");
            AddParagraph(body, "  - Recibos: Historial de recibos generados");
            AddParagraph(body, "  - Notificaciones: Alertas del sistema");
            AddParagraph(body, "  - Sugerencias: Canal directo para enviar ideas");
            AddParagraph(body, "");
            AddParagraph(body, "TARJETAS EN EL POS:");
            AddParagraph(body, "  - Ventas Hoy / Ingresos Hoy / Productos / Stock Bajo");
            AddParagraph(body, "");

            // POS
            AddHeading(body, "4. El Punto de Venta (POS)", 20, true);
            AddParagraph(body, "El POS es la funcion mas usada. Desde aqui procesa todas las ventas.");
            AddParagraph(body, "");
            AddParagraph(body, "PASO 1 — BUSCAR PRODUCTOS:");
            AddParagraph(body, "  - Escriba el nombre en la barra de busqueda (filtrado en tiempo real)");
            AddParagraph(body, "  - O haga clic en los botones de categoria para filtrar por grupo");
            AddParagraph(body, "");
            AddParagraph(body, "PASO 2 — AGREGAR AL CARRITO:");
            AddParagraph(body, "  - Haga clic en la tarjeta del producto para agregarlo");
            AddParagraph(body, "  - Clic multiple incrementa la cantidad");
            AddParagraph(body, "  - Use '+' y '-' en el carrito para ajustar cantidades");
            AddParagraph(body, "  - El sistema NO permite vender mas unidades que el stock disponible");
            AddParagraph(body, "");
            AddParagraph(body, "PASO 3 — CLIENTE (opcional):");
            AddParagraph(body, "  - Escriba el nombre del cliente si desea que aparezca en el recibo");
            AddParagraph(body, "  - Si lo deja en blanco, el recibo dira 'Mostrador'");
            AddParagraph(body, "");
            AddParagraph(body, "PASO 4 — IVA Y DESCUENTO:");
            AddParagraph(body, "  - IVA (%): Ingrese el porcentaje si aplica (ej: 15)");
            AddParagraph(body, "  - Descuento (%): Ingrese el porcentaje de descuento si aplica");
            AddParagraph(body, "  - El total se calcula automaticamente");
            AddParagraph(body, "");
            AddParagraph(body, "PASO 5 — COBRAR:");
            AddParagraph(body, "  - Haga clic en 'Cobrar' -> Confirme el pago");
            AddParagraph(body, "  - El stock se descuenta automaticamente");
            AddParagraph(body, "  - Se genera un recibo profesional con detalle de productos");
            AddParagraph(body, "");

            // Inventario
            AddHeading(body, "5. Gestion de Inventario", 20, true);
            AddParagraph(body, "AGREGAR PRODUCTO:");
            AddParagraph(body, "1. Haga clic en 'Nuevo Producto'");
            AddParagraph(body, "2. Complete: Nombre, Precio de Venta, Costo de Compra, Stock Inicial");
            AddParagraph(body, "3. Opcionalmente: Stock Minimo (alerta), Categoria, Foto del producto");
            AddParagraph(body, "4. El sistema muestra el margen de ganancia automaticamente");
            AddParagraph(body, "");
            AddParagraph(body, "CATEGORIAS (Drag & Drop):");
            AddParagraph(body, "1. Haga clic en 'Nueva Categoria' y escriba el nombre");
            AddParagraph(body, "2. Arrastre productos sobre las carpetas de categoria para organizarlos");
            AddParagraph(body, "3. Las categorias aparecen como filtros en el POS");
            AddParagraph(body, "");
            AddParagraph(body, "OPERACIONES DE STOCK:");
            AddParagraph(body, "  - Vender: Reduce stock (venta directa fuera del POS)");
            AddParagraph(body, "  - Devolucion: Aumenta stock por devolucion de cliente");
            AddParagraph(body, "  - Restock: Aumenta stock (compra a proveedor)");
            AddParagraph(body, "  - Ajuste: Corrige stock por conteo fisico");
            AddParagraph(body, "");
            AddParagraph(body, "HISTORIAL: Boton 'Historial' muestra todos los movimientos de stock.");
            AddParagraph(body, "EXPORTAR: Boton 'Exportar' descarga Excel con productos y movimientos.");
            AddParagraph(body, "");

            // Catalogo
            AddHeading(body, "6. Catalogo Digital", 20, true);
            AddParagraph(body, "Comparta su catalogo de productos profesionalmente:");
            AddParagraph(body, "1. Vaya a 'Catalogo' en la barra lateral");
            AddParagraph(body, "2. Elija un estilo visual: Moderno, Elegante, Minimalista, Vibrante o Clasico");
            AddParagraph(body, "3. Opciones para compartir:");
            AddParagraph(body, "   - Descargar PDF para imprimir");
            AddParagraph(body, "   - Abrir Link Publico (cualquiera puede verlo sin cuenta)");
            AddParagraph(body, "   - Enviar por WhatsApp");
            AddParagraph(body, "   - Enviar por Email");
            AddParagraph(body, "");
            AddParagraph(body, "Consejo: Comparta el enlace publico en redes sociales para que sus clientes vean precios actualizados.");
            AddParagraph(body, "");

            // Reportes
            AddHeading(body, "7. Reportes de Ventas", 20, true);
            AddParagraph(body, "Vaya a 'Reportes' para ver: Ingresos, Gastos y Ganancia Neta.");
            AddParagraph(body, "Filtre por Dia, Semana o Mes. Graficos de barras incluidos.");
            AddParagraph(body, "");

            // Temas
            AddHeading(body, "8. Temas Visuales", 20, true);
            AddParagraph(body, "Cuatro temas: Claro, Oscuro, Oceano, Atardecer.");
            AddParagraph(body, "Cambie desde el icono en la barra superior. Se guarda automaticamente.");
            AddParagraph(body, "");

            // Soporte
            AddHeading(body, "9. Soporte", 20, true);
            AddParagraph(body, "- WhatsApp: La forma mas rapida de contactarnos (recomendado)");
            AddParagraph(body, "- Email: Respuesta en maximo 24 horas");
            AddParagraph(body, "- Sugerencias: Buzon dentro del sistema");
            AddParagraph(body, "");

            AddParagraph(body, "Gracias por confiar en My-Negocio. Su exito es nuestra prioridad!");
            AddParagraph(body, "");
            AddParagraph(body, "-- El equipo de My-Negocio");

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    // ═══════════════════════════════════════════════════════════
    // GUIA: RESTAURANTE (restaurantes con POS y mesas)
    // ═══════════════════════════════════════════════════════════

    private static byte[] GenerarGuiaRestaurante(string nombreNegocio, string nombreDueno, string email, string telefono)
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            AddHeading(body, "Guia de Inicio — My-Negocio Restaurante", 28, true);
            AddParagraph(body, "Su sistema completo de POS, Mesas y Menu Digital");
            AddParagraph(body, "");

            // Diagrama del POS restaurante
            AddHeading(body, "Vista General del Punto de Venta (POS)", 16, true);
            AddParagraph(body, "+------------------------------------------------------------------+");
            AddParagraph(body, "|  MY-NEGOCIO                    [Topbar: Nombre / Campana / Tema] |");
            AddParagraph(body, "+----------------+------------------------+------------------------+");
            AddParagraph(body, "|  BARRA LATERAL | PLATOS                 | FACTURA                |");
            AddParagraph(body, "|                | [Buscar plato...     ] | [Local][Llevar][Deliv] |");
            AddParagraph(body, "|  > POS         | [Todos][Entradas]      | Mesa: [---v]           |");
            AddParagraph(body, "|    Mesas       | [Platos F.][Bebidas]   | Cliente: [nombre]      |");
            AddParagraph(body, "|    Menus       | [Postres]              |                        |");
            AddParagraph(body, "|    Inventario  | +------+ +------+      | Sopa del dia x1  $4.50 |");
            AddParagraph(body, "|    Distribuid. | |Sopa  | |Arroz |      | Jugo natural x2  $5.00 |");
            AddParagraph(body, "|    Repartidor. | |$4.50 | |$7.50 |      |                        |");
            AddParagraph(body, "|    Movimientos | +------+ +------+      | Subtotal:        $9.50 |");
            AddParagraph(body, "|    Reportes    |                        | IVA [12]%:       $1.14 |");
            AddParagraph(body, "|    Recibos     |                        | TOTAL:          $10.64 |");
            AddParagraph(body, "|    Notific.    |                        | [    COBRAR    ]       |");
            AddParagraph(body, "+----------------+------------------------+------------------------+");
            AddParagraph(body, "");

            // Bienvenida
            AddHeading(body, "1. Bienvenida", 20, true);
            AddHeading(body, $"Bienvenido, {nombreDueno}!", 18, false);
            AddParagraph(body, $"Bienvenido a My-Negocio Restaurante, la plataforma disenada para restaurantes como {nombreNegocio}.");
            AddParagraph(body, "");
            AddParagraph(body, "Con My-Negocio Restaurante usted puede:");
            AddParagraph(body, "- Tomar pedidos y cobrar en segundos con el Punto de Venta (POS)");
            AddParagraph(body, "- Gestionar mesas en tiempo real (libre, ocupada, reservada)");
            AddParagraph(body, "- Crear y compartir menus digitales por WhatsApp o email");
            AddParagraph(body, "- Registrar ordenes para consumo local, para llevar y delivery");
            AddParagraph(body, "- Controlar inventario de platos e insumos con alertas de stock bajo");
            AddParagraph(body, "- Generar reportes de ventas e ingresos por dia, semana o mes");
            AddParagraph(body, "- Administrar distribuidores (proveedores) y repartidores");
            AddParagraph(body, "");

            // Credenciales
            AddHeading(body, "2. Primeros Pasos", 20, true);
            AddParagraph(body, $"Email: {email}");
            AddParagraph(body, $"Telefono: {telefono}");
            AddParagraph(body, "Puede iniciar sesion con su email O su numero de telefono.");
            AddParagraph(body, "IMPORTANTE: Cambie su contrasena despues del primer inicio de sesion.");
            AddParagraph(body, "");

            // Dashboard
            AddHeading(body, "3. Su Dashboard", 20, true);
            AddParagraph(body, "BARRA LATERAL — Secciones disponibles:");
            AddParagraph(body, "  - POS: Punto de venta para tomar pedidos");
            AddParagraph(body, "  - Mesas: Vista del salon con estado de cada mesa");
            AddParagraph(body, "  - Menus: Creador de menus digitales compartibles");
            AddParagraph(body, "  - Inventario: Listado y control de platos e insumos");
            AddParagraph(body, "  - Distribuidores: Proveedores de ingredientes");
            AddParagraph(body, "  - Repartidores: Equipo de delivery");
            AddParagraph(body, "  - Movimientos: Ingresos y gastos manuales");
            AddParagraph(body, "  - Reportes: Graficos y estadisticas de ventas");
            AddParagraph(body, "  - Recibos: Historial de facturas generadas");
            AddParagraph(body, "");
            AddParagraph(body, "TARJETAS EN EL POS:");
            AddParagraph(body, "  - Ventas Hoy / Ingresos Hoy / Platos / Stock Bajo");
            AddParagraph(body, "");

            // POS
            AddHeading(body, "4. El Punto de Venta (POS)", 20, true);
            AddParagraph(body, "PASO 1 — TIPO DE ORDEN:");
            AddParagraph(body, "  - Local: Para consumir en el restaurante (requiere asignar mesa)");
            AddParagraph(body, "  - Para Llevar: El cliente retira en caja");
            AddParagraph(body, "  - Delivery: Se envia a domicilio (asignar repartidor y direccion)");
            AddParagraph(body, "");
            AddParagraph(body, "PASO 2 — SELECCIONAR MESA (solo si es Local):");
            AddParagraph(body, "  - Use el selector desplegable para elegir la mesa del cliente");
            AddParagraph(body, "");
            AddParagraph(body, "PASO 3 — AGREGAR PLATOS:");
            AddParagraph(body, "  - Busque por nombre o filtre por categoria (Entradas, Platos Fuertes, etc.)");
            AddParagraph(body, "  - Haga clic en la tarjeta del plato para agregarlo a la orden");
            AddParagraph(body, "  - Use '+' y '-' para ajustar cantidades en la factura");
            AddParagraph(body, "");
            AddParagraph(body, "PASO 4 — IVA Y DESCUENTO:");
            AddParagraph(body, "  - IVA (%): Ingrese el porcentaje si aplica (ej: 12)");
            AddParagraph(body, "  - Descuento (%): Ingrese el porcentaje si aplica");
            AddParagraph(body, "");
            AddParagraph(body, "PASO 5 — COBRAR:");
            AddParagraph(body, "  - Haga clic en 'Cobrar' -> Confirme el pago");
            AddParagraph(body, "  - Se genera recibo automaticamente");
            AddParagraph(body, "");
            AddParagraph(body, "DELIVERY: Use el boton 'Llamar Delivery' para abrir WhatsApp con el repartidor.");
            AddParagraph(body, "");

            // Mesas
            AddHeading(body, "5. Gestion de Mesas", 20, true);
            AddParagraph(body, "Vista en tiempo real del salon de su restaurante.");
            AddParagraph(body, "");
            AddParagraph(body, "COLORES DE ESTADO:");
            AddParagraph(body, "  - Verde (Libre): Mesa disponible para nuevos clientes");
            AddParagraph(body, "  - Rojo (Ocupada): Mesa con clientes sentados");
            AddParagraph(body, "  - Amarillo (Reservada): Mesa con reserva, no asignar a otros");
            AddParagraph(body, "");
            AddParagraph(body, "AGREGAR MESA:");
            AddParagraph(body, "1. Haga clic en 'Nueva Mesa'");
            AddParagraph(body, "2. Complete: Nombre (ej: 'Mesa 1', 'Terraza 2'), Numero, Capacidad");
            AddParagraph(body, "3. Use nombres descriptivos como su distribucion fisica real");
            AddParagraph(body, "");
            AddParagraph(body, "CAMBIAR ESTADO: Haga clic en la mesa para alternar entre Libre, Ocupada, Reservada.");
            AddParagraph(body, "");
            AddParagraph(body, "FLUJO TIPICO:");
            AddParagraph(body, "  Llegan clientes -> Ocupada -> Cobrar en POS -> Libre");
            AddParagraph(body, "  Reservacion telefonica -> Reservada -> Llegan -> Ocupada -> Libre");
            AddParagraph(body, "");

            // Menus digitales
            AddHeading(body, "6. Menus Digitales Compartibles", 20, true);
            AddParagraph(body, "Cree menus profesionales que sus clientes pueden ver en su celular:");
            AddParagraph(body, "1. Vaya a 'Menus' en la barra lateral");
            AddParagraph(body, "2. Haga clic en 'Nuevo Menu'");
            AddParagraph(body, "3. Escriba un nombre (ej: 'Menu del Dia', 'Carta General')");
            AddParagraph(body, "4. Seleccione el tipo: General, Desayuno, Almuerzo o Cena");
            AddParagraph(body, "   - Almuerzo y Cena permiten precio fijo de combo");
            AddParagraph(body, "5. Agregue secciones (Entradas, Sopa, Segundo, Bebida, Postre)");
            AddParagraph(body, "6. Agregue platos a cada seccion desde el panel izquierdo");
            AddParagraph(body, "7. Elija estilo visual: Moderno, Elegante, Minimalista, Vibrante o Clasico");
            AddParagraph(body, "8. Guarde y comparta por WhatsApp, Email, Link Publico o Imprima");
            AddParagraph(body, "");

            // Inventario
            AddHeading(body, "7. Inventario de Platos", 20, true);
            AddParagraph(body, "AGREGAR PLATO:");
            AddParagraph(body, "1. Vaya a 'Inventario' -> 'Nuevo Plato'");
            AddParagraph(body, "2. Complete: Nombre, Precio de Venta, Costo, Stock, Categoria, Foto (opcional)");
            AddParagraph(body, "3. Agregue Receta/Ingredientes para uso interno del personal");
            AddParagraph(body, "");
            AddParagraph(body, "CATEGORIAS: Cree categorias (Entradas, Platos Fuertes, Bebidas, Postres)");
            AddParagraph(body, "y arrastre platos a las carpetas para organizarlos.");
            AddParagraph(body, "");
            AddParagraph(body, "OPERACIONES DE STOCK: Vender, Devolucion, Restock, Ajuste de Stock.");
            AddParagraph(body, "EXPORTAR: Descargue Excel con todos sus platos y movimientos.");
            AddParagraph(body, "");

            // Distribuidores y repartidores
            AddHeading(body, "8. Distribuidores y Repartidores", 20, true);
            AddParagraph(body, "DISTRIBUIDORES (proveedores):");
            AddParagraph(body, "- Vaya a 'Distribuidores' -> 'Nuevo Distribuidor'");
            AddParagraph(body, "- Complete: Nombre de la compania, Contacto, Telefono, Email");
            AddParagraph(body, "");
            AddParagraph(body, "REPARTIDORES (delivery):");
            AddParagraph(body, "- Vaya a 'Repartidores' -> 'Nuevo Repartidor'");
            AddParagraph(body, "- Complete: Nombre, Apellido, Telefono, Vehiculo (Moto/Bici/Auto/A pie)");
            AddParagraph(body, "- El repartidor aparecera en el selector del POS al elegir Delivery");
            AddParagraph(body, "- Use 'Llamar Delivery' para abrir WhatsApp con el repartidor directamente");
            AddParagraph(body, "");

            // Reportes
            AddHeading(body, "9. Reportes Financieros", 20, true);
            AddParagraph(body, "Vaya a 'Reportes' para ver: Ingresos, Gastos y Ganancia Neta.");
            AddParagraph(body, "Filtre por Dia, Semana o Mes. Graficos de barras incluidos.");
            AddParagraph(body, "Consejo: Revise al cierre de cada semana para identificar tendencias.");
            AddParagraph(body, "");

            // Temas
            AddHeading(body, "10. Temas Visuales", 20, true);
            AddParagraph(body, "Cuatro temas: Claro, Oscuro, Oceano, Atardecer.");
            AddParagraph(body, "Cambie desde el icono en la barra superior. Se guarda automaticamente.");
            AddParagraph(body, "");

            // Guia rapida
            AddHeading(body, "11. Primeros 30 Minutos — Guia Rapida", 20, true);
            AddParagraph(body, "1. Inicie sesion y cambie su contrasena");
            AddParagraph(body, "2. Vaya a Inventario y cree sus categorias (Entradas, Platos Fuertes, Bebidas, etc.)");
            AddParagraph(body, "3. Registre sus platos con nombre, precio y categoria");
            AddParagraph(body, "4. Vaya a Mesas y configure todas las mesas de su salon");
            AddParagraph(body, "5. Si tiene delivery, registre a sus repartidores");
            AddParagraph(body, "6. Regrese al POS y procese su primera orden de prueba");
            AddParagraph(body, "7. Opcionalmente, cree su primer menu digital y compartalo");
            AddParagraph(body, "");

            // Soporte
            AddHeading(body, "12. Soporte", 20, true);
            AddParagraph(body, "- WhatsApp: La forma mas rapida de contactarnos (recomendado)");
            AddParagraph(body, "- Email: Respuesta en maximo 24 horas");
            AddParagraph(body, "- Sugerencias: Buzon dentro del sistema");
            AddParagraph(body, "");

            AddParagraph(body, "Gracias por confiar en My-Negocio. Mucho exito con su restaurante!");
            AddParagraph(body, "");
            AddParagraph(body, "-- El equipo de My-Negocio");

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    // ═══════════════════════════════════════════════════════════
    // GUIA: VENDEDOR (equipo de ventas)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Genera la guia para un nuevo vendedor que se une al equipo de ventas.
    /// Incluye: como iniciar sesion, gestion de negocios, leads, comisiones y mejores practicas.
    /// </summary>
    public static byte[] GenerarGuiaVendedor(string nombreVendedor, string correo)
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            AddHeading(body, "Manual del Vendedor - My-Negocio", 28, true);
            AddParagraph(body, "");

            AddHeading(body, $"Bienvenido al equipo, {nombreVendedor}!", 22, false);
            AddParagraph(body, "Este manual te guiara en todo lo que necesitas saber para vender y gestionar negocios en la plataforma My-Negocio.");
            AddParagraph(body, "");

            AddHeading(body, "1. Tus Credenciales de Acceso", 18, true);
            AddParagraph(body, $"Email: {correo}");
            AddParagraph(body, "Puedes iniciar sesion con tu email o tu numero de telefono.");
            AddParagraph(body, "IMPORTANTE: Cambia tu contrasena despues del primer inicio de sesion.");
            AddParagraph(body, "");

            AddHeading(body, "2. Tu Panel de Vendedor", 18, true);
            AddParagraph(body, "En tu dashboard encontraras:");
            AddParagraph(body, "- Tus Negocios: Lista de todos los negocios que has creado");
            AddParagraph(body, "- Estadisticas: Total de negocios, activos, en prueba y clientes");
            AddParagraph(body, "- Leads: Prospectos interesados que llegan del sitio web");
            AddParagraph(body, "");

            AddHeading(body, "3. Como Crear un Nuevo Negocio", 18, true);
            AddParagraph(body, "1. Haz clic en 'Nuevo Negocio' en tu dashboard");
            AddParagraph(body, "2. Completa los datos del negocio:");
            AddParagraph(body, "   - Nombre del negocio");
            AddParagraph(body, "   - Nombre del dueno");
            AddParagraph(body, "   - Email y telefono");
            AddParagraph(body, "   - Contrasena inicial");
            AddParagraph(body, "   - Tipo: Membresias, Artesanal (citas), Tienda (POS) o Restaurante");
            AddParagraph(body, "3. Selecciona si es periodo de prueba o pago");
            AddParagraph(body, "4. El sistema enviara automaticamente un email de bienvenida con guia especializada");
            AddParagraph(body, "");

            AddHeading(body, "4. Gestion de Leads", 18, true);
            AddParagraph(body, "- Los leads son personas que completaron el formulario en el sitio web");
            AddParagraph(body, "- Aparecen en tu panel con un badge de notificacion");
            AddParagraph(body, "- Contacta al lead lo antes posible (los primeros en responder cierran mas ventas)");
            AddParagraph(body, "- Marca el lead como 'Atendido' una vez que lo contactes");
            AddParagraph(body, "");

            AddHeading(body, "5. Ver el Panel de un Negocio", 18, true);
            AddParagraph(body, "- Puedes 'entrar' al panel de cualquier negocio que hayas creado");
            AddParagraph(body, "- Esto te permite ayudar al cliente o verificar que todo funcione");
            AddParagraph(body, "- Usa el boton 'Entrar' junto al nombre del negocio");
            AddParagraph(body, "- Para volver a tu panel, usa el boton 'Volver al Panel de Vendedor'");
            AddParagraph(body, "");

            AddHeading(body, "6. Mejores Practicas de Ventas", 18, true);
            AddParagraph(body, "- Responde leads en menos de 5 minutos");
            AddParagraph(body, "- Ofrece siempre un periodo de prueba de 7 dias");
            AddParagraph(body, "- Acompana al cliente en su primera semana de uso");
            AddParagraph(body, "- Agenda una llamada de seguimiento a los 3 dias");
            AddParagraph(body, "- Los clientes satisfechos son tu mejor fuente de referidos");
            AddParagraph(body, "");

            AddHeading(body, "7. Soporte Interno", 18, true);
            AddParagraph(body, "Si tienes alguna duda tecnica o necesitas ayuda:");
            AddParagraph(body, "- Contacta al equipo de soporte por WhatsApp");
            AddParagraph(body, "- Escribe a soporte@my-negocio.com");
            AddParagraph(body, "");

            AddParagraph(body, "Exito en tus ventas! Confiamos en ti.");
            AddParagraph(body, "");
            AddParagraph(body, "-- El equipo de My-Negocio");

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    // ═══════════════════════════════════════════════════════════
    // HELPERS PRIVADOS PARA CONSTRUIR EL DOCUMENTO
    // ═══════════════════════════════════════════════════════════

    private static void AddHeading(Body body, string text, int fontSize, bool bold)
    {
        var paragraph = new Paragraph();
        var run = new Run();

        var runProperties = new RunProperties();
        runProperties.Append(new FontSize { Val = (fontSize * 2).ToString() });
        if (bold)
            runProperties.Append(new Bold());
        runProperties.Append(new Color { Val = "333333" });
        runProperties.Append(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" });

        run.Append(runProperties);
        run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

        paragraph.Append(run);
        body.Append(paragraph);
    }

    private static void AddParagraph(Body body, string text)
    {
        var paragraph = new Paragraph();

        if (!string.IsNullOrEmpty(text))
        {
            var run = new Run();
            var runProperties = new RunProperties();
            runProperties.Append(new FontSize { Val = "22" }); // 11pt
            runProperties.Append(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" });
            run.Append(runProperties);
            run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            paragraph.Append(run);
        }

        body.Append(paragraph);
    }
}
