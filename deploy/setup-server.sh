#!/bin/bash
# ═══════════════════════════════════════════════════════════
# setup-server.sh — Configuración inicial del servidor Lightsail
# Ejecutar UNA SOLA VEZ en el servidor como root
# Usage: sudo bash setup-server.sh TU_DOMINIO DB_PASSWORD ADMIN_HASH
# ═══════════════════════════════════════════════════════════

set -euo pipefail

DOMAIN="${1:?Uso: sudo bash setup-server.sh midominio.com DB_PASSWORD ADMIN_HASH}"
DB_PASSWORD="${2:?Falta DB_PASSWORD}"
ADMIN_HASH="${3:?Falta ADMIN_HASH}"

echo "══════════════════════════════════════════"
echo " Configurando servidor para MiNegocio"
echo " Dominio: $DOMAIN"
echo "══════════════════════════════════════════"

# ─── 1. Actualizar sistema ───
echo "[1/9] Actualizando sistema..."
apt-get update && apt-get upgrade -y

# ─── 2. Agregar repositorio de Microsoft + Instalar .NET 9 ───
echo "[2/9] Instalando .NET 9..."
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg
curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -o /tmp/packages-microsoft-prod.deb
dpkg -i /tmp/packages-microsoft-prod.deb
rm /tmp/packages-microsoft-prod.deb
apt-get update
apt-get install -y aspnetcore-runtime-9.0

# ─── 3. Instalar SQL Server 2022 Express ───
echo "[3/9] Instalando SQL Server 2022 Express..."
curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list | tee /etc/apt/sources.list.d/mssql-server-2022.list
apt-get update
apt-get install -y mssql-server

# Configurar SQL Server Express con la contraseña proporcionada
ACCEPT_EULA=Y MSSQL_SA_PASSWORD="$DB_PASSWORD" MSSQL_PID=Express \
  /opt/mssql/bin/mssql-conf setup

systemctl enable mssql-server
systemctl start mssql-server

# Limitar RAM de SQL Server a 1GB (dejar 3GB para OS + .NET + Nginx)
/opt/mssql/bin/mssql-conf set memory.memorylimitmb 1024
systemctl restart mssql-server

# Instalar sqlcmd
curl https://packages.microsoft.com/keys/microsoft.asc | tee /etc/apt/trusted.gpg.d/microsoft.asc
curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/prod.list | tee /etc/apt/sources.list.d/mssql-release.list
apt-get update
ACCEPT_EULA=Y apt-get install -y mssql-tools18 unixodbc-dev
echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> /etc/profile.d/mssql.sh

# Crear la base de datos
sleep 5
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$DB_PASSWORD" -C -Q "CREATE DATABASE GimnasioDb;"
echo "  Base de datos GimnasioDb creada"

# ─── 4. Instalar Nginx ───
echo "[4/9] Instalando Nginx..."
apt-get install -y nginx
systemctl enable nginx

# ─── 5. Crear estructura de la app ───
echo "[5/9] Creando estructura de directorios..."
mkdir -p /opt/myapp/releases
mkdir -p /opt/myapp/backups

# Crear appsettings.Production.json (SECRETOS - solo en el servidor)
cat > /opt/myapp/appsettings.Production.json << APPSETTINGS
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "$DOMAIN;www.$DOMAIN",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=GimnasioDb;User Id=sa;Password=$DB_PASSWORD;TrustServerCertificate=True;Encrypt=False"
  },
  "AdminSettings": {
    "Email": "gustavo.larco@mynegocio.com",
    "PasswordHash": "$ADMIN_HASH"
  },
  "WhatsAppSettings": {
    "PhoneNumberId": "",
    "AccessToken": "",
    "ApiVersion": "v21.0",
    "Enabled": false
  }
}
APPSETTINGS

chown www-data:www-data /opt/myapp/appsettings.Production.json
chmod 600 /opt/myapp/appsettings.Production.json
echo "  appsettings.Production.json creado"

# ─── 6. Crear script de deploy ───
echo "[6/9] Creando script de deploy..."
cat > /opt/myapp/deploy.sh << 'DEPLOY'
#!/bin/bash
set -euo pipefail

RELEASE_DIR="/opt/myapp/releases/$(date +%Y%m%d_%H%M%S)"
CURRENT_LINK="/opt/myapp/current"
DEPLOY_SOURCE="/tmp/myapp-deploy"

echo "Deploying to $RELEASE_DIR..."

# Copiar archivos publicados
cp -r "$DEPLOY_SOURCE" "$RELEASE_DIR"

# Verificar que el DLL existe
if [ ! -f "$RELEASE_DIR/Gimnasio.dll" ]; then
    echo "ERROR: Gimnasio.dll no encontrado en el release!" >&2
    rm -rf "$RELEASE_DIR"
    exit 1
fi

# Copiar appsettings.Production.json (secretos del servidor)
cp /opt/myapp/appsettings.Production.json "$RELEASE_DIR/appsettings.Production.json"

# Permisos para www-data
chown -R www-data:www-data "$RELEASE_DIR"

# Swap symlink (atómico)
ln -sfn "$RELEASE_DIR" "${CURRENT_LINK}.tmp"
mv -Tf "${CURRENT_LINK}.tmp" "$CURRENT_LINK"

# Restart app
systemctl restart myapp

# Esperar 3 segundos y verificar que arrancó
sleep 3
if systemctl is-active --quiet myapp; then
    echo "App arrancó correctamente"
else
    echo "ERROR: App no arrancó. Revisa: sudo journalctl -u myapp -n 50" >&2
    exit 1
fi

# Limpiar releases viejos (mantener últimos 5)
cd /opt/myapp/releases
ls -dt */ | tail -n +6 | xargs -r rm -rf

# Limpiar temp
rm -rf "$DEPLOY_SOURCE"

echo "Deploy completado: $RELEASE_DIR"
DEPLOY

chmod +x /opt/myapp/deploy.sh

# ─── 7. Crear servicio systemd ───
echo "[7/9] Configurando servicio systemd..."
cat > /etc/systemd/system/myapp.service << 'SERVICE'
[Unit]
Description=MiNegocio ASP.NET Core App
After=network.target mssql-server.service

[Service]
WorkingDirectory=/opt/myapp/current
ExecStart=/usr/bin/dotnet /opt/myapp/current/Gimnasio.dll
Restart=always
RestartSec=5
SyslogIdentifier=myapp
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
SERVICE

systemctl daemon-reload
systemctl enable myapp

# ─── 8. Configurar Nginx ───
echo "[8/9] Configurando Nginx..."
cat > /etc/nginx/sites-available/myapp << NGINX
server {
    listen 80;
    server_name $DOMAIN www.$DOMAIN;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
        proxy_buffering off;
        client_max_body_size 10M;
    }
}
NGINX

ln -sf /etc/nginx/sites-available/myapp /etc/nginx/sites-enabled/myapp
rm -f /etc/nginx/sites-enabled/default
nginx -t && systemctl restart nginx

# ─── 9. SSL + Backup ───
echo "[9/9] Instalando Certbot + configurando backups..."
apt-get install -y certbot python3-certbot-nginx

# Backup automático diario
cat > /opt/myapp/backup-db.sh << 'BACKUP'
#!/bin/bash
BACKUP_DIR="/opt/myapp/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$(grep -oP '(?<=Password=)[^;]+' /opt/myapp/appsettings.Production.json | head -1)" -C \
  -Q "BACKUP DATABASE GimnasioDb TO DISK = '$BACKUP_DIR/gimnasio_$TIMESTAMP.bak' WITH COMPRESSION;"
# Mantener solo últimos 7 backups
find "$BACKUP_DIR" -name "*.bak" -mtime +7 -delete
BACKUP
chmod +x /opt/myapp/backup-db.sh

# Cron: backup diario a las 3:00 AM
(crontab -l 2>/dev/null; echo "0 3 * * * /opt/myapp/backup-db.sh >> /var/log/myapp-backup.log 2>&1") | crontab -

echo ""
echo "══════════════════════════════════════════"
echo " SETUP COMPLETO"
echo "══════════════════════════════════════════"
echo ""
echo " Siguiente paso: hacer el primer deploy"
echo " desde GitHub (git push main) y luego:"
echo ""
echo "   sudo certbot --nginx -d $DOMAIN -d www.$DOMAIN"
echo ""
echo " para activar HTTPS gratis."
echo ""
echo "══════════════════════════════════════════"
