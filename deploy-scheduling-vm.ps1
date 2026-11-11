# Deploy Scheduling Planning Prolog Server to Azure VM
param(
    [string]$ResourceGroup = "appsvc_windows_swedencentral",
    [string]$VMName = "scheduling-prolog-vm",
    [string]$Location = "swedencentral",
    [string]$AdminUsername = "azureuser"
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Scheduling Prolog - VM Deployment" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check Azure login
$account = az account show 2>$null
if (-not $account) {
    Write-Host "[ERROR] Not logged in to Azure. Run 'az login' first." -ForegroundColor Red
    exit 1
}

Write-Host "[INFO] Creating/Checking VM..." -ForegroundColor Yellow

# Check if VM exists
$vmExists = az vm show --name $VMName --resource-group $ResourceGroup 2>$null

if (-not $vmExists) {
    Write-Host "[INFO] Creating new Ubuntu VM..." -ForegroundColor Yellow
    Write-Host "   This will take a few minutes..." -ForegroundColor Gray
    
    # Create VM with Ubuntu 22.04
    az vm create `
        --resource-group $ResourceGroup `
        --name $VMName `
        --image Ubuntu2204 `
        --size Standard_B1s `
        --admin-username $AdminUsername `
        --generate-ssh-keys `
        --public-ip-address-allocation static `
        --public-ip-sku Standard
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Failed to create VM" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "[SUCCESS] VM created" -ForegroundColor Green
    
    # Open port 5003
    Write-Host "[INFO] Opening port 5003..." -ForegroundColor Yellow
    az vm open-port --resource-group $ResourceGroup --name $VMName --port 5003 --priority 1001
    
    Write-Host "[SUCCESS] Port 5003 opened" -ForegroundColor Green
} else {
    Write-Host "[INFO] VM already exists" -ForegroundColor Green
}

# Get VM public IP
Write-Host "[INFO] Getting VM public IP..." -ForegroundColor Yellow
$vmIP = az vm show -d --resource-group $ResourceGroup --name $VMName --query publicIps -o tsv

if (-not $vmIP) {
    Write-Host "[ERROR] Could not get VM IP" -ForegroundColor Red
    exit 1
}

Write-Host "[SUCCESS] VM IP: $vmIP" -ForegroundColor Green
Write-Host ""

# Create deployment script for VM
Write-Host "[INFO] Creating deployment script for VM..." -ForegroundColor Yellow

$setupScript = @'
#!/bin/bash
set -e

echo "========================================="
echo "Installing SWI-Prolog and dependencies"
echo "========================================="

# Update system
sudo apt-get update

# Install SWI-Prolog
sudo apt-add-repository -y ppa:swi-prolog/stable
sudo apt-get update
sudo apt-get install -y swi-prolog

# Create app directory
sudo mkdir -p /opt/scheduling-prolog
sudo chown azureuser:azureuser /opt/scheduling-prolog

echo "========================================="
echo "Installation complete!"
echo "========================================="
'@

$setupScript | Out-File -FilePath "vm-setup.sh" -Encoding UTF8

Write-Host "[SUCCESS] Setup script created" -ForegroundColor Green

# Upload and run setup script
Write-Host "[INFO] Uploading and running setup script on VM..." -ForegroundColor Yellow
Write-Host "   This may take a few minutes..." -ForegroundColor Gray

az vm run-command invoke `
    --resource-group $ResourceGroup `
    --name $VMName `
    --command-id RunShellScript `
    --scripts @vm-setup.sh

if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Failed to run setup script" -ForegroundColor Red
    exit 1
}

Write-Host "[SUCCESS] SWI-Prolog installed on VM" -ForegroundColor Green
Write-Host ""

# Upload Prolog files to VM
Write-Host "[INFO] Uploading Prolog files to VM..." -ForegroundColor Yellow

$prologFiles = Get-ChildItem "apps/scheduling-planning/backend/*.pl"
$yamlFile = "apps/scheduling-planning/backend/openapi.yaml"

foreach ($file in $prologFiles) {
    Write-Host "   Uploading: $($file.Name)" -ForegroundColor Gray
    scp -o StrictHostKeyChecking=no $file.FullName "${AdminUsername}@${vmIP}:/opt/scheduling-prolog/"
}

if (Test-Path $yamlFile) {
    Write-Host "   Uploading: openapi.yaml" -ForegroundColor Gray
    scp -o StrictHostKeyChecking=no $yamlFile "${AdminUsername}@${vmIP}:/opt/scheduling-prolog/"
}

Write-Host "[SUCCESS] Files uploaded" -ForegroundColor Green
Write-Host ""

# Create systemd service
Write-Host "[INFO] Creating systemd service..." -ForegroundColor Yellow

$serviceScript = @'
#!/bin/bash
sudo tee /etc/systemd/system/scheduling-prolog.service > /dev/null <<'EOF'
[Unit]
Description=Scheduling Planning Prolog Server
After=network.target

[Service]
Type=simple
User=azureuser
WorkingDirectory=/opt/scheduling-prolog
ExecStart=/usr/bin/swipl -s /opt/scheduling-prolog/scheduling_server.pl -g start_scheduling_server
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable scheduling-prolog
sudo systemctl start scheduling-prolog
echo "Service created and started"
'@

$serviceScript | Out-File -FilePath "vm-service.sh" -Encoding UTF8

az vm run-command invoke `
    --resource-group $ResourceGroup `
    --name $VMName `
    --command-id RunShellScript `
    --scripts @vm-service.sh

Write-Host "[SUCCESS] Service created and started" -ForegroundColor Green
Write-Host ""

# Cleanup temp files
Remove-Item vm-setup.sh -ErrorAction SilentlyContinue
Remove-Item vm-service.sh -ErrorAction SilentlyContinue

Write-Host "=========================================" -ForegroundColor Green
Write-Host "DEPLOYMENT COMPLETE!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""
Write-Host "VM Details:" -ForegroundColor White
Write-Host "   Name: $VMName" -ForegroundColor Gray
Write-Host "   Public IP: $vmIP" -ForegroundColor Gray
Write-Host "   SSH: ssh ${AdminUsername}@${vmIP}" -ForegroundColor Gray
Write-Host ""
Write-Host "API Endpoint:" -ForegroundColor White
Write-Host "   http://${vmIP}:5003/api/scheduling/health" -ForegroundColor Gray
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Wait 30 seconds for service to start" -ForegroundColor White
Write-Host "2. Test endpoint: curl http://${vmIP}:5003/api/scheduling/health" -ForegroundColor White
Write-Host "3. Update environment.azure.ts with new URL" -ForegroundColor White
Write-Host ""
Write-Host "To check service status:" -ForegroundColor Yellow
Write-Host "   ssh ${AdminUsername}@${vmIP} sudo systemctl status scheduling-prolog" -ForegroundColor Gray
Write-Host ""
Write-Host "To view logs:" -ForegroundColor Yellow
Write-Host "   ssh ${AdminUsername}@${vmIP} sudo journalctl -u scheduling-prolog -f" -ForegroundColor Gray
Write-Host ""
