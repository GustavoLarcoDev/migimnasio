# Protocolo 004 — Smart Development Pipeline

Pipeline inteligente de desarrollo que automatiza: clasificación de tareas → implementación con 4 agentes → actualización de tutoriales/changelog → tests y auditorías focalizadas.

## Activación

El usuario dice **"run protocolo 004"** o **"protocolo 004"** seguido de un todo list de tareas a implementar.

---

## Fase 0 — Clasificación de Tareas

**Entrada:** Todo list del usuario (texto libre, lista numerada, o bullet points).

**Proceso:**
1. Lanzar **4 agentes Opus 4.6 en paralelo** para clasificar las tareas:
   - **Agente Clasificador A** — Backend: Models, Services, DbContext, Migrations
   - **Agente Clasificador B** — Controllers: endpoints, auth, routing, validación
   - **Agente Clasificador C** — Frontend: Views, JS, CSS, modals, tablas
   - **Agente Clasificador D** — Integración: Background services, email, WhatsApp, config

2. Cada agente analiza TODAS las tareas del todo list y determina:
   - Archivos afectados (lista exacta con paths)
   - Complejidad estimada (baja / media / alta)
   - Dependencias entre tareas (qué debe ir primero)
   - Si la tarea le corresponde a su dominio o es compartida

3. Compilar tabla unificada:

| # | Tarea | Dominio | Archivos | Complejidad | Depende de |
|---|-------|---------|----------|-------------|------------|
| 1 | ... | Backend + Frontend | ... | Media | — |

4. Presentar al usuario para aprobación.
5. Si hay ambigüedad → preguntar antes de continuar.

**Salida:** Lista aprobada de tareas con asignación de agentes.

---

## Fase 1 — Implementación

Lanzar **4 agentes Opus 4.6** con permisos completos de lectura/escritura:

### Agente A — Backend
- Models (`Models/*.cs`)
- Services (`Services/*.cs`)
- ApplicationDbContext, Migrations
- Ejecuta `dotnet build` al terminar

### Agente B — Controllers
- Todos los controllers (`Controllers/*.cs`)
- Endpoints, autorización, routing
- Validación de entrada, respuestas HTTP
- Ejecuta `dotnet build` al terminar

### Agente C — Frontend
- Views (`Views/**/*.cshtml`)
- JavaScript (`wwwroot/js/**/*.js`)
- CSS, modals, tablas, componentes UI
- Ejecuta `dotnet build` al terminar

### Agente D — Integración
- Background services (`Services/*Service.cs` de background)
- EmailService, WhatsAppService
- Configuración (`appsettings*.json`, `Program.cs`)
- Ejecuta `dotnet build` al terminar

### Reglas de coordinación:
- Si dos agentes necesitan editar el mismo archivo → el agente de menor letra tiene prioridad
- Cada agente debe respetar convenciones del proyecto (ver CLAUDE.md)
- Si un agente encuentra un conflicto → reportar y esperar resolución
- Al terminar, cada agente reporta: archivos creados/modificados, tests necesarios

---

## Fase 2 — Actualización Post-Implementación

**Ejecutada por el agente principal (no sub-agentes).**

### 2a. Tutoriales (si hay cambios visuales)
- Actualizar los 4 archivos de tutorial:
  - `wwwroot/js/tutorials/tutorial-membresias.js`
  - `wwwroot/js/tutorials/tutorial-artesanal.js`
  - `wwwroot/js/tutorials/tutorial-tienda.js`
  - `wwwroot/js/tutorials/tutorial-restaurante.js`
- Solo si se agregaron/movieron/eliminaron elementos de UI visibles

### 2b. Changelog
- Generar objeto JSON con todos los cambios implementados
- Auto-incrementar versión (patch para fixes, minor para features, major para breaking changes)
- Cada cambio clasificado como: `feature`, `fix`, `mejora`, `eliminado`

### 2c. Formato del entry en changelog.json:
```json
{
  "version": "1.1.0",
  "fecha": "2026-02-27",
  "titulo": "Título descriptivo del release",
  "cambios": [
    { "tipo": "feature", "descripcion": "Descripción del cambio" },
    { "tipo": "fix", "descripcion": "Descripción del fix" }
  ]
}
```

---

## Fase 3 — Protocolo 002 Focalizado (opcional)

**Preguntar al usuario:** "¿Quieres ejecutar tests focalizados en los cambios?"

Si acepta:
1. Generar test cases SOLO para features nuevas/cambiadas
2. Si un cambio afecta funcionalidad existente, incluir esos tests también
3. Ejecutar Phase 0 de Protocolo 002 (build + start app + seed)
4. Lanzar solo los agentes de test relevantes (no los 8)
5. Si hay fallos → ejecutar Fix Agents

---

## Fase 4 — Protocolo 001 Focalizado (opcional)

**Preguntar al usuario:** "¿Quieres auditoría de seguridad del código nuevo?"

Si acepta:
1. Identificar qué dominios del Protocolo 001 son relevantes
2. Lanzar solo esos agentes (ej: si solo cambió frontend → solo Agente 5)
3. Foco en código nuevo con estándares máximos
4. Reportar hallazgos y ofrecer auto-fix

---

## Fase 5 — Tab Admin Changelog

Actualizar el changelog en el panel admin:

1. **Prepend** nueva entrada al inicio del array en `wwwroot/data/changelog.json`
2. El tab "Actualizaciones" en el sidebar admin carga este JSON estático
3. No requiere endpoint de controller — es archivo estático servido por ASP.NET

### Diseño del tab:
- Accordions de Bootstrap 5 (jerárquico, no tabular)
- Primera versión expandida, resto colapsadas
- Badges de colores por tipo:
  - `feature` → `bg-primary`
  - `fix` → `bg-danger`
  - `mejora` → `bg-success`
  - `eliminado` → `bg-warning text-dark`
- Cache-bust: `$.getJSON('/data/changelog.json?_=' + Date.now())`
- Prevención XSS con `escHtml()` en todo texto del JSON
- Compatible con los 4 temas vía CSS variables de Metronic

---

## Ejemplo de Uso

```
Usuario: protocolo 004

Claude: Dame tu lista de tareas para implementar.

Usuario:
1. Agregar campo "teléfono" a clientes de membresías
2. Fix: el botón de renovar no muestra el precio correcto
3. Agregar exportar PDF de recibos
4. Cambiar color del badge de "vencido" a rojo

Claude: [Ejecuta Fase 0 — Clasificación]
→ Presenta tabla de tareas clasificadas
→ Usuario aprueba

Claude: [Ejecuta Fase 1 — Implementación con 4 agentes]
→ Agente A: agrega campo teléfono al modelo + migración
→ Agente B: actualiza endpoints de cliente
→ Agente C: actualiza formularios + badge CSS + botón renovar
→ Agente D: no aplica

Claude: [Ejecuta Fase 2 — Post-implementación]
→ Actualiza tutoriales si aplica
→ Genera changelog v1.1.0

Claude: ¿Quieres ejecutar tests focalizados? (Fase 3)
Claude: ¿Quieres auditoría de seguridad? (Fase 4)

Claude: [Fase 5 — Actualiza changelog.json]
→ Prepend nueva entrada
→ Tab admin ya funcional
```

---

## Reglas Generales

1. **NUNCA** registrar servicios en `Program.cs` para código que no está commiteado
2. **NUNCA** agregar filtros globales de ModelState
3. **NUNCA** usar CDN externo para i18n de DataTables
4. Usar `TimeHelper.Now` en vez de `DateTime.Now`
5. Background services usan `IServiceScopeFactory`
6. Todos los controllers de negocio filtran por `NegocioId`
7. `dotnet build` debe pasar con 0 errores al final de cada fase
