# Deploy MiNegocio a Amazon Lightsail — Guía Paso a Paso

## Costo: $24/mes (todo incluido)

---

## PASO 1: Crear cuenta AWS

1. Ve a https://aws.amazon.com
2. Click "Create an AWS Account"
3. Pon tu email, nombre, tarjeta de crédito
4. Verificación por teléfono
5. Selecciona el plan "Basic Support (Free)"
6. Ya tienes cuenta

---

## PASO 2: Crear instancia Lightsail

1. Ve a https://lightsail.aws.amazon.com
2. Click **"Create instance"**
3. Configuración:
   - Region: **Oregon (us-west-2)** o la más cercana a tus usuarios
   - Platform: **Linux/Unix**
   - Blueprint: **OS Only → Ubuntu 22.04 LTS**
   - Instance plan: **$24 USD/month** (2 vCPUs, 4 GB RAM, 80 GB SSD)
   - Instance name: `minegocio-prod`
4. Click **"Create instance"**
5. Espera 2 minutos a que arranque

---

## PASO 3: IP estática

1. En Lightsail, ve a **Networking** tab
2. Click **"Create static IP"**
3. Attach to instance: `minegocio-prod`
4. Name: `minegocio-ip`
5. Click **"Create"**
6. **ANOTA LA IP** (ej: `44.230.xxx.xxx`) — la necesitas para el dominio
54.203.178.179
---

## PASO 4: Abrir puertos

1. En tu instancia `minegocio-prod` → tab **Networking**
2. En **IPv4 Firewall**, verifica que existan:
   - SSH (22) ✓ (ya viene)
   - HTTP (80) ✓ (ya viene)
   - HTTPS (443) — **click "Add rule"**, selecciona HTTPS, click **"Create"**

---

## PASO 5: Apuntar dominio

En tu registrador de dominio (GoDaddy, Namecheap, etc.):

1. Ve a DNS settings de tu dominio
2. Agrega/edita estos registros:

| Tipo | Host | Valor | TTL |
|------|------|-------|-----|
| A | @ | `44.230.xxx.xxx` (tu IP estática) | 300 |
| A | www | `44.230.xxx.xxx` (tu IP estática) | 300 |

3. Espera 5-15 minutos a que propague

---

## PASO 6: Descargar SSH key

1. En Lightsail → **Account** (arriba derecha) → **SSH keys**
2. Click **"Download"** en la key de tu región
3. Se descarga un archivo `.pem` (ej: `LightsailDefaultKey-us-west-2.pem`)
4. **GUARDA ESTE ARCHIVO** — lo necesitas para GitHub

---

## PASO 7: Configurar GitHub Secrets

1. Ve a tu repo en GitHub: https://github.com/GustavoLarcoDev/migimnasio
2. **Settings** → **Secrets and variables** → **Actions**
3. Click **"New repository secret"** y agrega estos 2:

| Nombre del secret | Valor |
|---|---|
| `SSH_PRIVATE_KEY` | Abre el archivo `.pem` con un editor de texto, copia TODO el contenido (incluyendo `-----BEGIN RSA PRIVATE KEY-----` y `-----END RSA PRIVATE KEY-----`) |
| `SERVER_HOST` | Tu IP estática (ej: `44.230.xxx.xxx`) |

---

## PASO 8: Setup del servidor (UNA SOLA VEZ)

### 8.1 Conectarte al servidor

Abre tu terminal y ejecuta:

```bash
chmod 400 ~/Downloads/LightsailDefaultKey-us-west-2.pem

ssh -i ~/Downloads/LightsailDefaultKey-us-west-2.pem ubuntu@TU_IP_ESTATICA
```

(Reemplaza `TU_IP_ESTATICA` con tu IP real)

Si pregunta "Are you sure you want to continue connecting?" escribe `yes`

### 8.2 Subir el script de setup

Desde OTRA terminal (no la del servidor), en tu máquina local:

```bash
scp -i ~/Downloads/LightsailDefaultKey-us-west-2.pem \
  ~/Desktop/Gimnasio/deploy/setup-server.sh \
  ubuntu@TU_IP_ESTATICA:/tmp/
```

### 8.3 Ejecutar el setup

De vuelta en la terminal del servidor:

```bash
sudo bash /tmp/setup-server.sh TU_DOMINIO "TU_PASSWORD_SQL" 'TU_HASH_ADMIN'
```

**Ejemplo real:**
```bash
sudo bash /tmp/setup-server.sh mynegocio.com "M1P@ssw0rd_Segur0!" '$2a$11$ZQkD7m3PfL5s.1QGPyEZFO0ChceFoQ54kO82jqOoy3TyTftJW2aBe'
```

**IMPORTANTE sobre los parámetros:**
- `TU_DOMINIO` = tu dominio sin www (ej: `mynegocio.com`)
- `TU_PASSWORD_SQL` = una contraseña FUERTE para SQL Server (mínimo 8 chars, mayúscula, minúscula, número, símbolo)
- `TU_HASH_ADMIN` = el BCrypt hash del admin (el que ya tienes en appsettings.json, entre comillas simples para que el `$` no se interprete)

El script tarda ~5-10 minutos. Cuando termine verás "SETUP COMPLETO".

---

## PASO 9: Primer deploy

Desde tu máquina local, en la carpeta del proyecto:

```bash
git add .github/ deploy/
git commit -m "add deployment pipeline and server setup"
git push origin main
```

Esto dispara GitHub Actions automáticamente. Para ver el progreso:
1. Ve a https://github.com/GustavoLarcoDev/migimnasio/actions
2. Verás el workflow corriendo
3. Espera a que termine (~2-3 minutos)

### Verificar que funcionó

En tu navegador, ve a `http://TU_IP_ESTATICA` — deberías ver la landing page.

Si no funciona, SSH al servidor y revisa:
```bash
sudo journalctl -u myapp -n 50
```

---

## PASO 10: Activar HTTPS

SSH al servidor y ejecuta:

```bash
sudo certbot --nginx -d tudominio.com -d www.tudominio.com
```

Te pedirá:
1. Email → pon tu email real (para renovaciones)
2. Terms of service → `Y`
3. Share email → `N`

Listo. Tu sitio ahora tiene HTTPS con certificado gratis que se renueva solo.

---

## LISTO — Tu sistema está en producción

### Cómo hacer cambios futuros

```bash
# 1. Haces cambios en tu código local
# 2. Commit y push
git add -A
git commit -m "descripción del cambio"
git push origin main

# 3. GitHub Actions se encarga del resto (~2-3 min)
# 4. Tu sitio se actualiza automáticamente
```

### Si modificaste tablas de la BD

Las migraciones de EF Core se aplican automáticamente al arrancar la app.
Solo asegúrate de:

```bash
# 1. Crear la migración localmente
dotnet ef migrations add NombreDelCambio

# 2. Commit y push
git add Migrations/
git commit -m "migration: descripción del cambio"
git push origin main

# 3. GitHub Actions despliega y al arrancar la app ejecuta db.Database.Migrate()
```

---

## Comandos útiles (SSH al servidor)

```bash
# Ver logs en tiempo real
sudo journalctl -u myapp -f

# Reiniciar la app
sudo systemctl restart myapp

# Ver status de la app
sudo systemctl status myapp

# Ver status de SQL Server
sudo systemctl status mssql-server

# Backup manual de la BD
sudo /opt/myapp/backup-db.sh

# Ver backups guardados
ls -lh /opt/myapp/backups/

# Ver cuánto disco queda
df -h

# Ver uso de RAM
free -h

# Ver releases desplegados
ls -la /opt/myapp/releases/
```

---

## Estructura en el servidor

```
/opt/myapp/
├── current -> releases/20260215_143000/    ← symlink a versión activa
├── releases/
│   ├── 20260215_143000/                    ← última versión
│   ├── 20260214_120000/                    ← versión anterior
│   └── ...                                 ← máx 5 releases
├── backups/
│   ├── gimnasio_20260215_030000.bak        ← backup diario 3AM
│   └── ...                                 ← últimos 7 días
├── appsettings.Production.json             ← secretos (NUNCA en git)
├── deploy.sh                               ← script de deploy
└── backup-db.sh                            ← script de backup
```

---

## Credenciales de prueba

| Rol | Email | Password |
|-----|-------|----------|
| Admin | gustavo.larco@mynegocio.com | (tu password) |
| Negocio | juan@powerfit.com | Test1234! |
| Vendedor | carlos@mynegocio.com | Test1234! |

**IMPORTANTE:** Cambia las passwords de prueba en producción.
