# Protocolo SRI — Facturación Electrónica Ecuador

Documentación completa del módulo de facturación electrónica del SRI implementado en My-Negocio. Incluye arquitectura, flujo técnico, configuración, y guía de mantenimiento.

## Activación

El usuario dice **"protocolo SRI"** o **"run protocolo SRI"** para consultar esta documentación o ejecutar tareas relacionadas con facturación electrónica.

---

## Resumen del Módulo

My-Negocio permite a los negocios emitir **facturas electrónicas** válidas ante el SRI (Servicio de Rentas Internas) de Ecuador. El flujo es:

1. Negocio activa facturación en su perfil → configura datos SRI → sube certificado .p12
2. Al generar un recibo, aparece botón **"Facturar"**
3. Click → modal con datos del comprador → "Emitir Factura"
4. Backend: genera XML SRI → firma XAdES-BES → envía SOAP al SRI → recibe autorización → genera RIDE PDF
5. Tab "Facturación" en dashboard para consultar, descargar, reenviar facturas

---

## Archivos del Módulo

### Archivos Nuevos

| Archivo | Propósito |
|---------|-----------|
| `Models/FacturaElectronica.cs` | Modelo EF Core de factura electrónica |
| `Models/FacturacionSettings.cs` | Options class (`IOptions<FacturacionSettings>`) |
| `Services/IFacturacionElectronicaService.cs` | Interface + DTOs (FacturaRequest, FacturaItemRequest, ValidacionConfigResult) |
| `Services/FacturacionElectronicaService.cs` | Implementación completa (~800 líneas) |
| `Services/FacturaReintentoService.cs` | Background service: reintento cada 10 min |
| `Controllers/FacturacionController.cs` | 13 endpoints, hereda NegocioBaseController |
| `Helpers/CertificadoEncryptionHelper.cs` | AES-256-GCM para cifrar contraseña .p12 |

### Archivos Modificados

| Archivo | Cambio |
|---------|--------|
| `Models/Negocio.cs` | +15 campos SRI en clase `Gym` |
| `Data/ApplicationDbContext.cs` | DbSet + 4 índices + FK config |
| `Program.cs` | Registro de servicio, options, HttpClient("SRI") |
| `Services/BackgroundServicesRegistration.cs` | Registro de FacturaReintentoService |
| `Services/IEmailService.cs` | +SendEmailWithAttachmentsAsync |
| `Services/EmailService.cs` | Implementación con adjuntos (XML + PDF) |
| `Controllers/ClientesController.cs` | +campos SRI en GetPerfilNegocio |
| `Views/Shared/_DashboardLayout.cshtml` | Sidebar link + JS completo + modal facturar |
| `Views/Negocios/Dashboard.cshtml` | Config SRI en perfil + tab facturación |
| `Views/Negocios/DashboardArtesanal.cshtml` | Idem |
| `Views/Negocios/DashboardTienda.cshtml` | Idem + botón facturar en recibo |
| `Views/Negocios/DashboardRestaurante.cshtml` | Idem |
| `Gimnasio.csproj` | +QRCoder 1.6.0, +System.Security.Cryptography.Xml 9.0.0 |
| `appsettings.json` | +sección FacturacionSettings |
| `Migrations/20260228140144_AddFacturacionElectronica.cs` | Migración EF Core |

---

## Modelo de Datos

### Campos agregados a `Gym` (Negocios)

```csharp
// FACTURACION ELECTRONICA SRI
bool FacturacionElectronicaActiva = false
string Ruc                    // MaxLength(13)
string RazonSocial            // MaxLength(300)
string NombreComercial        // MaxLength(300)
string DireccionMatriz        // MaxLength(500)
string CodigoEstablecimiento  // MaxLength(3), default "001"
string PuntoEmision           // MaxLength(3), default "001"
string CertificadoP12Base64   // nvarchar(max) — certificado completo en Base64
string CertificadoPasswordEncriptado // MaxLength(500) — AES-256-GCM
int SriAmbiente               // 1=Pruebas, 2=Producción
bool ObligadoContabilidad     // default false
string ContribuyenteEspecial  // MaxLength(20)
string RegimenContribuyente   // MaxLength(50)
string AgenteRetencion        // MaxLength(20)
```

### Tabla `FacturasElectronicas`

```
FacturaId               GUID PK
NegocioId               GUID FK → Negocios (Cascade)
ReciboId                GUID? FK → Recibos (SetNull)
Establecimiento         nvarchar(3)
PuntoEmision            nvarchar(3)
Secuencial              int
NumeroCompleto          nvarchar(21)    — "001-001-000000001"
ClaveAcceso             nvarchar(49)    — 49 dígitos
CompradorIdentificacion nvarchar(20)
CompradorTipoIdentificacion nvarchar(2) — "04"=RUC, "05"=Cédula, "06"=Pasaporte, "07"=ConsumidorFinal
CompradorRazonSocial    nvarchar(300)
CompradorEmail          nvarchar(200)
CompradorDireccion      nvarchar(500)
TotalSinImpuestos       decimal(18,2)
TotalDescuento          decimal(18,2)
MontoIva                decimal(18,2)
ImporteTotal            decimal(18,2)
FormaPagoSri            nvarchar(2)     — "01"=Efectivo, "16"=Débito, "19"=Crédito, "20"=Transferencia
XmlAutorizadoComprimido varbinary(max)  — GZip comprimido
EstadoSri               nvarchar(20)    — Pendiente|Recibida|Autorizada|NoAutorizada|Anulada
NumeroAutorizacion      nvarchar(49)
FechaAutorizacion       datetime2
MensajesSri             nvarchar(max)
RidePdf                 varbinary(max)
DetalleItemsJson        nvarchar(max)
IntentosEnvio           int
FechaEmision            datetime2
FechaCreacion           datetime2
FechaUltimoIntento      datetime2?
```

### Índices

| Nombre | Columnas | Tipo |
|--------|----------|------|
| `IX_FacturasElectronicas_NegocioId_ClaveAcceso` | NegocioId, ClaveAcceso | Unique (filtered: ClaveAcceso IS NOT NULL) |
| `IX_FacturasElectronicas_Numeracion` | NegocioId, Establecimiento, PuntoEmision, Secuencial | Unique (filtered) |
| `IX_FacturasElectronicas_NegocioId_EstadoSri` | NegocioId, EstadoSri | Non-unique |
| `IX_FacturasElectronicas_NegocioId_FechaEmision` | NegocioId, FechaEmision | Non-unique |

---

## Flujo Técnico de Emisión

### 1. Generación XML (Esquema SRI v1.1.0)

```xml
<factura id="comprobante" version="1.1.0">
  <infoTributaria>
    <ambiente>1|2</ambiente>
    <tipoEmision>1</tipoEmision>
    <razonSocial>...</razonSocial>
    <ruc>...</ruc>
    <claveAcceso>49 dígitos</claveAcceso>
    <codDoc>01</codDoc>  <!-- 01 = Factura -->
    <estab>001</estab>
    <ptoEmi>001</ptoEmi>
    <secuencial>000000001</secuencial>
    <dirMatriz>...</dirMatriz>
  </infoTributaria>
  <infoFactura>
    <fechaEmision>DD/MM/YYYY</fechaEmision>
    <obligadoContabilidad>SI|NO</obligadoContabilidad>
    <tipoIdentificacionComprador>04|05|06|07</tipoIdentificacionComprador>
    <razonSocialComprador>...</razonSocialComprador>
    <identificacionComprador>...</identificacionComprador>
    <totalSinImpuestos>100.00</totalSinImpuestos>
    <totalDescuento>0.00</totalDescuento>
    <totalConImpuestos>
      <totalImpuesto>
        <codigo>2</codigo>           <!-- IVA -->
        <codigoPorcentaje>4</codigoPorcentaje> <!-- 15% -->
        <baseImponible>100.00</baseImponible>
        <valor>15.00</valor>
      </totalImpuesto>
    </totalConImpuestos>
    <importeTotal>115.00</importeTotal>
    <moneda>DOLAR</moneda>
    <pagos>
      <pago>
        <formaPago>01</formaPago>
        <total>115.00</total>
      </pago>
    </pagos>
  </infoFactura>
  <detalles>
    <detalle>
      <codigoPrincipal>PROD001</codigoPrincipal>
      <descripcion>...</descripcion>
      <cantidad>1</cantidad>
      <precioUnitario>100.00</precioUnitario>
      <descuento>0.00</descuento>
      <precioTotalSinImpuesto>100.00</precioTotalSinImpuesto>
      <impuestos>
        <impuesto>
          <codigo>2</codigo>
          <codigoPorcentaje>4</codigoPorcentaje>
          <tarifa>15</tarifa>
          <baseImponible>100.00</baseImponible>
          <valor>15.00</valor>
        </impuesto>
      </impuestos>
    </detalle>
  </detalles>
  <infoAdicional>
    <campoAdicional nombre="Email">cliente@email.com</campoAdicional>
    <campoAdicional nombre="Direccion">...</campoAdicional>
  </infoAdicional>
</factura>
```

### 2. Clave de Acceso (49 dígitos)

```
Posición | Largo | Contenido
---------|-------|----------
1-8      | 8     | Fecha emisión (DDMMAAAA)
9-10     | 2     | Tipo documento ("01" = Factura)
11-23    | 13    | RUC del emisor
24       | 1     | Ambiente (1=Pruebas, 2=Producción)
25-27    | 3     | Establecimiento
28-30    | 3     | Punto de emisión
31-39    | 9     | Secuencial (zero-padded)
40-47    | 8     | Código numérico (random 8 dígitos)
48       | 1     | Tipo emisión ("1" = Normal)
49       | 1     | Dígito verificador (Módulo 11)
```

**Algoritmo Módulo 11:**
- Factores: 2,3,4,5,6,7 (cíclico de derecha a izquierda sobre los primeros 48 dígitos)
- Suma ponderada → 11 - (suma % 11)
- Si resultado = 11 → 0, si resultado = 10 → 1

### 3. Firma Digital XAdES-BES

- Carga certificado .p12 con `X509CertificateLoader.LoadPkcs12()` (.NET 9)
- Contraseña descifrada con AES-256-GCM (`CertificadoEncryptionHelper.Decrypt`)
- Firma enveloped signature usando `System.Security.Cryptography.Xml`
- `SignedXml` con `Reference` URI="" + transforms (enveloped-signature + exc-c14n)
- `KeyInfo` con `KeyInfoX509Data` (certificado completo)

### 4. Envío SOAP al SRI

**Endpoints:**

| Ambiente | Recepción | Autorización |
|----------|-----------|--------------|
| Pruebas | `https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl` | `https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl` |
| Producción | `https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl` | `https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl` |

**Flujo:**
1. `POST validarComprobante` → envía XML firmado en Base64 dentro de SOAP envelope
2. Respuesta: `RECIBIDA` (éxito) o `DEVUELTA` (error con mensajes)
3. Esperar 3-5 segundos
4. `POST autorizacionComprobante` → consulta por claveAcceso
5. Respuesta: `AUTORIZADO` (con número y fecha autorización) o `NO AUTORIZADO` (con errores)

**SOAP Envelope (Recepción):**
```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                  xmlns:ec="http://ec.gob.sri.ws.recepcion">
  <soapenv:Body>
    <ec:validarComprobante>
      <xml>{XML_FIRMADO_BASE64}</xml>
    </ec:validarComprobante>
  </soapenv:Body>
</soapenv:Envelope>
```

**SOAP Envelope (Autorización):**
```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                  xmlns:ec="http://ec.gob.sri.ws.autorizacion">
  <soapenv:Body>
    <ec:autorizacionComprobante>
      <claveAccesoComprobante>{CLAVE_ACCESO_49}</claveAccesoComprobante>
    </ec:autorizacionComprobante>
  </soapenv:Body>
</soapenv:Envelope>
```

### 5. Post-Autorización

- XML autorizado se comprime con GZip → `XmlAutorizadoComprimido`
- Se genera RIDE PDF (pendiente implementación completa con Select.HtmlToPdf)
- Se envía por email al comprador (XML + PDF como adjuntos)

---

## Endpoints del Controller

`FacturacionController` hereda `NegocioBaseController`. Ruta base: `[Route("Negocios")]`

### Configuración

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `GetConfigFacturacion?negocioId=` | Valida configuración + certificado |
| POST | `ToggleFacturacion?negocioId=&activa=` | Activa/desactiva facturación |
| POST | `GuardarConfigFacturacion` | Guarda todos los campos SRI |

### Certificado

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `SubirCertificado` | FormData: certificado (.p12/.pfx) + password |
| GET | `ValidarCertificado?negocioId=` | Verifica validez y vencimiento |

### Emisión

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `EmitirFactura` | Body JSON: FacturaRequest completo |
| POST | `EmitirFacturaDesdeRecibo` | Body JSON: FacturaRequest con ReciboId |

### Consultas

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `GetFacturas?negocioId=&desde=&hasta=&estado=` | Lista con filtros |
| GET | `GetFactura?negocioId=&facturaId=` | Detalle completo |
| GET | `GetEstadisticasFacturacion?negocioId=` | Stats: totales, autorizadas, pendientes |

### Descargas

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `DescargarFacturaXml?negocioId=&facturaId=` | Descarga XML (descomprimido) |
| GET | `DescargarFacturaRide?negocioId=&facturaId=` | Descarga RIDE PDF |

### Acciones

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `EnviarFacturaEmail?negocioId=&facturaId=` | Reenvía XML+PDF por email |
| POST | `ReintentarFactura?negocioId=&facturaId=` | Reintenta envío al SRI |
| POST | `AnularFactura?negocioId=&facturaId=` | Marca como anulada |

---

## DTOs

### FacturaRequest

```csharp
public class FacturaRequest
{
    public Guid NegocioId { get; set; }
    public Guid? ReciboId { get; set; }
    public string CompradorIdentificacion { get; set; }
    public string CompradorTipoIdentificacion { get; set; } // "04","05","06","07"
    public string CompradorRazonSocial { get; set; }
    public string CompradorEmail { get; set; }
    public string CompradorDireccion { get; set; }
    public string FormaPagoSri { get; set; }                // "01","16","19","20"
    public List<FacturaItemRequest> Items { get; set; }
}
```

### FacturaItemRequest

```csharp
public class FacturaItemRequest
{
    public string CodigoPrincipal { get; set; }
    public string Descripcion { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public bool AplicaIva { get; set; } = true;  // IVA 15%
}
```

---

## Cifrado de Contraseña del Certificado

**Algoritmo:** AES-256-GCM
**Clave maestra:** `FacturacionSettings.EncryptionKey` (Base64 de 32 bytes)

**Formato almacenado:** `Base64(nonce[12] + ciphertext[N] + tag[16])`

**Helper:** `CertificadoEncryptionHelper`
- `Encrypt(plainText, base64Key)` → string Base64
- `Decrypt(encryptedBase64, base64Key)` → string plainText

**Generar clave:**
```bash
openssl rand -base64 32
```

**Ubicación:** `appsettings.Production.json` → `FacturacionSettings.EncryptionKey`
- NUNCA en git (appsettings.Production.json está en .gitignore)
- En servidor: `/opt/myapp/appsettings.Production.json`

---

## Background Service — Reintentos

**Clase:** `FacturaReintentoService` (BackgroundService)
**Frecuencia:** Cada 10 minutos
**Registrado en:** `BackgroundServicesRegistration.cs` (7° servicio)

**Lógica:**
1. Busca facturas con `EstadoSri` = "Pendiente" o "Recibida"
2. Que tengan `IntentosEnvio` < `MaxReintentosEnvio` (default 3)
3. Llama `ReprocesarPendientesAsync` del servicio
4. Delay de 1 segundo entre cada reintento para no saturar SRI

---

## Configuración (appsettings.json)

```json
"FacturacionSettings": {
    "EncryptionKey": "GENERATE_WITH_openssl_rand_-base64_32",
    "MaxReintentosEnvio": 3,
    "TimeoutSriSegundos": 30,
    "Endpoints": {
        "RecepcionPruebas": "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl",
        "AutorizacionPruebas": "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl",
        "RecepcionProduccion": "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl",
        "AutorizacionProduccion": "https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl"
    }
}
```

**Program.cs:**
```csharp
builder.Services.Configure<FacturacionSettings>(
    builder.Configuration.GetSection("FacturacionSettings"));
builder.Services.AddScoped<IFacturacionElectronicaService, FacturacionElectronicaService>();
builder.Services.AddHttpClient("SRI", client => {
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

---

## UI — Secciones Agregadas

### Perfil del Negocio (4 dashboards)

Card "Facturación Electrónica SRI" después de "Suscripción":
- Toggle switch para activar/desactivar
- Campos condicionales (solo visibles si toggle ON):
  - RUC, Razón Social, Nombre Comercial, Dirección Matriz
  - Establecimiento (default "001"), Punto Emisión (default "001")
  - Ambiente: Pruebas / Producción
  - Régimen Contribuyente, Obligado Contabilidad, Contribuyente Especial, Agente Retención
- Sección certificado: upload .p12 + password + badge con vencimiento
- Botón "Guardar Configuración SRI"

### Tab Facturación (sidebar)

- Solo visible si `window._facturacionActiva = true`
- 4 stat cards: Total emitidas, Autorizadas, Pendientes, Monto total
- Filtros: período (desde/hasta), estado
- Tabla con: Número, Fecha, Comprador, RUC/CI, Total, Estado (badge), Acciones
- Acciones por fila: Ver, XML, RIDE, Email, Reintentar, Anular
- Botón "Nueva Factura" (sin recibo previo)

### Modal Facturar (#modalFacturar)

Modal compartido en `_DashboardLayout.cshtml`:
- Tipo identificación: Consumidor Final / Cédula / RUC / Pasaporte
- Campos: Identificación, Razón Social, Email, Dirección
- Forma de pago: Efectivo / Tarjeta Débito / Tarjeta Crédito / Transferencia
- Tabla de items (precargada desde recibo o manual)
- Totales: Subtotal, IVA 15%, TOTAL
- Botón "Emitir Factura" con spinner

### JavaScript (window functions)

```javascript
// Configuración
window._facGuardarConfig()    // Guarda config SRI
window._facToggle(activa)     // Activa/desactiva
window._facSubirCert()        // Sube certificado .p12

// Facturación
window._facAbrirModal(items, reciboId)  // Abre modal con items
window._facEmitir()           // Emite factura desde modal
window._facNuevaFactura()     // Nueva factura sin recibo

// Tab
window._facFiltrar()          // Filtra tabla por fecha/estado

// Acciones por fila
window._facDescargarXml(id)   // Descarga XML
window._facDescargarRide(id)  // Descarga RIDE PDF
window._facEnviarEmail(id)    // Reenvía por email
window._facReintentar(id)     // Reintenta envío SRI
window._facAnular(id)         // Anula factura
```

---

## IVA Ecuador

- **Tarifa vigente:** 15% (código SRI: 2, codigoPorcentaje: 4)
- Si cambia la tarifa IVA, actualizar en `FacturacionElectronicaService.cs`:
  - Método `GenerarXmlFactura` → `<tarifa>`, `<codigoPorcentaje>`
  - Método `EmitirFacturaAsync` → cálculo de MontoIva

---

## Tipos de Identificación SRI

| Código | Tipo | Largo |
|--------|------|-------|
| 04 | RUC | 13 dígitos |
| 05 | Cédula | 10 dígitos |
| 06 | Pasaporte | Variable |
| 07 | Consumidor Final | "9999999999999" |

---

## Formas de Pago SRI

| Código | Descripción |
|--------|-------------|
| 01 | Sin utilización del sistema financiero (Efectivo) |
| 16 | Tarjeta de débito |
| 19 | Tarjeta de crédito |
| 20 | Otros con utilización del sistema financiero (Transferencia) |

---

## Requisitos para Emitir Facturas

Para que un negocio pueda emitir facturas electrónicas necesita:

1. **Datos SRI completos:** RUC (13 dígitos), Razón Social, Dirección Matriz
2. **Certificado digital .p12** válido (no vencido) emitido por entidad autorizada
3. **Contraseña del certificado** guardada y cifrada
4. **Ambiente configurado:** Pruebas (1) para testing, Producción (2) para facturas reales
5. **EncryptionKey** configurada en `appsettings.Production.json`

---

## Pendientes / Mejoras Futuras

- [ ] **RIDE PDF completo:** Implementar generación de RIDE usando Select.HtmlToPdf (ya incluido en el proyecto). Actualmente el campo `RidePdf` queda null.
- [ ] **Notas de crédito:** Tipo documento "04", para anulaciones parciales o totales
- [ ] **Retenciones:** Tipo documento "07"
- [ ] **Guías de remisión:** Tipo documento "06"
- [ ] **Consulta masiva al SRI:** Para sincronizar estados de facturas antiguas
- [ ] **Reporte fiscal mensual:** Exportar todas las facturas del mes para declaración
- [ ] **QR en RIDE:** Ya se tiene QRCoder instalado, falta integrar en el PDF

---

## Deploy y Mantenimiento

### Primera vez en producción

```bash
# 1. Generar clave de cifrado
openssl rand -base64 32

# 2. SSH al servidor
ssh -i "KEY" ubuntu@54.203.178.179

# 3. Editar appsettings.Production.json
sudo nano /opt/myapp/appsettings.Production.json
# Agregar en FacturacionSettings.EncryptionKey el valor generado

# 4. Reiniciar app
sudo systemctl restart myapp
```

### Migración de base de datos

La migración `20260228140144_AddFacturacionElectronica` se aplica automáticamente al iniciar la app (auto-migration en Program.cs). Crea:
- 14 columnas nuevas en tabla `Negocios`
- Tabla `FacturasElectronicas` con 4 índices

### Certificados .p12

- Los negocios obtienen su certificado del SRI o de entidades autorizadas (Security Data, ANF, etc.)
- El certificado se almacena completo en Base64 en la BD (`CertificadoP12Base64`)
- La contraseña se cifra con AES-256-GCM antes de almacenar
- Verificar vencimiento periódicamente (endpoint `ValidarCertificado`)

---

## Commit de Referencia

- **Commit:** `fb74493`
- **Mensaje:** `feat: facturación electrónica SRI Ecuador`
- **Fecha:** 2026-02-28
- **Release de producción:** `20260228_192554`
