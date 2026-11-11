# Port Logistics - VM Setup Helper Script
# Run this script on your DEI VM to set up the environment

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Port Logistics - VM Setup Helper" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# This script provides commands to copy to your VM
# It's a guide, not meant to be run directly on Windows

$commands = @"
# ============================================
# STEP 1: Install .NET Runtime
# ============================================
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
sudo ./dotnet-install.sh --channel 9.0 --runtime aspnetcore --install-dir /usr/share/dotnet
export PATH=`$PATH:/usr/share/dotnet
echo 'export PATH=`$PATH:/usr/share/dotnet' >> ~/.bashrc
dotnet --version

# ============================================
# STEP 2: Create Systemd Service
# ============================================
sudo tee /etc/systemd/system/portlogistics.service > /dev/null <<EOF
[Unit]
Description=Port Logistics Web API
After=network.target

[Service]
Type=notify
WorkingDirectory=/var/www/portlogistics/current
ExecStart=/usr/share/dotnet/dotnet /var/www/portlogistics/current/WebApi.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=portlogistics
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable portlogistics.service

# ============================================
# STEP 3: Create Directory Structure
# ============================================
sudo mkdir -p /var/www/portlogistics/current
sudo mkdir -p /var/backups/portlogistics
sudo mkdir -p /var/log/portlogistics
sudo chown -R www-data:www-data /var/www/portlogistics
sudo chown -R www-data:www-data /var/log/portlogistics

# ============================================
# STEP 4: Generate SSH Key for GitHub Actions
# ============================================
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/github_deploy -N ""
cat ~/.ssh/github_deploy.pub >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys

echo ""
echo "=========================================="
echo "IMPORTANT: Copy this private key to GitHub Secrets as DEI_VM_SSH_KEY:"
echo "=========================================="
cat ~/.ssh/github_deploy
echo "=========================================="

# ============================================
# STEP 5: Create Production Configuration Template
# ============================================
sudo tee /var/www/portlogistics/current/appsettings.Production.json > /dev/null <<EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://0.0.0.0:5000",
  "ConnectionStrings": {
    "AbsanteeDatabase": "Server=localhost;Port=3306;Database=AbsanteeDB;User=root;Password=CHANGE_ME;"
  }
}
EOF

sudo chown www-data:www-data /var/www/portlogistics/current/appsettings.Production.json
sudo chmod 600 /var/www/portlogistics/current/appsettings.Production.json

# ============================================
# STEP 6: Configure Firewall (if UFW is enabled)
# ============================================
sudo ufw allow 5000/tcp
sudo ufw status

# ============================================
# STEP 7: Install MySQL Client (optional, for testing)
# ============================================
sudo apt update
sudo apt install -y mysql-client

echo ""
echo "=========================================="
echo "Setup Complete!"
echo "=========================================="
echo ""
echo "Next steps:"
echo "1. Edit /var/www/portlogistics/current/appsettings.Production.json with your database credentials"
echo "2. Add GitHub Secrets in your repository:"
echo "   - DEI_VM_HOST: your VM hostname"
echo "   - DEI_VM_USERNAME: your username"
echo "   - DEI_VM_SSH_KEY: the private key shown above"
echo "   - DEI_VM_PORT: 22"
echo "   - DEI_VM_APP_URL: http://your-vm-hostname:5000"
echo "3. Push to main branch or trigger manual deployment"
echo ""
"@

Write-Host "Copy and run these commands on your DEI VM:" -ForegroundColor Yellow
Write-Host ""
Write-Host $commands -ForegroundColor White
Write-Host ""

# Save to file for easy copy
$commands | Out-File -FilePath "vm-setup-commands.sh" -Encoding UTF8
Write-Host "Commands saved to: vm-setup-commands.sh" -ForegroundColor Green
Write-Host "You can copy this file to your VM and run it" -ForegroundColor Green
Write-Host ""

# Display GitHub Secrets info
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "GitHub Secrets to Configure:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Go to: GitHub Repository → Settings → Secrets and variables → Actions" -ForegroundColor Yellow
Write-Host ""
Write-Host "Add these secrets:" -ForegroundColor White
Write-Host "  • DEI_VM_HOST         → Your VM hostname (e.g., vs123.ua.pt)" -ForegroundColor Gray
Write-Host "  • DEI_VM_USERNAME     → Your VM username (e.g., student123)" -ForegroundColor Gray
Write-Host "  • DEI_VM_SSH_KEY      → Private key from VM (from ssh-keygen)" -ForegroundColor Gray
Write-Host "  • DEI_VM_PORT         → SSH port (usually 22)" -ForegroundColor Gray
Write-Host "  • DEI_VM_APP_URL      → App URL (e.g., http://vs123.ua.pt:5000)" -ForegroundColor Gray
Write-Host ""
