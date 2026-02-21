// ============================================================
// My-Negocio — Utilidades compartidas
// Funciones globales usadas en múltiples vistas/dashboards
// ============================================================

// Escapar HTML para prevenir XSS al inyectar texto en el DOM
function escHtml(str) {
    if (!str) return '';
    return $('<span>').text(str).html();
}

// Escapar para uso en atributos HTML (onclick, title, etc.)
// Necesario porque escHtml no escapa comillas simples que rompen contexto JS
function escAttr(str) {
    if (!str) return '';
    return escHtml(str).replace(/'/g, '&#39;');
}

// Saludo según hora del día (Ecuador UTC-5)
function saludoHora() {
    var h = new Date().getHours();
    if (h < 12) return 'Buenos días';
    if (h < 18) return 'Buenas tardes';
    return 'Buenas noches';
}
