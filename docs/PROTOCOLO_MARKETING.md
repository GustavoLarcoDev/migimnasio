# Protocolo Marketing — Generación Programática de Assets de Marketing

Pipeline automatizado que genera flyers (PNG 1080x1080) y videos (MP4 1080x1920 vertical) para My-Negocio usando HTML + Playwright + ffmpeg.

## Activación

El usuario dice **"run protocolo marketing"** o **"protocolo marketing"**, opcionalmente seguido de:
- Cantidad deseada (ej: "20 flyers y 20 videos")
- Categorías específicas (ej: "solo restaurantes y tiendas")
- Temas específicos (ej: "enfoque en POS y reservas")

Si no especifica cantidad, generar **4 flyers + 4 videos por categoría** (20+20 = 40 total).

---

## Requisitos Previos

Verificar que estén instalados antes de iniciar:
```bash
# Playwright con Chromium
npx playwright --version
npx playwright install chromium

# ffmpeg para conversión de video
ffmpeg -version
```

---

## Paleta de Colores (OBLIGATORIA — NUNCA cambiar)

| Rol | Color | Hex | Uso |
|-----|-------|-----|-----|
| **Principal** | Azul Control | `#0047AB` | Logo, botones, encabezados, bordes principales |
| **Acento** | Azul Crecimiento | `#00D4FF` | CTAs, flechas, alertas, detalles tech, highlights |
| **Fondo oscuro** | Gris Oxford | `#2C3E50` | Textos secundarios, fondos oscuros, footers |
| **Blanco** | Blanco | `#FFFFFF` | Textos sobre fondos oscuros/gradiente |
| **Gradiente** | — | `linear-gradient(135deg, #0047AB, #00D4FF)` | Fondos hero, barras, CTAs |

Colores auxiliares (solo para badges/estados):
- Verde: `#27AE60` (activo, libre, exitoso)
- Rojo: `#E74C3C` (error, agotado, ocupado, vencido)
- Amarillo: `#F39C12` (advertencia, stock bajo, por vencer)
- Naranja: `#E67E22` (acento alternativo para diferenciación)

---

## Logo Oficial (OBLIGATORIO en TODOS los assets)

**Archivo original:** `/Users/gustavolarco/Downloads/Gemini_Generated_Image_ylhzusylhzusylhz (1)-Picsart-BackgroundRemover.png` (2048x2048, RGBA, transparencia nativa perfecta)

**Archivos procesados** (recortados y pre-escalados con LANCZOS):
- **Full resolution:** `~/Desktop/marketing/logo.png` (1496x942, RGBA, cropped)
- **Pre-escalado esquina (flyers):** `~/Desktop/marketing/logo-corner.png` (500x314, LANCZOS)
- **Pre-escalado CTA (videos):** `~/Desktop/marketing/logo-cta.png` (800x503, LANCZOS)

**Reglas de uso en FLYERS:**
1. Logo en la **esquina superior izquierda**, directo sobre el fondo (SIN contenedor blanco, SIN background)
2. CSS: `width: 280px`, con `filter: drop-shadow(0 0 12px rgba(255,255,255,0.6)) drop-shadow(0 2px 6px rgba(0,0,0,0.2))` para que resalte sobre fondos oscuros/gradiente
3. Usar `logo-corner.png` (500px) como source — NUNCA escalar el full-res en HTML

```css
.logo-corner {
    position: absolute;
    top: 30px;
    left: 30px;
}
.logo-corner img {
    width: 280px;
    height: auto;
    filter: drop-shadow(0 0 12px rgba(255,255,255,0.6)) drop-shadow(0 2px 6px rgba(0,0,0,0.2));
}
```

**Reglas de uso en VIDEOS — Pop-Up al final (OBLIGATORIO):**
1. Última escena (CTA): logo aparece con **efecto pop-up** (scale 0→1.12→0.95→1 + rotación sutil)
2. Detrás del logo: **glow circular** (radial-gradient blanco semi-transparente que pulsa)
3. Después del pop-up: "$10/mes" → "7 DÍAS GRATIS" → "Tu Negocio, Tu Control." con slideUp
4. Usar `logo-cta.png` (800px) — display width: 550px
5. CSS completo para la escena CTA:

```css
/* Glow behind logo */
.cta-logo-glow {
    position: absolute;
    top: 50%; left: 50%;
    width: 500px; height: 500px;
    transform: translate(-50%, -50%) scale(0);
    border-radius: 50%;
    background: radial-gradient(circle, rgba(255,255,255,0.25) 0%, transparent 70%);
    animation: glowPulse 0.8s ease-out 7.2s both;
}
@keyframes glowPulse {
    0% { transform: translate(-50%, -50%) scale(0); opacity: 0; }
    50% { transform: translate(-50%, -50%) scale(1.2); opacity: 1; }
    100% { transform: translate(-50%, -50%) scale(1); opacity: 0.6; }
}

/* Logo pop-up */
.cta-logo {
    width: 550px;
    position: relative;
    z-index: 2;
    opacity: 0;
    animation: logoPopUp 0.5s cubic-bezier(0.175, 0.885, 0.32, 1.275) 7.3s both;
}
.cta-logo img {
    width: 100%;
    filter: drop-shadow(0 0 20px rgba(255,255,255,0.4)) drop-shadow(0 4px 15px rgba(0,0,0,0.3));
}
@keyframes logoPopUp {
    0% { opacity: 0; transform: scale(0) rotate(-10deg); }
    70% { opacity: 1; transform: scale(1.12) rotate(2deg); }
    85% { transform: scale(0.95) rotate(-1deg); }
    100% { opacity: 1; transform: scale(1) rotate(0); }
}
```

```html
<!-- HTML structure for video CTA scene -->
<div class="scene scene-4">
    <div class="cta-logo-wrap">
        <div class="cta-logo-glow"></div>
        <div class="cta-logo"><img src="/Users/gustavolarco/Desktop/marketing/logo-cta.png" alt="My-Negocio"></div>
    </div>
    <div class="cta-price">$10<small>/mes</small></div>
    <div class="cta-trial">7 DÍAS GRATIS</div>
    <div class="cta-slogan">Tu Negocio, Tu Control.</div>
</div>
```

**IMPORTANTE — NO procesar el logo:**
- El logo actual ya tiene transparencia PERFECTA nativa (generada por Picsart BackgroundRemover)
- NUNCA aplicar flood fill, background removal, ni ningún procesamiento adicional
- NUNCA usar un logo con fondo checkerboard falso — siempre usar el archivo con transparencia real
- Si se necesita re-generar los pre-escalados: solo `Image.crop(getbbox())` + `Image.resize(LANCZOS)`

---

## Tipografía e Iconos

```html
<!-- Google Fonts — Montserrat Bold -->
<link href="https://fonts.googleapis.com/css2?family=Montserrat:wght@400;600;700;800;900&display=swap" rel="stylesheet">

<!-- Bootstrap Icons CDN -->
<link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet">
```

- **Títulos/slogans:** Montserrat 700-900 (Bold/Extra Bold/Black)
- **Cuerpo:** Montserrat 400-600
- **Iconos:** Bootstrap Icons (bi-people, bi-cash-stack, bi-calendar-check, bi-bell, bi-receipt, bi-bar-chart, bi-scissors, bi-bag, bi-shop, etc.)

---

## Estructura de Carpetas

```
~/Desktop/marketing/
├── general/       → flyers/ + videos/
├── restaurantes/  → flyers/ + videos/
├── membresias/    → flyers/ + videos/
├── barberias/     → flyers/ + videos/
└── tiendas/       → flyers/ + videos/
```

Nomenclatura de archivos: `{categoria}-{numero}.{png|mp4}`
- Ejemplo: `general-1.png`, `restaurantes-3.mp4`
- Números continúan desde el último existente (verificar con `ls` antes de crear)

---

## Categorías y Contenido

### 5 Categorías de Negocio

| Categoría | Enfoque | Features principales |
|-----------|---------|---------------------|
| **General** | Marca My-Negocio, propuesta de valor, multi-negocio | Brand, pricing ($10/mes), 7 días gratis, 4 tipos de negocio, comparativas, seguridad, multi-dispositivo |
| **Membresías** | Gimnasios, academias, clubes | Clientes, pagos, membresías, vencimientos, recordatorios, dashboard, reportes, asistencia, planes |
| **Barberías** | Barberías, salones, spas | Agenda por empleado, conflictos de citas, confirmaciones, servicios+precios, historial cliente, ganancias por barbero |
| **Restaurantes** | Restaurantes, cafeterías, bares | Control de mesas, menú digital, reservas, POS, pedidos, analytics de platos, equipo |
| **Tiendas** | Tiendas de ropa, accesorios, etc. | POS/recibos, inventario+stock, catálogo digital, movimientos, alertas de stock, reportes de ventas |

### Temas Recurrentes por Flyer

Cada categoría debe cubrir estos arquetipos de contenido (no todos obligatorios):

1. **Hero/Feature principal** — La feature estrella de la categoría con mockup visual
2. **Problema → Solución** — Lo que pasa sin sistema vs con My-Negocio
3. **Features Grid/List** — Múltiples features con iconos
4. **CTA + Precio** — "$10/mes" + lista de features + "PROBAR 7 DÍAS GRATIS"
5. **Dashboard/Mockup** — Simulación de cómo se ve el sistema
6. **Estadísticas/Social Proof** — Números, testimonios, datos
7. **Comparativa** — Antes vs Después, Sin vs Con
8. **Proceso/Pasos** — Flujo visual de cómo funciona

### Temas Recurrentes por Video

1. **Feature Demo** — Una feature animándose paso a paso
2. **Montaje/Reel** — Varias features en secuencia rápida
3. **Flujo Completo** — De inicio a fin (registro→uso→resultado)
4. **Counter/Stats** — Números contando, gráficos construyéndose
5. **Before/After** — Transición de caos a orden
6. **CTA/Pricing** — Precio animado + features + call to action

---

## Especificaciones Técnicas de Flyers

### Formato
- **Dimensiones:** 1080 x 1080 px (cuadrado, ideal para Instagram/Facebook)
- **Formato salida:** PNG
- **Viewport Playwright:** `{ width: 1080, height: 1080 }`

### Estructura HTML de un Flyer

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=1080, height=1080">
    <link href="https://fonts.googleapis.com/css2?family=Montserrat:wght@400;600;700;800;900&display=swap" rel="stylesheet">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet">
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            width: 1080px;
            height: 1080px;
            overflow: hidden;
            font-family: 'Montserrat', sans-serif;
            /* background según diseño */
        }
        /* ... resto del CSS ... */
    </style>
</head>
<body>
    <!-- Contenido del flyer -->
</body>
</html>
```

### Principios de Diseño para Flyers

1. **Jerarquía visual:** Título grande (40-72px) → Subtítulo (20-28px) → Contenido → CTA → URL
2. **Espaciado generoso:** padding mínimo 40px en bordes, gap entre elementos 20-30px
3. **Sombras suaves:** `box-shadow: 0 4px 20px rgba(0,0,0,0.1)` en tarjetas
4. **Bordes redondeados:** `border-radius: 12-16px` en tarjetas, `20-30px` en botones
5. **Mockups CSS-only:** Simular phones, laptops, receipts, calendarios, charts con CSS puro
6. **Marca siempre visible:** "MY-NEGOCIO" o "My-Negocio" (con guion) + "my-negocio.com" en cada flyer
7. **Sin fotos reales** — todo gráfico debe ser CSS/SVG/iconos
8. **Sin WhatsApp** — no mencionar ni mostrar WhatsApp
9. **Sin información sensible** — no emails reales, no teléfonos reales, no credenciales
10. **Barra inferior gradiente** — la mayoría de flyers deben tener una barra en la parte inferior con el gradiente y "my-negocio.com"

### Captura con Playwright (Flyers)

```javascript
const browser = await chromium.launch();
const page = await browser.newPage();
await page.setViewportSize({ width: 1080, height: 1080 });
await page.goto(`file://${htmlPath}`);
await page.waitForLoadState('networkidle');
await page.waitForTimeout(2000); // Esperar fonts
await page.screenshot({ path: outputPath, type: 'png' });
await page.close();
```

---

## Especificaciones Técnicas de Videos

### Formato
- **Dimensiones:** 1080 x 1920 px (vertical/portrait, ideal para Reels/Stories/TikTok)
- **Duración:** 15-18 segundos (máximo 20s)
- **Formato salida:** MP4 (H.264)
- **FPS:** ~25 (determinado por Playwright recording)

### Estructura HTML de un Video

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=1080, height=1920">
    <link href="https://fonts.googleapis.com/css2?family=Montserrat:wght@400;600;700;800;900&display=swap" rel="stylesheet">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet">
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            width: 1080px;
            height: 1920px;
            overflow: hidden;
            font-family: 'Montserrat', sans-serif;
            background: linear-gradient(135deg, #0047AB, #00D4FF);
        }

        /* === SISTEMA DE ESCENAS === */
        .scene {
            position: absolute;
            top: 0; left: 0;
            width: 100%; height: 100%;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            opacity: 0;
            padding: 60px;
        }

        /* Escena 1: 0-4s */
        .scene-1 { animation: fadeScene 4s ease-in-out 0s forwards; }
        /* Escena 2: 4-8s */
        .scene-2 { animation: fadeScene 4s ease-in-out 4s forwards; }
        /* Escena 3: 8-12s */
        .scene-3 { animation: fadeScene 4s ease-in-out 8s forwards; }
        /* Escena 4: 12-15s */
        .scene-4 { animation: fadeScene 3s ease-in-out 12s forwards; }
        /* Escena 5: 15-18s */
        .scene-5 { animation: fadeScene 3s ease-in-out 15s forwards; }

        @keyframes fadeScene {
            0% { opacity: 0; transform: translateY(30px); }
            15% { opacity: 1; transform: translateY(0); }
            85% { opacity: 1; transform: translateY(0); }
            100% { opacity: 0; transform: translateY(-20px); }
        }

        /* === SUBTÍTULO FIJO === */
        .subtitle-bar {
            position: fixed;
            bottom: 0;
            left: 0;
            width: 100%;
            padding: 30px 40px;
            background: linear-gradient(transparent, rgba(0,0,0,0.7));
            z-index: 100;
        }
        .subtitle-bar p {
            color: white;
            font-size: 28px;
            font-weight: 700;
            text-align: center;
            text-shadow: 2px 2px 8px rgba(0,0,0,0.8);
        }

        /* === ANIMACIONES COMUNES === */
        @keyframes slideUp {
            from { opacity: 0; transform: translateY(50px); }
            to { opacity: 1; transform: translateY(0); }
        }
        @keyframes slideFromLeft {
            from { opacity: 0; transform: translateX(-80px); }
            to { opacity: 1; transform: translateX(0); }
        }
        @keyframes slideFromRight {
            from { opacity: 0; transform: translateX(80px); }
            to { opacity: 1; transform: translateX(0); }
        }
        @keyframes scaleIn {
            from { opacity: 0; transform: scale(0.5); }
            to { opacity: 1; transform: scale(1); }
        }
        @keyframes pulse {
            0%, 100% { transform: scale(1); }
            50% { transform: scale(1.05); }
        }
        @keyframes countUp {
            from { opacity: 0; }
            to { opacity: 1; }
        }
    </style>
</head>
<body>
    <!-- Escenas del video -->
    <div class="scene scene-1"><!-- Contenido escena 1 --></div>
    <div class="scene scene-2"><!-- Contenido escena 2 --></div>
    <div class="scene scene-3"><!-- Contenido escena 3 --></div>
    <div class="scene scene-4"><!-- Contenido escena 4 --></div>
    <div class="scene scene-5"><!-- CTA final --></div>

    <!-- Subtítulo fijo -->
    <div class="subtitle-bar">
        <p>Texto del subtítulo</p>
    </div>
</body>
</html>
```

### Principios de Diseño para Videos

1. **4-5 escenas** de 3-4 segundos cada una
2. **Transiciones suaves:** fade + translateY entre escenas
3. **Subtítulo fijo** en la parte inferior (blanco, bold, text-shadow)
4. **Última escena siempre CTA:** "my-negocio.com" + precio o "7 días gratis"
5. **Elementos animados dentro de escenas:** slide, scale, pulse, countUp
6. **Fondos:** gradiente o #2C3E50 oscuro (nunca blanco puro para video)
7. **Texto grande:** mínimo 36px para títulos en video, 28px para subtítulos
8. **Sin audio** — solo visual

### Captura con Playwright (Videos)

```javascript
const context = await browser.newContext({
    viewport: { width: 1080, height: 1920 },
    recordVideo: {
        dir: tempVideoDir,
        size: { width: 1080, height: 1920 }
    }
});
const page = await context.newPage();
await page.goto(`file://${htmlPath}`);
await page.waitForLoadState('networkidle');
await page.waitForTimeout(2000); // Esperar que carguen fonts
await page.waitForTimeout(animationDurationMs); // Esperar animación completa
await page.close();
await context.close(); // Esto guarda el video .webm

// El video se guarda como .webm en tempVideoDir
// Encontrar el archivo .webm generado y convertir
```

### Conversión WebM → MP4

```bash
ffmpeg -i input.webm \
  -c:v libx264 \
  -preset medium \
  -crf 23 \
  -c:a aac \
  -movflags +faststart \
  output.mp4
```

---

## Script de Captura Completo (Template)

```javascript
const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');
const { execSync } = require('child_process');

const MARKETING_DIR = path.join(require('os').homedir(), 'Desktop', 'marketing');
const TEMP_DIR = path.join(MARKETING_DIR, '_temp_capture');

async function captureFlyer(browser, htmlPath, outputPath) {
    const page = await browser.newPage();
    await page.setViewportSize({ width: 1080, height: 1080 });
    await page.goto(`file://${htmlPath}`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    await page.screenshot({ path: outputPath, type: 'png' });
    await page.close();
    console.log(`✓ Flyer: ${path.basename(outputPath)}`);
}

async function captureVideo(browser, htmlPath, outputPath, durationMs) {
    const videoDir = path.join(TEMP_DIR, 'videos_raw');
    fs.mkdirSync(videoDir, { recursive: true });

    const context = await browser.newContext({
        viewport: { width: 1080, height: 1920 },
        recordVideo: { dir: videoDir, size: { width: 1080, height: 1920 } }
    });
    const page = await context.newPage();
    await page.goto(`file://${htmlPath}`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000 + durationMs);

    const video = page.video();
    await page.close();
    await context.close();

    const webmPath = await video.path();

    // Convertir WebM → MP4
    execSync(`ffmpeg -y -i "${webmPath}" -c:v libx264 -preset medium -crf 23 -c:a aac -movflags +faststart "${outputPath}"`, { stdio: 'pipe' });

    console.log(`✓ Video: ${path.basename(outputPath)}`);
}

async function main() {
    fs.mkdirSync(TEMP_DIR, { recursive: true });
    const browser = await chromium.launch();

    try {
        // === FLYERS ===
        // await captureFlyer(browser, 'path/to/flyer.html', 'path/to/output.png');

        // === VIDEOS ===
        // await captureVideo(browser, 'path/to/video.html', 'path/to/output.mp4', 18000);

    } finally {
        await browser.close();
    }

    console.log('\n=== Captura completa ===');
}

main().catch(console.error);
```

---

## Distribución de Agentes

### Para generación estándar (20 flyers + 20 videos = 40 assets)

Lanzar **4 agentes Opus en paralelo:**

| Agente | Categorías | Flyers | Videos | Total |
|--------|-----------|--------|--------|-------|
| **Agente 1** | General + Membresías | 8 | 8 | 16 |
| **Agente 2** | Restaurantes | 4 | 4 | 8 |
| **Agente 3** | Barberías | 4 | 4 | 8 |
| **Agente 4** | Tiendas | 4 | 4 | 8 |

### Para generación ampliada (40+ assets)

Lanzar **5 agentes Opus** (1 por categoría) o ejecutar en 2 tandas de 4 agentes.

### Instrucciones para cada agente

Cada agente debe recibir en su prompt:
1. La paleta de colores completa
2. Las URLs de Google Fonts y Bootstrap Icons
3. Los paths exactos de salida
4. La descripción detallada de CADA flyer y video (tema, layout, textos, escenas)
5. El script de captura o las instrucciones técnicas
6. La instrucción de limpiar archivos temporales al final

---

## Reglas de Contenido

### SIEMPRE incluir:
- Marca "My-Negocio" (con guion, capital M y N) en cada asset
- URL "my-negocio.com" en cada asset
- Precio "$10/mes" en al menos 1 flyer y 1 video por categoría
- "7 días gratis" o "PROBAR GRATIS" en CTAs
- Iconos Bootstrap Icons para features
- Mockups CSS-only (dashboards, phones, calendarios, recibos, charts)

### NUNCA incluir:
- Fotos reales de personas o productos
- Referencias a WhatsApp (ni logo, ni texto, ni número)
- Información sensible (emails reales, teléfonos reales, contraseñas)
- URLs distintas a "my-negocio.com"
- Logos de terceros
- Contenido en inglés (todo en español)
- Fuentes que no sean Montserrat
- Colores fuera de la paleta definida

---

## Verificación Final

Después de que todos los agentes completen, ejecutar:

```bash
# Contar archivos generados
echo "=== INVENTARIO ==="
for cat in general membresias barberias restaurantes tiendas; do
  flyers=$(find ~/Desktop/marketing/$cat/flyers -name "*.png" | wc -l)
  videos=$(find ~/Desktop/marketing/$cat/videos -name "*.mp4" | wc -l)
  echo "$cat: $flyers flyers, $videos videos"
done
echo "Flyers totales: $(find ~/Desktop/marketing -name '*.png' -path '*/flyers/*' | wc -l)"
echo "Videos totales: $(find ~/Desktop/marketing -name '*.mp4' -path '*/videos/*' | wc -l)"
```

### Checklist de calidad:
- [ ] Todos los archivos existen y tienen tamaño > 0
- [ ] Flyers son 1080x1080
- [ ] Videos son 1080x1920 vertical y duran 15-20s
- [ ] La marca "My-Negocio" aparece en todos los assets
- [ ] "my-negocio.com" aparece en todos los assets
- [ ] No hay fotos, no hay WhatsApp, no hay info sensible
- [ ] Paleta de colores consistente
- [ ] Tipografía Montserrat en todo
- [ ] Archivos temporales limpiados

---

## Assets Existentes Aprobados (referencia)

### Flyers aprobados (39 total):
- general: 1-8 (8/8)
- membresias: 2-8 (7/8, el #1 fue descartado — dashboard con stat cards)
- barberias: 1-8 (8/8)
- restaurantes: 1-8 (8/8)
- tiendas: 1-8 (8/8)

### Videos aprobados (40 total):
- general: 1-8 (8/8)
- membresias: 1-8 (8/8)
- barberias: 1-8 (8/8)
- restaurantes: 1-8 (8/8)
- tiendas: 1-8 (8/8)

### Temas ya cubiertos (NO repetir):

**General (1-8):**
1. Hero Brand — logo + tagline + 4 iconos negocio
2. Oferta Trial — "7 DÍAS GRATIS" + features
3. Features Grid — 6 cards con iconos
4. Social Proof — estadísticas + testimonios
5. Comparativa — Sin sistema vs Con My-Negocio
6. Multi-dispositivo — laptop + tablet + phone
7. Seguridad — escudo + features de seguridad
8. Velocidad — "Configura en 5 minutos" + 3 pasos

**Membresías (2-8, #1 descartado):**
2. Recordatorios — "60% menos atrasos" + notificación
3. Reportes — chart mockup + periodos
4. CTA Precio — "$10/mes" + features
5. Asistencia — check-in card + gráfico semanal
6. Vencimientos — timeline verde→rojo
7. Planes — 3 pricing cards (Básico/Premium/VIP)
8. Dashboard Completo — stats + charts + tabla actividad

**Barberías (1-8):**
1. Agenda — calendar por barbero (Carlos, Miguel, Pedro)
2. Sin Conflictos — citas cruzadas bloqueadas
3. Confirmación Automática — notificación mockup
4. CTA — precio + features
5. Servicios — 6 service cards con precios
6. Historial — perfil de cliente + visitas
7. Ganancias — earnings por barbero + progress bars
8. Crece tu equipo — timeline de crecimiento

**Restaurantes (1-8):**
1. Mesas — grid 4x3 colores (libre/reservada/ocupada)
2. Menú Digital — phone mockup con menú
3. POS — flujo orden→cobrar→recibo
4. CTA — precio + features
5. Gestión de Pedidos — cola de 4 pedidos con badges
6. Analytics — bar chart horizontal top 5 platos
7. Gestiona tu equipo — grilla semanal turnos
8. Pedidos para llevar — flujo 3 pasos

**Tiendas (1-8):**
1. POS — receipt mockup + productos
2. Inventario — 4 product cards con stock badges
3. Catálogo — phone mockup con catálogo
4. CTA — precio + features
5. Movimientos — tabla de historial stock
6. Reportes — dashboard ventas + line chart
7. Alertas — 3 alert cards (agotado/bajo/restock)
8. Todo en Uno — 4 circles conectados (POS/Inventario/Catálogo/Reportes)

**Videos ya cubiertos** (mismos temas que flyers + variaciones animadas: montajes, flujos completos, counters, before/after).

Al generar nuevos assets, consultar esta lista y crear temas DIFERENTES.
