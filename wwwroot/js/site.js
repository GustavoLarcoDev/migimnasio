// ============================================================
// My-Negocio — Utilidades compartidas
// Funciones globales usadas en múltiples vistas/dashboards
// ============================================================

// ── XSS helpers ──────────────────────────────────────────────
function escHtml(str) {
    if (!str) return '';
    return $('<span>').text(str).html();
}

function escAttr(str) {
    if (!str) return '';
    return escHtml(str).replace(/'/g, '&#39;');
}

// ── Saludo según hora (Ecuador UTC-5) ───────────────────────
function saludoHora() {
    var h = new Date().getHours();
    if (h < 12) return 'Buenos días';
    if (h < 18) return 'Buenas tardes';
    return 'Buenas noches';
}

// ── SweetAlert2 mixin global ────────────────────────────────
const swalMG = Swal.mixin({
    confirmButtonColor: '#3E97FF',
    cancelButtonColor: '#E4E6EF',
    cancelButtonText: 'Cancelar',
    reverseButtons: true,
    customClass: {
        popup: 'mg-swal-popup',
        confirmButton: 'mg-swal-confirm',
        cancelButton: 'mg-swal-cancel'
    }
});

// ── Modales de confirmación estándar ────────────────────────
function mgConfirmDelete(title, text, onConfirm) {
    swalMG.fire({
        title: title,
        text: text,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Sí, eliminar',
        confirmButtonColor: '#F1416C'
    }).then(function(r) { if (r.isConfirmed) onConfirm(); });
}

function mgConfirmAction(title, text, confirmText, confirmColor, onConfirm) {
    swalMG.fire({
        title: title,
        text: text,
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: confirmText,
        confirmButtonColor: confirmColor || '#3E97FF'
    }).then(function(r) { if (r.isConfirmed) onConfirm(); });
}

function mgSuccess(title, text) {
    swalMG.fire({ title: title, text: text, icon: 'success' });
}

// ── AJAX helpers con CSRF automático ────────────────────────
function mgPost(url, data, btn, onSuccess) {
    var $btn = btn ? $(btn) : null;
    var originalHtml = $btn ? $btn.html() : '';
    if ($btn) $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span>Procesando...');

    $.post(url, data)
        .done(function(res) {
            if (res.success !== undefined && !res.success) {
                toastr.error(res.message || 'Error en la operación');
            } else {
                onSuccess(res);
            }
        })
        .fail(function(xhr) {
            var msg = xhr.responseJSON?.message || 'Error del servidor';
            toastr.error(msg);
        })
        .always(function() {
            if ($btn) $btn.prop('disabled', false).html(originalHtml);
        });
}

function mgGet(url, data, onSuccess) {
    $.get(url, data)
        .done(function(res) { onSuccess(res); })
        .fail(function(xhr) {
            var msg = xhr.responseJSON?.message || 'Error al cargar datos';
            toastr.error(msg);
        });
}

// ── DataTables idioma español ───────────────────────────────
const dtLangES = {
    processing: 'Procesando...',
    search: 'Buscar:',
    lengthMenu: 'Mostrar _MENU_ registros',
    info: 'Mostrando _START_ a _END_ de _TOTAL_ registros',
    infoEmpty: 'Mostrando 0 a 0 de 0 registros',
    infoFiltered: '(filtrado de _MAX_ registros totales)',
    loadingRecords: 'Cargando...',
    zeroRecords: 'No se encontraron resultados',
    emptyTable: 'No hay datos disponibles',
    paginate: { first: '<<', previous: '<', next: '>', last: '>>' },
    aria: {
        sortAscending: ': activar para ordenar ascendente',
        sortDescending: ': activar para ordenar descendente'
    }
};

// ── Utilidades de formato ───────────────────────────────────
function fmtMoney(val) {
    return '$' + (parseFloat(val) || 0).toFixed(2);
}

function toDateInput(date) {
    if (!date) return '';
    var d = new Date(date);
    return d.getFullYear() + '-' +
        String(d.getMonth() + 1).padStart(2, '0') + '-' +
        String(d.getDate()).padStart(2, '0');
}
