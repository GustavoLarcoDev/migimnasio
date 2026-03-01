// ═══════════════════════════════════════════════════════════
// propaganda-editor.js — Editor de disenos marketing (Fabric.js)
//
// Editor visual tipo Canva para crear flyers, posts y
// material de marketing. Usa Fabric.js para canvas interactivo.
// ═══════════════════════════════════════════════════════════

var PropagandaEditor = (function () {
    'use strict';

    var canvas = null;
    var currentDisenoId = null;
    var isDirty = false;
    var isEditorMode = false;
    var clipboard = null;

    // POST JSON helper (endpoints usan [FromBody])
    function postJson(url, data, onSuccess) {
        $.ajax({
            url: url,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            success: function (res) {
                if (res.success !== undefined && !res.success) {
                    toastr.error(res.message || 'Error en la operacion');
                } else if (onSuccess) {
                    onSuccess(res);
                }
            },
            error: function (xhr) {
                toastr.error(xhr.responseJSON?.message || 'Error del servidor');
            }
        });
    }

    // Tamanos predefinidos
    var SIZES = {
        'ig-post': { w: 1080, h: 1080, label: 'Instagram Post' },
        'ig-story': { w: 1080, h: 1920, label: 'Instagram Story' },
        'fb-post': { w: 1200, h: 630, label: 'Facebook Post' },
        'flyer-a4': { w: 2480, h: 3508, label: 'Flyer A4' },
        'custom': { w: 1080, h: 1080, label: 'Personalizado' }
    };

    // Paleta de colores My-Negocio
    var COLORS = [
        '#0047AB', '#00D4FF', '#2C3E50', '#FFFFFF', '#000000',
        '#50CD89', '#F1416C', '#FFC107', '#7239EA', '#FF6B35',
        '#E4E6EF', '#F5F8FA', '#181C32', '#3E97FF', '#009EF7'
    ];

    // Plantillas predefinidas
    var TEMPLATES = [
        {
            id: 'promo-general',
            nombre: 'Promocion General',
            thumbnail: null,
            size: 'ig-post',
            build: function (w, h) {
                return [
                    { type: 'rect', left: 0, top: 0, width: w, height: h, fill: '#0047AB', selectable: false },
                    { type: 'rect', left: 0, top: h * 0.65, width: w, height: h * 0.35, fill: '#2C3E50', selectable: false },
                    { type: 'text', text: 'TU OFERTA AQUI', left: w / 2, top: h * 0.25, fontSize: 72, fontFamily: 'Inter', fontWeight: 'bold', fill: '#FFFFFF', textAlign: 'center', originX: 'center', originY: 'center' },
                    { type: 'text', text: 'Descripcion del producto o servicio', left: w / 2, top: h * 0.42, fontSize: 36, fontFamily: 'Inter', fill: '#00D4FF', textAlign: 'center', originX: 'center', originY: 'center' },
                    { type: 'text', text: '$XX.XX', left: w / 2, top: h * 0.58, fontSize: 96, fontFamily: 'Inter', fontWeight: 'bold', fill: '#FFFFFF', textAlign: 'center', originX: 'center', originY: 'center' },
                    { type: 'text', text: 'my-negocio.com', left: w / 2, top: h * 0.88, fontSize: 28, fontFamily: 'Inter', fill: '#00D4FF', textAlign: 'center', originX: 'center', originY: 'center' }
                ];
            }
        },
        {
            id: 'descuento',
            nombre: 'Descuento',
            thumbnail: null,
            size: 'ig-post',
            build: function (w, h) {
                return [
                    { type: 'rect', left: 0, top: 0, width: w, height: h, fill: '#FFFFFF', selectable: false },
                    { type: 'circle', left: w / 2, top: h * 0.4, radius: w * 0.28, fill: '#F1416C', originX: 'center', originY: 'center' },
                    { type: 'text', text: '-50%', left: w / 2, top: h * 0.35, fontSize: 120, fontFamily: 'Inter', fontWeight: 'bold', fill: '#FFFFFF', textAlign: 'center', originX: 'center', originY: 'center' },
                    { type: 'text', text: 'OFF', left: w / 2, top: h * 0.48, fontSize: 48, fontFamily: 'Inter', fontWeight: 'bold', fill: '#FFFFFF', textAlign: 'center', originX: 'center', originY: 'center' },
                    { type: 'text', text: 'OFERTA ESPECIAL', left: w / 2, top: h * 0.1, fontSize: 48, fontFamily: 'Inter', fontWeight: 'bold', fill: '#0047AB', textAlign: 'center', originX: 'center', originY: 'center' },
                    { type: 'text', text: 'Valido por tiempo limitado', left: w / 2, top: h * 0.78, fontSize: 32, fontFamily: 'Inter', fill: '#2C3E50', textAlign: 'center', originX: 'center', originY: 'center' },
                    { type: 'text', text: 'my-negocio.com', left: w / 2, top: h * 0.9, fontSize: 28, fontFamily: 'Inter', fill: '#0047AB', textAlign: 'center', originX: 'center', originY: 'center' }
                ];
            }
        },
        {
            id: 'nuevo-producto',
            nombre: 'Nuevo Producto',
            thumbnail: null,
            size: 'ig-post',
            build: function (w, h) {
                return [
                    { type: 'rect', left: 0, top: 0, width: w, height: h, fill: '#2C3E50', selectable: false },
                    { type: 'rect', left: w * 0.05, top: h * 0.05, width: w * 0.9, height: h * 0.9, fill: 'transparent', stroke: '#00D4FF', strokeWidth: 3 },
                    { type: 'text', text: 'NUEVO', left: w / 2, top: h * 0.15, fontSize: 36, fontFamily: 'Inter', fontWeight: 'bold', fill: '#00D4FF', textAlign: 'center', originX: 'center', originY: 'center', charSpacing: 600 },
                    { type: 'text', text: 'Nombre del\nProducto', left: w / 2, top: h * 0.4, fontSize: 64, fontFamily: 'Inter', fontWeight: 'bold', fill: '#FFFFFF', textAlign: 'center', originX: 'center', originY: 'center' },
                    { type: 'rect', left: w / 2 - 40, top: h * 0.55, width: 80, height: 4, fill: '#00D4FF', originX: 'center' },
                    { type: 'text', text: 'Descripcion breve del producto\no servicio que ofreces', left: w / 2, top: h * 0.68, fontSize: 28, fontFamily: 'Inter', fill: '#E4E6EF', textAlign: 'center', originX: 'center', originY: 'center' },
                    { type: 'text', text: 'my-negocio.com', left: w / 2, top: h * 0.88, fontSize: 24, fontFamily: 'Inter', fill: '#00D4FF', textAlign: 'center', originX: 'center', originY: 'center' }
                ];
            }
        },
        {
            id: 'horario',
            nombre: 'Horario de Atencion',
            thumbnail: null,
            size: 'ig-post',
            build: function (w, h) {
                return [
                    { type: 'rect', left: 0, top: 0, width: w, height: h, fill: '#F5F8FA', selectable: false },
                    { type: 'rect', left: 0, top: 0, width: w, height: h * 0.2, fill: '#0047AB' },
                    { type: 'text', text: 'HORARIO DE ATENCION', left: w / 2, top: h * 0.1, fontSize: 42, fontFamily: 'Inter', fontWeight: 'bold', fill: '#FFFFFF', textAlign: 'center', originX: 'center', originY: 'center' },
                    { type: 'text', text: 'Lunes a Viernes\n9:00 AM - 6:00 PM\n\nSabado\n9:00 AM - 2:00 PM\n\nDomingo\nCerrado', left: w / 2, top: h * 0.55, fontSize: 36, fontFamily: 'Inter', fill: '#2C3E50', textAlign: 'center', originX: 'center', originY: 'center', lineHeight: 1.4 },
                    { type: 'text', text: 'my-negocio.com', left: w / 2, top: h * 0.92, fontSize: 24, fontFamily: 'Inter', fill: '#0047AB', textAlign: 'center', originX: 'center', originY: 'center' }
                ];
            }
        },
        {
            id: 'story-cta',
            nombre: 'Story con CTA',
            thumbnail: null,
            size: 'ig-story',
            build: function (w, h) {
                return [
                    { type: 'rect', left: 0, top: 0, width: w, height: h, fill: '#0047AB', selectable: false },
                    { type: 'rect', left: 0, top: h * 0.5, width: w, height: h * 0.5, fill: '#2C3E50', selectable: false },
                    { type: 'text', text: 'TU NEGOCIO\nMERECE\nMAS', left: w / 2, top: h * 0.25, fontSize: 72, fontFamily: 'Inter', fontWeight: 'bold', fill: '#FFFFFF', textAlign: 'center', originX: 'center', originY: 'center', lineHeight: 1.3 },
                    { type: 'rect', left: w / 2, top: h * 0.6, width: w * 0.7, height: 60, rx: 30, ry: 30, fill: '#00D4FF', originX: 'center', originY: 'center' },
                    { type: 'text', text: 'CONOCE MAS', left: w / 2, top: h * 0.6, fontSize: 28, fontFamily: 'Inter', fontWeight: 'bold', fill: '#FFFFFF', textAlign: 'center', originX: 'center', originY: 'center' },
                    { type: 'text', text: 'Gestiona tu negocio\ndesde $10/mes', left: w / 2, top: h * 0.75, fontSize: 36, fontFamily: 'Inter', fill: '#E4E6EF', textAlign: 'center', originX: 'center', originY: 'center' },
                    { type: 'text', text: 'my-negocio.com', left: w / 2, top: h * 0.9, fontSize: 28, fontFamily: 'Inter', fill: '#00D4FF', textAlign: 'center', originX: 'center', originY: 'center' }
                ];
            }
        },
        {
            id: 'testimonio',
            nombre: 'Testimonio',
            thumbnail: null,
            size: 'ig-post',
            build: function (w, h) {
                return [
                    { type: 'rect', left: 0, top: 0, width: w, height: h, fill: '#FFFFFF', selectable: false },
                    { type: 'text', text: '\u201C', left: w * 0.1, top: h * 0.15, fontSize: 200, fontFamily: 'Georgia', fill: '#00D4FF', originX: 'center', originY: 'center' },
                    { type: 'text', text: 'Escribe aqui el\ntestimonio de tu\ncliente satisfecho', left: w / 2, top: h * 0.45, fontSize: 40, fontFamily: 'Inter', fill: '#2C3E50', textAlign: 'center', originX: 'center', originY: 'center', fontStyle: 'italic', lineHeight: 1.5 },
                    { type: 'rect', left: w / 2, top: h * 0.68, width: 60, height: 4, fill: '#0047AB', originX: 'center' },
                    { type: 'text', text: 'Nombre del Cliente', left: w / 2, top: h * 0.76, fontSize: 28, fontFamily: 'Inter', fontWeight: 'bold', fill: '#0047AB', textAlign: 'center', originX: 'center', originY: 'center' },
                    { type: 'text', text: 'my-negocio.com', left: w / 2, top: h * 0.92, fontSize: 24, fontFamily: 'Inter', fill: '#E4E6EF', textAlign: 'center', originX: 'center', originY: 'center' }
                ];
            }
        }
    ];

    // ══════════════════════════════════════════════════════
    // Inicializacion
    // ══════════════════════════════════════════════════════

    function init() {
        renderGallery();
        loadDesigns();
    }

    function renderGallery() {
        var container = document.getElementById('propagandaGalleryContent');
        if (!container) return;

        container.innerHTML =
            '<div class="row g-3 mb-4" id="propagandaDesignsList">' +
            '   <div class="col-12 text-center py-4"><div class="spinner-border text-primary"></div></div>' +
            '</div>';
    }

    function loadDesigns() {
        $.get('/Negocios/GetDisenosEditor', function (data) {
            renderDesignCards(data);
        }).fail(function () {
            var list = document.getElementById('propagandaDesignsList');
            if (list) list.innerHTML = '<div class="col-12"><div class="alert alert-danger">Error al cargar disenos</div></div>';
        });
    }

    function renderDesignCards(designs) {
        var list = document.getElementById('propagandaDesignsList');
        if (!list) return;

        var html = '';

        // Boton crear nuevo
        html += '<div class="col-xl-3 col-lg-4 col-md-6 col-12">' +
            '<div class="mg-card h-100 border-2 border-dashed d-flex align-items-center justify-content-center" ' +
            'style="min-height:220px;cursor:pointer;border-color:var(--mg-primary) !important;" onclick="PropagandaEditor.showNewDesignModal()">' +
            '<div class="text-center p-4">' +
            '<i class="bi bi-plus-circle fs-1 text-primary d-block mb-2"></i>' +
            '<span class="fw-semibold text-primary">Crear Nuevo Diseno</span>' +
            '</div></div></div>';

        if (designs && designs.length > 0) {
            for (var i = 0; i < designs.length; i++) {
                var d = designs[i];
                var thumb = d.thumbnailDataUri
                    ? '<img src="' + escAttr(d.thumbnailDataUri) + '" class="w-100" style="height:160px;object-fit:contain;background:#f5f5f5;" />'
                    : '<div class="d-flex align-items-center justify-content-center" style="height:160px;background:#f5f5f5;"><i class="bi bi-image fs-1 text-muted"></i></div>';

                html += '<div class="col-xl-3 col-lg-4 col-md-6 col-12">' +
                    '<div class="mg-card h-100">' +
                    '<div class="position-relative">' + thumb +
                    '<div class="position-absolute top-0 end-0 p-2">' +
                    '<span class="badge bg-dark bg-opacity-75">' + d.ancho + 'x' + d.alto + '</span>' +
                    '</div></div>' +
                    '<div class="card-body p-3">' +
                    '<h6 class="fw-bold mb-1 text-truncate">' + escHtml(d.nombre) + '</h6>' +
                    '<small class="text-muted">' + formatDate(d.fechaModificacion) + '</small>' +
                    '<div class="d-flex gap-1 mt-2">' +
                    '<button class="btn btn-sm btn-primary flex-grow-1" onclick="PropagandaEditor.openEditor(\'' + d.disenoId + '\')"><i class="bi bi-pencil me-1"></i>Editar</button>' +
                    '<button class="btn btn-sm btn-light" onclick="PropagandaEditor.duplicateDesign(\'' + d.disenoId + '\')" title="Duplicar"><i class="bi bi-copy"></i></button>' +
                    '<button class="btn btn-sm btn-light-danger" onclick="PropagandaEditor.deleteDesign(\'' + d.disenoId + '\')" title="Eliminar"><i class="bi bi-trash"></i></button>' +
                    '</div></div></div></div>';
            }
        }

        list.innerHTML = html;
    }

    function formatDate(dateStr) {
        if (!dateStr) return '';
        var d = new Date(dateStr);
        return d.toLocaleDateString('es-EC', { day: '2-digit', month: 'short', year: 'numeric' });
    }

    // ══════════════════════════════════════════════════════
    // Modal nuevo diseno
    // ══════════════════════════════════════════════════════

    function showNewDesignModal() {
        var modal = document.getElementById('newDesignModal');
        if (!modal) createNewDesignModal();
        var bsModal = new bootstrap.Modal(document.getElementById('newDesignModal'));

        // Render tamanos
        var sizesHtml = '';
        for (var key in SIZES) {
            var s = SIZES[key];
            var checked = key === 'ig-post' ? 'checked' : '';
            sizesHtml += '<div class="col-6 col-md-4"><label class="mg-card p-3 d-block text-center" style="cursor:pointer;">' +
                '<input type="radio" name="newDesignSize" value="' + key + '" ' + checked + ' class="d-none" />' +
                '<i class="bi bi-aspect-ratio d-block fs-4 mb-1 text-primary"></i>' +
                '<span class="fw-semibold small d-block">' + s.label + '</span>' +
                '<span class="text-muted" style="font-size:0.75rem;">' + s.w + 'x' + s.h + '</span>' +
                '</label></div>';
        }
        document.getElementById('newDesignSizes').innerHTML = sizesHtml;

        // Render plantillas
        var tplHtml = '<div class="col-6 col-md-4"><label class="mg-card p-3 d-block text-center" style="cursor:pointer;">' +
            '<input type="radio" name="newDesignTemplate" value="" checked class="d-none" />' +
            '<i class="bi bi-file-earmark d-block fs-4 mb-1 text-muted"></i>' +
            '<span class="fw-semibold small">En Blanco</span></label></div>';
        for (var t = 0; t < TEMPLATES.length; t++) {
            tplHtml += '<div class="col-6 col-md-4"><label class="mg-card p-3 d-block text-center" style="cursor:pointer;">' +
                '<input type="radio" name="newDesignTemplate" value="' + TEMPLATES[t].id + '" class="d-none" />' +
                '<i class="bi bi-palette d-block fs-4 mb-1 text-primary"></i>' +
                '<span class="fw-semibold small">' + escHtml(TEMPLATES[t].nombre) + '</span></label></div>';
        }
        document.getElementById('newDesignTemplates').innerHTML = tplHtml;

        // Selection highlight
        document.querySelectorAll('#newDesignModal label').forEach(function (lbl) {
            lbl.addEventListener('click', function () {
                var group = this.querySelector('input').name;
                document.querySelectorAll('input[name="' + group + '"]').forEach(function (inp) {
                    inp.closest('label').style.borderColor = '';
                });
                this.style.borderColor = 'var(--mg-primary)';
            });
        });

        bsModal.show();
    }

    function createNewDesignModal() {
        var div = document.createElement('div');
        div.innerHTML =
            '<div class="modal fade" id="newDesignModal" tabindex="-1">' +
            '<div class="modal-dialog modal-lg modal-dialog-centered modal-fullscreen-sm-down">' +
            '<div class="modal-content">' +
            '<div class="modal-header bg-light">' +
            '<h3 class="modal-title fw-bolder"><i class="bi bi-plus-circle text-primary me-2"></i>Nuevo Diseno</h3>' +
            '<button type="button" class="btn-close" data-bs-dismiss="modal"></button>' +
            '</div>' +
            '<div class="modal-body">' +
            '<div class="mb-3"><label class="form-label fw-semibold">Nombre</label>' +
            '<input type="text" class="form-control form-control-lg" id="newDesignName" placeholder="Ej: Promo Febrero" maxlength="200" /></div>' +
            '<div class="mb-4"><label class="form-label fw-semibold">Tamano</label>' +
            '<div class="row g-2" id="newDesignSizes"></div>' +
            '<div id="customSizeFields" class="row g-2 mt-2 d-none">' +
            '<div class="col-6"><input type="number" class="form-control" id="customWidth" placeholder="Ancho (px)" min="100" max="5000" value="1080" /></div>' +
            '<div class="col-6"><input type="number" class="form-control" id="customHeight" placeholder="Alto (px)" min="100" max="5000" value="1080" /></div>' +
            '</div></div>' +
            '<div class="mb-3"><label class="form-label fw-semibold">Plantilla</label>' +
            '<div class="row g-2" id="newDesignTemplates"></div></div>' +
            '</div>' +
            '<div class="modal-footer bg-light">' +
            '<button type="button" class="btn btn-light" data-bs-dismiss="modal">Cancelar</button>' +
            '<button type="button" class="btn btn-primary" onclick="PropagandaEditor.createNewDesign()"><i class="bi bi-check-circle me-2"></i>Crear</button>' +
            '</div></div></div></div>';
        document.body.appendChild(div.firstChild);

        // Show/hide custom size fields
        document.getElementById('newDesignSizes').addEventListener('change', function (e) {
            var val = e.target.value;
            document.getElementById('customSizeFields').classList.toggle('d-none', val !== 'custom');
        });
    }

    function createNewDesign() {
        var nombre = document.getElementById('newDesignName').value.trim() || 'Sin nombre';
        var sizeKey = document.querySelector('input[name="newDesignSize"]:checked').value;
        var tplId = document.querySelector('input[name="newDesignTemplate"]:checked').value;

        var w, h;
        if (sizeKey === 'custom') {
            w = parseInt(document.getElementById('customWidth').value) || 1080;
            h = parseInt(document.getElementById('customHeight').value) || 1080;
        } else {
            w = SIZES[sizeKey].w;
            h = SIZES[sizeKey].h;
        }

        bootstrap.Modal.getInstance(document.getElementById('newDesignModal')).hide();

        // Create on server, then open editor
        postJson('/Negocios/CrearDiseno', { Nombre: nombre, CanvasJson: '{}', ThumbnailDataUri: '', Ancho: w, Alto: h }, function (resp) {
            if (resp.success) {
                currentDisenoId = resp.dataId;
                initEditor(w, h, nombre, tplId);
            }
        });
    }

    // ══════════════════════════════════════════════════════
    // Editor Canvas (Fabric.js)
    // ══════════════════════════════════════════════════════

    function openEditor(disenoId) {
        $.get('/Negocios/GetDiseno?disenoId=' + disenoId, function (data) {
            currentDisenoId = data.disenoId;
            initEditor(data.ancho, data.alto, data.nombre, null, data.canvasJson);
        }).fail(function () {
            toastr.error('Error al cargar el diseno');
        });
    }

    function initEditor(w, h, nombre, templateId, existingJson) {
        isEditorMode = true;
        isDirty = false;

        var container = document.getElementById('propagandaTabContent');
        if (!container) return;

        // Scale canvas to fit viewport
        var maxW = Math.min(window.innerWidth - 380, 900);
        var maxH = window.innerHeight - 200;
        var scale = Math.min(maxW / w, maxH / h, 1);
        var displayW = Math.round(w * scale);
        var displayH = Math.round(h * scale);

        container.innerHTML = buildEditorHTML(nombre, w, h, displayW, displayH);

        canvas = new fabric.Canvas('propagandaCanvas', {
            width: displayW,
            height: displayH,
            backgroundColor: '#FFFFFF',
            preserveObjectStacking: true
        });

        // Set zoom to match scale
        canvas.setZoom(scale);

        // Load existing or template
        if (existingJson && existingJson !== '{}') {
            canvas.loadFromJSON(existingJson, function () {
                canvas.setZoom(scale);
                canvas.renderAll();
            });
        } else if (templateId) {
            applyTemplate(templateId, w, h);
        }

        // Events
        canvas.on('object:modified', function () { isDirty = true; });
        canvas.on('object:added', function () { isDirty = true; });
        canvas.on('object:removed', function () { isDirty = true; });
        canvas.on('selection:created', updatePropertiesPanel);
        canvas.on('selection:updated', updatePropertiesPanel);
        canvas.on('selection:cleared', clearPropertiesPanel);

        bindEditorEvents();
        updateLayersPanel();
    }

    function buildEditorHTML(nombre, w, h, displayW, displayH) {
        return '' +
            '<div class="d-flex flex-wrap justify-content-between align-items-center mb-3">' +
            '   <div class="d-flex align-items-center gap-2">' +
            '       <button class="btn btn-light btn-sm" onclick="PropagandaEditor.closeEditor()"><i class="bi bi-arrow-left me-1"></i>Volver</button>' +
            '       <input type="text" class="form-control form-control-sm fw-bold" id="editorDesignName" value="' + escAttr(nombre) + '" style="max-width:250px;" />' +
            '       <span class="badge bg-light text-muted">' + w + 'x' + h + '</span>' +
            '   </div>' +
            '   <div class="d-flex gap-2">' +
            '       <button class="btn btn-light btn-sm" onclick="PropagandaEditor.undo()" title="Deshacer"><i class="bi bi-arrow-counterclockwise"></i></button>' +
            '       <button class="btn btn-light btn-sm" onclick="PropagandaEditor.redo()" title="Rehacer"><i class="bi bi-arrow-clockwise"></i></button>' +
            '       <button class="btn btn-success btn-sm" onclick="PropagandaEditor.saveDesign()"><i class="bi bi-save me-1"></i>Guardar</button>' +
            '       <div class="dropdown">' +
            '           <button class="btn btn-primary btn-sm dropdown-toggle" data-bs-toggle="dropdown"><i class="bi bi-download me-1"></i>Exportar</button>' +
            '           <ul class="dropdown-menu dropdown-menu-end">' +
            '               <li><a class="dropdown-item" href="#" onclick="PropagandaEditor.exportImage(\'png\');return false;"><i class="bi bi-file-image me-2"></i>PNG</a></li>' +
            '               <li><a class="dropdown-item" href="#" onclick="PropagandaEditor.exportImage(\'jpeg\');return false;"><i class="bi bi-file-image me-2"></i>JPG</a></li>' +
            '           </ul>' +
            '       </div>' +
            '   </div>' +
            '</div>' +
            '<div class="row g-3">' +
            '   <div class="col-lg-2 col-12 order-2 order-lg-1">' +
            '       <div class="mg-card p-2">' +
            '           <h6 class="fw-bold px-2 pt-2 mb-2"><i class="bi bi-tools me-1"></i>Herramientas</h6>' +
            '           <div class="d-grid gap-1">' +
            '               <button class="btn btn-sm btn-light text-start" onclick="PropagandaEditor.addText()"><i class="bi bi-fonts me-2"></i>Texto</button>' +
            '               <button class="btn btn-sm btn-light text-start" onclick="PropagandaEditor.addHeading()"><i class="bi bi-type-h1 me-2"></i>Titulo</button>' +
            '               <button class="btn btn-sm btn-light text-start" onclick="PropagandaEditor.addRect()"><i class="bi bi-square me-2"></i>Rectangulo</button>' +
            '               <button class="btn btn-sm btn-light text-start" onclick="PropagandaEditor.addCircle()"><i class="bi bi-circle me-2"></i>Circulo</button>' +
            '               <button class="btn btn-sm btn-light text-start" onclick="PropagandaEditor.addTriangle()"><i class="bi bi-triangle me-2"></i>Triangulo</button>' +
            '               <button class="btn btn-sm btn-light text-start" onclick="PropagandaEditor.addLine()"><i class="bi bi-dash-lg me-2"></i>Linea</button>' +
            '               <button class="btn btn-sm btn-light text-start" onclick="PropagandaEditor.addImage()"><i class="bi bi-image me-2"></i>Imagen</button>' +
            '           </div>' +
            '           <h6 class="fw-bold px-2 pt-3 mb-2"><i class="bi bi-stack me-1"></i>Capas</h6>' +
            '           <div id="editorLayers" class="small" style="max-height:200px;overflow-y:auto;"></div>' +
            '       </div>' +
            '   </div>' +
            '   <div class="col-lg-7 col-12 order-1 order-lg-2">' +
            '       <div class="text-center" style="overflow:auto;background:#e9ecef;border-radius:8px;padding:16px;">' +
            '           <canvas id="propagandaCanvas" width="' + displayW + '" height="' + displayH + '"></canvas>' +
            '       </div>' +
            '   </div>' +
            '   <div class="col-lg-3 col-12 order-3">' +
            '       <div class="mg-card p-3" id="editorProperties">' +
            '           <h6 class="fw-bold mb-3"><i class="bi bi-sliders me-1"></i>Propiedades</h6>' +
            '           <p class="text-muted small">Selecciona un elemento para editar sus propiedades</p>' +
            '       </div>' +
            '   </div>' +
            '</div>';
    }

    function bindEditorEvents() {
        // Keyboard shortcuts
        document.addEventListener('keydown', editorKeyHandler);
    }

    function editorKeyHandler(e) {
        if (!isEditorMode || !canvas) return;
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;

        if (e.key === 'Delete' || e.key === 'Backspace') {
            deleteSelected();
            e.preventDefault();
        } else if (e.ctrlKey || e.metaKey) {
            if (e.key === 'c') { copySelected(); e.preventDefault(); }
            else if (e.key === 'v') { pasteClipboard(); e.preventDefault(); }
            else if (e.key === 's') { saveDesign(); e.preventDefault(); }
            else if (e.key === 'a') { canvas.discardActiveObject(); var sel = new fabric.ActiveSelection(canvas.getObjects(), { canvas: canvas }); canvas.setActiveObject(sel); canvas.requestRenderAll(); e.preventDefault(); }
        }
    }

    // ══════════════════════════════════════════════════════
    // Herramientas de dibujo
    // ══════════════════════════════════════════════════════

    function addText() {
        var text = new fabric.IText('Escribe aqui', {
            left: 100, top: 100,
            fontSize: 32, fontFamily: 'Inter',
            fill: '#000000'
        });
        canvas.add(text);
        canvas.setActiveObject(text);
        canvas.renderAll();
        updateLayersPanel();
    }

    function addHeading() {
        var text = new fabric.IText('TITULO', {
            left: 100, top: 100,
            fontSize: 64, fontFamily: 'Inter',
            fontWeight: 'bold', fill: '#0047AB'
        });
        canvas.add(text);
        canvas.setActiveObject(text);
        canvas.renderAll();
        updateLayersPanel();
    }

    function addRect() {
        var rect = new fabric.Rect({
            left: 100, top: 100, width: 200, height: 150,
            fill: '#0047AB', rx: 8, ry: 8
        });
        canvas.add(rect);
        canvas.setActiveObject(rect);
        canvas.renderAll();
        updateLayersPanel();
    }

    function addCircle() {
        var circle = new fabric.Circle({
            left: 100, top: 100, radius: 80,
            fill: '#00D4FF'
        });
        canvas.add(circle);
        canvas.setActiveObject(circle);
        canvas.renderAll();
        updateLayersPanel();
    }

    function addTriangle() {
        var tri = new fabric.Triangle({
            left: 100, top: 100, width: 150, height: 130,
            fill: '#2C3E50'
        });
        canvas.add(tri);
        canvas.setActiveObject(tri);
        canvas.renderAll();
        updateLayersPanel();
    }

    function addLine() {
        var line = new fabric.Line([50, 100, 350, 100], {
            stroke: '#000000', strokeWidth: 3
        });
        canvas.add(line);
        canvas.setActiveObject(line);
        canvas.renderAll();
        updateLayersPanel();
    }

    function addImage() {
        var input = document.createElement('input');
        input.type = 'file';
        input.accept = 'image/*';
        input.onchange = function (e) {
            var file = e.target.files[0];
            if (!file) return;
            var reader = new FileReader();
            reader.onload = function (ev) {
                fabric.Image.fromURL(ev.target.result, function (img) {
                    var maxDim = 400;
                    if (img.width > maxDim || img.height > maxDim) {
                        var scale = maxDim / Math.max(img.width, img.height);
                        img.scale(scale);
                    }
                    img.set({ left: 50, top: 50 });
                    canvas.add(img);
                    canvas.setActiveObject(img);
                    canvas.renderAll();
                    updateLayersPanel();
                });
            };
            reader.readAsDataURL(file);
        };
        input.click();
    }

    // ══════════════════════════════════════════════════════
    // Panel de propiedades
    // ══════════════════════════════════════════════════════

    function updatePropertiesPanel() {
        var obj = canvas.getActiveObject();
        if (!obj) { clearPropertiesPanel(); return; }

        var panel = document.getElementById('editorProperties');
        if (!panel) return;

        var isText = obj.type === 'i-text' || obj.type === 'text' || obj.type === 'textbox';
        var isShape = obj.type === 'rect' || obj.type === 'circle' || obj.type === 'triangle';

        var html = '<h6 class="fw-bold mb-3"><i class="bi bi-sliders me-1"></i>Propiedades</h6>';

        // Fill color
        if (!obj.isType || obj.type !== 'image') {
            html += '<div class="mb-3"><label class="form-label small fw-semibold">Color de relleno</label>' +
                '<div class="d-flex gap-1 flex-wrap">';
            for (var c = 0; c < COLORS.length; c++) {
                var active = (obj.fill === COLORS[c]) ? 'border:2px solid var(--mg-primary);' : 'border:2px solid transparent;';
                html += '<div style="width:28px;height:28px;border-radius:4px;cursor:pointer;background:' + COLORS[c] + ';' + active + '" ' +
                    'onclick="PropagandaEditor.setFill(\'' + COLORS[c] + '\')"></div>';
            }
            html += '<input type="color" class="form-control form-control-color p-0" style="width:28px;height:28px;" value="' + (obj.fill || '#000000') + '" onchange="PropagandaEditor.setFill(this.value)" />';
            html += '</div></div>';
        }

        // Stroke
        html += '<div class="mb-3"><label class="form-label small fw-semibold">Borde</label>' +
            '<div class="d-flex gap-2 align-items-center">' +
            '<input type="color" class="form-control form-control-color p-0" style="width:28px;height:28px;" value="' + (obj.stroke || '#000000') + '" onchange="PropagandaEditor.setStroke(this.value)" />' +
            '<input type="number" class="form-control form-control-sm" style="width:60px;" value="' + (obj.strokeWidth || 0) + '" min="0" max="20" onchange="PropagandaEditor.setStrokeWidth(parseInt(this.value))" />' +
            '</div></div>';

        // Text properties
        if (isText) {
            html += '<div class="mb-3"><label class="form-label small fw-semibold">Fuente</label>' +
                '<select class="form-select form-select-sm" onchange="PropagandaEditor.setFontFamily(this.value)">' +
                '<option value="Inter"' + (obj.fontFamily === 'Inter' ? ' selected' : '') + '>Inter</option>' +
                '<option value="Arial"' + (obj.fontFamily === 'Arial' ? ' selected' : '') + '>Arial</option>' +
                '<option value="Georgia"' + (obj.fontFamily === 'Georgia' ? ' selected' : '') + '>Georgia</option>' +
                '<option value="Courier New"' + (obj.fontFamily === 'Courier New' ? ' selected' : '') + '>Courier New</option>' +
                '<option value="Impact"' + (obj.fontFamily === 'Impact' ? ' selected' : '') + '>Impact</option>' +
                '</select></div>';

            html += '<div class="mb-3"><label class="form-label small fw-semibold">Tamano</label>' +
                '<input type="number" class="form-control form-control-sm" value="' + Math.round(obj.fontSize || 32) + '" min="8" max="300" onchange="PropagandaEditor.setFontSize(parseInt(this.value))" /></div>';

            html += '<div class="mb-3 d-flex gap-1">' +
                '<button class="btn btn-sm ' + (obj.fontWeight === 'bold' ? 'btn-primary' : 'btn-light') + '" onclick="PropagandaEditor.toggleBold()"><i class="bi bi-type-bold"></i></button>' +
                '<button class="btn btn-sm ' + (obj.fontStyle === 'italic' ? 'btn-primary' : 'btn-light') + '" onclick="PropagandaEditor.toggleItalic()"><i class="bi bi-type-italic"></i></button>' +
                '<button class="btn btn-sm ' + (obj.underline ? 'btn-primary' : 'btn-light') + '" onclick="PropagandaEditor.toggleUnderline()"><i class="bi bi-type-underline"></i></button>' +
                '<button class="btn btn-sm btn-light" onclick="PropagandaEditor.setTextAlign(\'left\')" title="Izquierda"><i class="bi bi-text-left"></i></button>' +
                '<button class="btn btn-sm btn-light" onclick="PropagandaEditor.setTextAlign(\'center\')" title="Centro"><i class="bi bi-text-center"></i></button>' +
                '<button class="btn btn-sm btn-light" onclick="PropagandaEditor.setTextAlign(\'right\')" title="Derecha"><i class="bi bi-text-right"></i></button>' +
                '</div>';
        }

        // Opacity
        html += '<div class="mb-3"><label class="form-label small fw-semibold">Opacidad</label>' +
            '<input type="range" class="form-range" min="0" max="1" step="0.05" value="' + (obj.opacity || 1) + '" onchange="PropagandaEditor.setOpacity(parseFloat(this.value))" /></div>';

        // Z-order
        html += '<div class="mb-3"><label class="form-label small fw-semibold">Orden</label>' +
            '<div class="d-flex gap-1">' +
            '<button class="btn btn-sm btn-light" onclick="PropagandaEditor.bringForward()" title="Subir"><i class="bi bi-chevron-up"></i></button>' +
            '<button class="btn btn-sm btn-light" onclick="PropagandaEditor.sendBackward()" title="Bajar"><i class="bi bi-chevron-down"></i></button>' +
            '<button class="btn btn-sm btn-light" onclick="PropagandaEditor.bringToFront()" title="Al frente"><i class="bi bi-chevron-double-up"></i></button>' +
            '<button class="btn btn-sm btn-light" onclick="PropagandaEditor.sendToBack()" title="Al fondo"><i class="bi bi-chevron-double-down"></i></button>' +
            '</div></div>';

        // Delete
        html += '<div class="mt-3"><button class="btn btn-sm btn-danger w-100" onclick="PropagandaEditor.deleteSelected()"><i class="bi bi-trash me-1"></i>Eliminar</button></div>';

        panel.innerHTML = html;
    }

    function clearPropertiesPanel() {
        var panel = document.getElementById('editorProperties');
        if (panel) {
            panel.innerHTML = '<h6 class="fw-bold mb-3"><i class="bi bi-sliders me-1"></i>Propiedades</h6>' +
                '<p class="text-muted small">Selecciona un elemento para editar sus propiedades</p>';
        }
    }

    // ══════════════════════════════════════════════════════
    // Property setters
    // ══════════════════════════════════════════════════════

    function setFill(color) { var o = canvas.getActiveObject(); if (o) { o.set('fill', color); canvas.renderAll(); isDirty = true; updatePropertiesPanel(); } }
    function setStroke(color) { var o = canvas.getActiveObject(); if (o) { o.set('stroke', color); canvas.renderAll(); isDirty = true; } }
    function setStrokeWidth(w) { var o = canvas.getActiveObject(); if (o) { o.set('strokeWidth', w); canvas.renderAll(); isDirty = true; } }
    function setFontFamily(f) { var o = canvas.getActiveObject(); if (o) { o.set('fontFamily', f); canvas.renderAll(); isDirty = true; } }
    function setFontSize(s) { var o = canvas.getActiveObject(); if (o) { o.set('fontSize', s); canvas.renderAll(); isDirty = true; } }
    function setOpacity(v) { var o = canvas.getActiveObject(); if (o) { o.set('opacity', v); canvas.renderAll(); isDirty = true; } }
    function setTextAlign(a) { var o = canvas.getActiveObject(); if (o) { o.set('textAlign', a); canvas.renderAll(); isDirty = true; } }

    function toggleBold() {
        var o = canvas.getActiveObject();
        if (o) { o.set('fontWeight', o.fontWeight === 'bold' ? 'normal' : 'bold'); canvas.renderAll(); isDirty = true; updatePropertiesPanel(); }
    }
    function toggleItalic() {
        var o = canvas.getActiveObject();
        if (o) { o.set('fontStyle', o.fontStyle === 'italic' ? 'normal' : 'italic'); canvas.renderAll(); isDirty = true; updatePropertiesPanel(); }
    }
    function toggleUnderline() {
        var o = canvas.getActiveObject();
        if (o) { o.set('underline', !o.underline); canvas.renderAll(); isDirty = true; updatePropertiesPanel(); }
    }

    // Z-order
    function bringForward() { var o = canvas.getActiveObject(); if (o) { canvas.bringForward(o); canvas.renderAll(); updateLayersPanel(); } }
    function sendBackward() { var o = canvas.getActiveObject(); if (o) { canvas.sendBackwards(o); canvas.renderAll(); updateLayersPanel(); } }
    function bringToFront() { var o = canvas.getActiveObject(); if (o) { canvas.bringToFront(o); canvas.renderAll(); updateLayersPanel(); } }
    function sendToBack() { var o = canvas.getActiveObject(); if (o) { canvas.sendToBack(o); canvas.renderAll(); updateLayersPanel(); } }

    // ══════════════════════════════════════════════════════
    // Layers panel
    // ══════════════════════════════════════════════════════

    function updateLayersPanel() {
        if (!canvas) return;
        var panel = document.getElementById('editorLayers');
        if (!panel) return;

        var objects = canvas.getObjects();
        if (objects.length === 0) {
            panel.innerHTML = '<p class="text-muted small px-1">Sin elementos</p>';
            return;
        }

        var html = '';
        for (var i = objects.length - 1; i >= 0; i--) {
            var obj = objects[i];
            var icon = 'bi-square';
            var name = 'Forma';
            if (obj.type === 'i-text' || obj.type === 'text' || obj.type === 'textbox') { icon = 'bi-fonts'; name = (obj.text || '').substring(0, 15); }
            else if (obj.type === 'circle') { icon = 'bi-circle'; name = 'Circulo'; }
            else if (obj.type === 'triangle') { icon = 'bi-triangle'; name = 'Triangulo'; }
            else if (obj.type === 'rect') { icon = 'bi-square'; name = 'Rectangulo'; }
            else if (obj.type === 'line') { icon = 'bi-dash-lg'; name = 'Linea'; }
            else if (obj.type === 'image') { icon = 'bi-image'; name = 'Imagen'; }

            var active = canvas.getActiveObject() === obj ? 'bg-primary bg-opacity-10' : '';
            html += '<div class="d-flex align-items-center gap-1 px-1 py-1 rounded ' + active + '" style="cursor:pointer;" onclick="PropagandaEditor.selectLayer(' + i + ')">' +
                '<i class="bi ' + icon + ' text-muted"></i>' +
                '<span class="text-truncate flex-grow-1" style="font-size:0.8rem;">' + escHtml(name) + '</span>' +
                '<button class="btn btn-sm p-0 text-muted" onclick="event.stopPropagation();PropagandaEditor.toggleLayerVisibility(' + i + ')" title="Visibilidad">' +
                '<i class="bi ' + (obj.visible !== false ? 'bi-eye' : 'bi-eye-slash') + '"></i></button>' +
                '</div>';
        }
        panel.innerHTML = html;
    }

    function selectLayer(index) {
        var obj = canvas.getObjects()[index];
        if (obj) {
            canvas.setActiveObject(obj);
            canvas.renderAll();
            updatePropertiesPanel();
            updateLayersPanel();
        }
    }

    function toggleLayerVisibility(index) {
        var obj = canvas.getObjects()[index];
        if (obj) {
            obj.visible = !obj.visible;
            canvas.renderAll();
            updateLayersPanel();
            isDirty = true;
        }
    }

    // ══════════════════════════════════════════════════════
    // Clipboard
    // ══════════════════════════════════════════════════════

    function copySelected() {
        var obj = canvas.getActiveObject();
        if (obj) obj.clone(function (cloned) { clipboard = cloned; });
    }

    function pasteClipboard() {
        if (!clipboard) return;
        clipboard.clone(function (cloned) {
            canvas.discardActiveObject();
            cloned.set({ left: cloned.left + 20, top: cloned.top + 20, evented: true });
            if (cloned.type === 'activeSelection') {
                cloned.canvas = canvas;
                cloned.forEachObject(function (obj) { canvas.add(obj); });
                cloned.setCoords();
            } else {
                canvas.add(cloned);
            }
            clipboard.top += 20;
            clipboard.left += 20;
            canvas.setActiveObject(cloned);
            canvas.requestRenderAll();
            updateLayersPanel();
        });
    }

    function deleteSelected() {
        var active = canvas.getActiveObject();
        if (!active) return;
        if (active.type === 'activeSelection') {
            active.forEachObject(function (obj) { canvas.remove(obj); });
            canvas.discardActiveObject();
        } else {
            canvas.remove(active);
        }
        canvas.renderAll();
        updateLayersPanel();
        clearPropertiesPanel();
        isDirty = true;
    }

    // ══════════════════════════════════════════════════════
    // Templates
    // ══════════════════════════════════════════════════════

    function applyTemplate(templateId, w, h) {
        if (!canvas || !templateId) return;

        var tpl = null;
        for (var i = 0; i < TEMPLATES.length; i++) {
            if (TEMPLATES[i].id === templateId) { tpl = TEMPLATES[i]; break; }
        }
        if (!tpl) return;

        var items = tpl.build(w, h);
        for (var j = 0; j < items.length; j++) {
            var item = items[j];
            var obj = null;

            if (item.type === 'rect') {
                obj = new fabric.Rect(item);
            } else if (item.type === 'circle') {
                obj = new fabric.Circle(item);
            } else if (item.type === 'text') {
                obj = new fabric.IText(item.text, item);
            }

            if (obj) {
                canvas.add(obj);
            }
        }
        canvas.renderAll();
        updateLayersPanel();
    }

    // ══════════════════════════════════════════════════════
    // Save / Export
    // ══════════════════════════════════════════════════════

    function saveDesign() {
        if (!canvas || !currentDisenoId) return;

        var nombre = document.getElementById('editorDesignName') ? document.getElementById('editorDesignName').value.trim() : '';

        // Get JSON at zoom=1
        var currentZoom = canvas.getZoom();
        canvas.setZoom(1);
        var json = JSON.stringify(canvas.toJSON());
        var thumbnail = canvas.toDataURL({ format: 'png', quality: 0.5, multiplier: 0.2 });
        canvas.setZoom(currentZoom);

        postJson('/Negocios/GuardarDiseno', {
            DisenoId: currentDisenoId,
            Nombre: nombre,
            CanvasJson: json,
            ThumbnailDataUri: thumbnail
        }, function (resp) {
            if (resp.success) {
                toastr.success('Diseno guardado');
                isDirty = false;
            }
        });
    }

    function exportImage(format) {
        if (!canvas) return;

        var currentZoom = canvas.getZoom();
        canvas.setZoom(1);
        canvas.discardActiveObject();
        canvas.renderAll();

        var dataUrl = canvas.toDataURL({
            format: format || 'png',
            quality: format === 'jpeg' ? 0.92 : 1,
            multiplier: 1
        });

        canvas.setZoom(currentZoom);
        canvas.renderAll();

        var link = document.createElement('a');
        var nombre = document.getElementById('editorDesignName') ? document.getElementById('editorDesignName').value.trim() : 'diseno';
        link.download = nombre + '.' + (format === 'jpeg' ? 'jpg' : 'png');
        link.href = dataUrl;
        link.click();
    }

    // ══════════════════════════════════════════════════════
    // Close editor
    // ══════════════════════════════════════════════════════

    function closeEditor() {
        if (isDirty) {
            swalMG.fire({
                title: 'Cambios sin guardar',
                text: 'Tienes cambios sin guardar. Que deseas hacer?',
                icon: 'warning',
                showCancelButton: true,
                showDenyButton: true,
                confirmButtonText: '<i class="bi bi-save me-1"></i>Guardar y salir',
                denyButtonText: 'Salir sin guardar',
                cancelButtonText: 'Cancelar'
            }).then(function (result) {
                if (result.isConfirmed) {
                    saveDesign();
                    setTimeout(function () { exitEditor(); }, 500);
                } else if (result.isDenied) {
                    exitEditor();
                }
            });
        } else {
            exitEditor();
        }
    }

    function exitEditor() {
        isEditorMode = false;
        if (canvas) {
            canvas.dispose();
            canvas = null;
        }
        currentDisenoId = null;
        isDirty = false;
        document.removeEventListener('keydown', editorKeyHandler);

        var container = document.getElementById('propagandaTabContent');
        if (container) {
            container.innerHTML =
                '<div class="d-flex flex-wrap justify-content-between align-items-center mb-4">' +
                '   <div><h3 class="fw-bold mb-0"><i class="bi bi-palette text-primary me-2"></i>Propaganda</h3>' +
                '   <p class="text-muted mb-0">Crea flyers, posts y material de marketing para tu negocio</p></div>' +
                '</div>' +
                '<div id="propagandaGalleryContent"></div>';
            renderGallery();
            loadDesigns();
        }
    }

    // ══════════════════════════════════════════════════════
    // Delete / Duplicate from gallery
    // ══════════════════════════════════════════════════════

    function deleteDesign(id) {
        mgConfirmDelete(function () {
            postJson('/Negocios/EliminarDiseno', { DisenoId: id }, function (resp) {
                if (resp.success) {
                    toastr.success('Diseno eliminado');
                    loadDesigns();
                }
            });
        });
    }

    function duplicateDesign(id) {
        postJson('/Negocios/DuplicarDiseno', { DisenoId: id }, function (resp) {
            if (resp.success) {
                toastr.success('Diseno duplicado');
                loadDesigns();
            }
        });
    }

    // Undo/Redo stubs (Fabric.js doesn't have built-in undo)
    function undo() { toastr.info('Deshacer no disponible aun'); }
    function redo() { toastr.info('Rehacer no disponible aun'); }

    // ══════════════════════════════════════════════════════
    // Public API
    // ══════════════════════════════════════════════════════

    return {
        init: init,
        showNewDesignModal: showNewDesignModal,
        createNewDesign: createNewDesign,
        openEditor: openEditor,
        closeEditor: closeEditor,
        saveDesign: saveDesign,
        exportImage: exportImage,
        deleteDesign: deleteDesign,
        duplicateDesign: duplicateDesign,
        addText: addText,
        addHeading: addHeading,
        addRect: addRect,
        addCircle: addCircle,
        addTriangle: addTriangle,
        addLine: addLine,
        addImage: addImage,
        setFill: setFill,
        setStroke: setStroke,
        setStrokeWidth: setStrokeWidth,
        setFontFamily: setFontFamily,
        setFontSize: setFontSize,
        setOpacity: setOpacity,
        setTextAlign: setTextAlign,
        toggleBold: toggleBold,
        toggleItalic: toggleItalic,
        toggleUnderline: toggleUnderline,
        bringForward: bringForward,
        sendBackward: sendBackward,
        bringToFront: bringToFront,
        sendToBack: sendToBack,
        deleteSelected: deleteSelected,
        selectLayer: selectLayer,
        toggleLayerVisibility: toggleLayerVisibility,
        undo: undo,
        redo: redo
    };
})();
