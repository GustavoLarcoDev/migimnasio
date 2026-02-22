// ============================================================
// My-Negocio — Sistema de Tutorial Interactivo
// Carga el tutorial correcto según el tipo de negocio
// ============================================================

(function () {
    'use strict';

    // Referencia global al driver activo
    var activeTutorial = null;

    // Función para navegar a un tab del sidebar
    function navigateToTab(tabId) {
        var link = document.querySelector('[data-mg-tab="' + tabId + '"]');
        if (link) {
            link.click();
            // Esperar a que el tab se muestre
            return new Promise(function (resolve) { setTimeout(resolve, 400); });
        }
        return Promise.resolve();
    }

    // Verificar si un elemento existe y es visible
    function elementExists(selector) {
        var el = document.querySelector(selector);
        return el && el.offsetParent !== null;
    }

    // Función para abrir sidebar en mobile antes de mostrar elementos del sidebar
    function ensureSidebarVisible() {
        var sidebar = document.getElementById('sidebar');
        if (window.innerWidth < 992 && sidebar && !sidebar.classList.contains('show')) {
            var toggle = document.getElementById('sidebarToggle');
            if (toggle) toggle.click();
            return new Promise(function (resolve) { setTimeout(resolve, 350); });
        }
        return Promise.resolve();
    }

    // Función para cerrar sidebar en mobile
    function closeSidebarMobile() {
        var sidebar = document.getElementById('sidebar');
        var overlay = document.getElementById('sidebarOverlay');
        if (window.innerWidth < 992 && sidebar && sidebar.classList.contains('show')) {
            sidebar.classList.remove('show');
            if (overlay) overlay.classList.remove('show');
            document.body.style.overflow = '';
        }
    }

    // Helper para crear título con icono
    function stepTitle(icon, text, color) {
        color = color || 'primary';
        return '<span class="mg-tutorial-icon mg-tutorial-icon--' + color + '"><i class="bi bi-' + icon + '"></i></span> ' + text;
    }

    // Configuración base del driver
    function createDriver(steps) {
        if (typeof window.driver === 'undefined' || !window.driver.js) {
            console.warn('Driver.js no está cargado');
            return null;
        }

        var driverFn = window.driver.js.driver;

        // Filtrar pasos cuyo elemento no existe (para evitar que se trabe)
        var validSteps = [];
        for (var i = 0; i < steps.length; i++) {
            var step = steps[i];
            if (!step.element) {
                // Paso sin elemento (centrado) — siempre válido
                validSteps.push(step);
            } else if (document.querySelector(step.element)) {
                validSteps.push(step);
            }
            // Si el elemento no existe, omitir silenciosamente
        }

        if (validSteps.length === 0) {
            console.warn('No hay pasos válidos para el tutorial');
            return null;
        }

        var driverObj = driverFn({
            showProgress: true,
            animate: true,
            smoothScroll: true,
            allowClose: true,
            overlayClickNext: false,
            stagePadding: 8,
            stageRadius: 12,
            popoverOffset: 12,
            nextBtnText: 'Siguiente →',
            prevBtnText: '← Anterior',
            doneBtnText: '¡Listo!',
            progressText: '{{current}} de {{total}}',
            steps: validSteps,
            onDestroyStarted: function () {
                activeTutorial = null;
                // Cerrar sidebar mobile si quedó abierto
                closeSidebarMobile();
                driverObj.destroy();
            }
        });

        return driverObj;
    }

    // Iniciar tutorial
    function startTutorial() {
        // Si ya hay un tutorial activo, destruirlo
        if (activeTutorial) {
            activeTutorial.destroy();
            activeTutorial = null;
        }

        var tipo = window._mgTipoNegocio || 'membresias';
        var steps = null;

        // Obtener pasos según tipo de negocio
        switch (tipo) {
            case 'artesanal':
                steps = window._mgTutorialArtesanal;
                break;
            case 'tienda':
                steps = window._mgTutorialTienda;
                break;
            case 'restaurante':
                steps = window._mgTutorialRestaurante;
                break;
            default:
                steps = window._mgTutorialMembresias;
                break;
        }

        if (!steps || steps.length === 0) {
            console.warn('Tutorial no disponible para tipo: ' + tipo);
            if (typeof toastr !== 'undefined') {
                toastr.info('El tutorial se está cargando, intenta de nuevo en un momento.');
            }
            return;
        }

        // Navegar al primer tab antes de iniciar
        var firstTab = steps[0] && steps[0]._tab;
        var startFn = function () {
            activeTutorial = createDriver(steps);
            if (activeTutorial) {
                activeTutorial.drive();
            }
        };

        if (firstTab) {
            navigateToTab(firstTab).then(startFn);
        } else {
            startFn();
        }
    }

    // Exponer funciones globales para uso en tutoriales específicos
    window._mgTutorial = {
        navigateToTab: navigateToTab,
        elementExists: elementExists,
        ensureSidebarVisible: ensureSidebarVisible,
        closeSidebarMobile: closeSidebarMobile,
        stepTitle: stepTitle,
        startTutorial: startTutorial
    };

    // Bind al botón de tutorial
    document.addEventListener('DOMContentLoaded', function () {
        var btn = document.getElementById('btnTutorial');
        if (btn) {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                startTutorial();
            });

            // Pulso animado si es primera visita
            var tutorialSeen = localStorage.getItem('mg-tutorial-seen-' + (window._mgTipoNegocio || 'membresias'));
            if (!tutorialSeen) {
                btn.classList.add('mg-tutorial-btn--pulse');
                btn.addEventListener('click', function () {
                    localStorage.setItem('mg-tutorial-seen-' + (window._mgTipoNegocio || 'membresias'), '1');
                    btn.classList.remove('mg-tutorial-btn--pulse');
                }, { once: true });
            }
        }
    });
})();
