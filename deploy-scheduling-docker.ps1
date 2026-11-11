# Deploy Scheduling-Planning Service to Azure using Docker
# This is the BEST solution for deploying Prolog to Azure

param(
    [string]$ResourceGroup = "appsvc_windows_swedencentral",
    [string]$AppName = "scheduling-planning-3db01",
    [string]$Location = "swedencentral",
    [string]$RegistryName = "portlogisticsacr3db01",  # Must be globally unique, lowercase alphanumeric only
    [string]$ImageName = "scheduling-planning",
    [string]$ImageTag = "latest",
    [switch]$SkipBuild,
    [switch]$SkipPush,
    [switch]$LocalTest
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Scheduling-Planning - Docker Deployment" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is running
Write-Host "[INFO] Checking Docker..." -ForegroundColor Yellow
try {
    docker version | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Docker is not running. Please start Docker Desktop." -ForegroundColor Red
        exit 1
    }
    Write-Host "[SUCCESS] Docker is running" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "[ERROR] Docker is not installed or not running." -ForegroundColor Red
    Write-Host "Install Docker Desktop from: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
    exit 1
}

# Navigate to backend directory
$backendPath = "apps/scheduling-planning/backend"
if (-not (Test-Path $backendPath)) {
    Write-Host "[ERROR] Backend directory not found: $backendPath" -ForegroundColor Red
    exit 1
}

# ============================================
# BUILD DOCKER IMAGE
# ============================================
if (-not $SkipBuild) {
    Write-Host "[INFO] Building Docker image..." -ForegroundColor Yellow
    Write-Host "   Image: $ImageName" -ForegroundColor Gray
    Write-Host "   Tag: $ImageTag" -ForegroundColor Gray
    Write-Host ""
    
    Push-Location $backendPath
    
    docker build -t ${ImageName}:${ImageTag} .
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Docker build failed." -ForegroundColor Red
        Pop-Location
        exit 1
    }
    
    Pop-Location
    Write-Host "[SUCCESS] Docker image built successfully" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "[INFO] Skipping Docker build (--SkipBuild flag)" -ForegroundColor Yellow
    Write-Host ""
}

# ============================================
# LOCAL TEST (Optional)
# ============================================
if ($LocalTest) {
    Write-Host "[INFO] Running local test..." -ForegroundColor Yellow
    Write-Host ""
    
    # Stop any existing container
    docker stop scheduling-test 2>$null | Out-Null
    docker rm scheduling-test 2>$null | Out-Null
    
    # Run container locally
    Write-Host "Starting container on http://localhost:5003..." -ForegroundColor Cyan
    docker run -d -p 5003:8080 -e PORT=8080 --name scheduling-test ${ImageName}:${ImageTag}
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Failed to start container" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "[SUCCESS] Container started" -ForegroundColor Green
    Write-Host ""
    Write-Host "Testing endpoints..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    
    try {
        $health = Invoke-RestMethod -Uri "http://localhost:5003/api/scheduling/health" -Method Get -TimeoutSec 10
        Write-Host "[SUCCESS] Health check passed: $health" -ForegroundColor Green
    } catch {
        Write-Host "[WARNING] Health check failed (server may still be starting)" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "[INFO] Container logs:" -ForegroundColor Yellow
    docker logs scheduling-test
    Write-Host ""
    
    Write-Host "[INFO] Container is running. Test it at http://localhost:5003" -ForegroundColor Cyan
    Write-Host "[INFO] Press Enter to stop container and continue deployment, or Ctrl+C to exit..." -ForegroundColor Yellow
    Read-Host
    
    docker stop scheduling-test | Out-Null
    docker rm scheduling-test | Out-Null
    Write-Host "[INFO] Test container stopped" -ForegroundColor Gray
    Write-Host ""
}

# ============================================
# AZURE LOGIN CHECK
# ============================================
Write-Host "[INFO] Checking Azure login..." -ForegroundColor Yellow
$account = az account show 2>$null
if (-not $account) {
    Write-Host "[ERROR] Not logged in to Azure. Running 'az login'..." -ForegroundColor Red
    az login
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Azure login failed. Exiting." -ForegroundColor Red
        exit 1
    }
}
$accountInfo = az account show --query "{Subscription:name, ID:id}" --output table
Write-Host "[SUCCESS] Logged in to Azure" -ForegroundColor Green
Write-Host $accountInfo
Write-Host ""

# ============================================
# CREATE AZURE CONTAINER REGISTRY
# ============================================
Write-Host "[INFO] Setting up Azure Container Registry..." -ForegroundColor Yellow

$acrExists = az acr show --name $RegistryName --resource-group $ResourceGroup 2>$null

if (-not $acrExists) {
    Write-Host "[INFO] Creating Azure Container Registry..." -ForegroundColor Yellow
    az acr create `
        --resource-group $ResourceGroup `
        --name $RegistryName `
        --sku Basic `
        --location $Location `
        --admin-enabled true
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Failed to create ACR. Exiting." -ForegroundColor Red
        exit 1
    }
    Write-Host "[SUCCESS] ACR created" -ForegroundColor Green
} else {
    Write-Host "[SUCCESS] ACR already exists" -ForegroundColor Green
}
Write-Host ""

# ============================================
# PUSH IMAGE TO ACR
# ============================================
if (-not $SkipPush) {
    Write-Host "[INFO] Pushing image to Azure Container Registry..." -ForegroundColor Yellow
    
    # Get ACR login server
    $loginServer = az acr show --name $RegistryName --resource-group $ResourceGroup --query loginServer --output tsv
    $fullImageName = "${loginServer}/${ImageName}:${ImageTag}"
    
    Write-Host "   Login Server: $loginServer" -ForegroundColor Gray
    Write-Host "   Full Image: $fullImageName" -ForegroundColor Gray
    Write-Host ""
    
    # Login to ACR
    Write-Host "Logging into ACR..." -ForegroundColor Yellow
    az acr login --name $RegistryName
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] ACR login failed" -ForegroundColor Red
        exit 1
    }
    
    # Tag image for ACR
    Write-Host "Tagging image..." -ForegroundColor Yellow
    docker tag ${ImageName}:${ImageTag} $fullImageName
    
    # Push to ACR
    Write-Host "Pushing to ACR (this may take a few minutes)..." -ForegroundColor Yellow
    docker push $fullImageName
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Failed to push image to ACR" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "[SUCCESS] Image pushed to ACR" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "[INFO] Skipping image push (--SkipPush flag)" -ForegroundColor Yellow
    $loginServer = az acr show --name $RegistryName --resource-group $ResourceGroup --query loginServer --output tsv
    $fullImageName = "${loginServer}/${ImageName}:${ImageTag}"
    Write-Host ""
}

# ============================================
# CREATE/UPDATE WEB APP FOR CONTAINERS
# ============================================
Write-Host "[INFO] Deploying Web App for Containers..." -ForegroundColor Yellow

$appExists = az webapp show --name $AppName --resource-group $ResourceGroup 2>$null

# Get ACR credentials
$acrUsername = az acr credential show --name $RegistryName --resource-group $ResourceGroup --query username --output tsv
$acrPassword = az acr credential show --name $RegistryName --resource-group $ResourceGroup --query "passwords[0].value" --output tsv

if (-not $appExists) {
    Write-Host "[INFO] Creating new Web App..." -ForegroundColor Yellow
    
    # Check if App Service Plan exists (Linux required for containers)
    $planName = "portlogistics-linux-plan"
    $planExists = az appservice plan show --name $planName --resource-group $ResourceGroup 2>$null
    
    if (-not $planExists) {
        Write-Host "[INFO] Creating Linux App Service Plan..." -ForegroundColor Yellow
        az appservice plan create `
            --name $planName `
            --resource-group $ResourceGroup `
            --location $Location `
            --is-linux `
            --sku B1
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[ERROR] Failed to create App Service Plan" -ForegroundColor Red
            exit 1
        }
    }
    
    # Create Web App with Container
    Write-Host "[INFO] Creating Web App with container image..." -ForegroundColor Yellow
    az webapp create `
        --resource-group $ResourceGroup `
        --plan $planName `
        --name $AppName `
        --deployment-container-image-name $fullImageName `
        --docker-registry-server-user $acrUsername `
        --docker-registry-server-password $acrPassword
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Failed to create Web App" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "[SUCCESS] Web App created" -ForegroundColor Green
} else {
    Write-Host "[INFO] Updating existing Web App..." -ForegroundColor Yellow
    
    # Update container image
    az webapp config container set `
        --name $AppName `
        --resource-group $ResourceGroup `
        --docker-custom-image-name $fullImageName `
        --docker-registry-server-url "https://${loginServer}" `
        --docker-registry-server-user $acrUsername `
        --docker-registry-server-password $acrPassword
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Failed to update Web App" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "[SUCCESS] Web App updated" -ForegroundColor Green
}
Write-Host ""

# ============================================
# CONFIGURE WEB APP SETTINGS
# ============================================
Write-Host "[INFO] Configuring Web App settings..." -ForegroundColor Yellow

# Set environment variables
az webapp config appsettings set `
    --name $AppName `
    --resource-group $ResourceGroup `
    --settings `
        PORT=8080 `
        WEBSITES_PORT=8080 `
        WEBSITES_CONTAINER_START_TIME_LIMIT=1800 | Out-Null

# Enable container logging
az webapp log config `
    --name $AppName `
    --resource-group $ResourceGroup `
    --docker-container-logging filesystem | Out-Null

Write-Host "[SUCCESS] Web App configured" -ForegroundColor Green
Write-Host ""

# ============================================
# RESTART WEB APP
# ============================================
Write-Host "[INFO] Restarting Web App..." -ForegroundColor Yellow
az webapp restart --name $AppName --resource-group $ResourceGroup | Out-Null
Write-Host "[SUCCESS] Web App restarted" -ForegroundColor Green
Write-Host ""

# ============================================
# DEPLOYMENT COMPLETE
# ============================================
$appUrl = "https://${AppName}.azurewebsites.net"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "DEPLOYMENT COMPLETE!" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Scheduling Service URL:" -ForegroundColor Green
Write-Host "   Base: $appUrl" -ForegroundColor White
Write-Host "   Health: ${appUrl}/api/scheduling/health" -ForegroundColor White
Write-Host "   Generate: ${appUrl}/api/scheduling/generate" -ForegroundColor White
Write-Host ""
Write-Host "Container Image:" -ForegroundColor Green
Write-Host "   Registry: $loginServer" -ForegroundColor White
Write-Host "   Image: $fullImageName" -ForegroundColor White
Write-Host ""
Write-Host "[IMPORTANT] Wait 2-3 minutes for container to start" -ForegroundColor Yellow
Write-Host ""
Write-Host "Useful Commands:" -ForegroundColor Cyan
Write-Host "   View logs: az webapp log tail --name $AppName --resource-group $ResourceGroup" -ForegroundColor Gray
Write-Host "   Test health: curl ${appUrl}/api/scheduling/health" -ForegroundColor Gray
Write-Host "   Restart app: az webapp restart --name $AppName --resource-group $ResourceGroup" -ForegroundColor Gray
Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Next: Test Operation Plans in Azure!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
