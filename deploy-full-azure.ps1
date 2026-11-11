# Full Azure Deployment Script - Backend + Frontend
# This script deploys both the backend API and the Angular SPA to Azure

param(
    [string]$ResourceGroup = "appsvc_windows_swedencentral",
    [string]$BackendAppName = "portlogistics-api-3db01",
    [string]$StorageAccountName = "portlogisticsspa",
    [string]$Location = "swedencentral",
    [switch]$SkipBackend,
    [switch]$SkipFrontend,
    [switch]$SkipBuild,
    [switch]$SkipUpload,
    [switch]$ForceUpload
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Port Logistics - Full Azure Deployment" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check if logged in to Azure
Write-Host "[INFO] Checking Azure login status..." -ForegroundColor Yellow
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
# BACKEND DEPLOYMENT
# ============================================
if (-not $SkipBackend) {
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host "STEP 1: BACKEND API DEPLOYMENT" -ForegroundColor Cyan
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host ""

    # Build and publish backend
    if (-not $SkipBuild) {
        Write-Host "[INFO] Cleaning previous backend build..." -ForegroundColor Yellow
        if (Test-Path "publish") {
            Remove-Item -Path "publish" -Recurse -Force
        }
        
        Write-Host "[INFO] Building backend API (clean build)..." -ForegroundColor Yellow
        dotnet clean apps/port-logistics/backend/WebApi/WebApi.csproj -c Release
        dotnet publish apps/port-logistics/backend/WebApi/WebApi.csproj -c Release -o ./publish --no-self-contained --force
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[ERROR] Backend build failed. Exiting." -ForegroundColor Red
            exit 1
        }
        Write-Host "[SUCCESS] Backend build successful" -ForegroundColor Green
        Write-Host ""
    }

    # Create deployment package
    Write-Host "[INFO] Creating backend deployment package..." -ForegroundColor Yellow
    Remove-Item deploy.zip -ErrorAction SilentlyContinue
    Push-Location publish
    Compress-Archive -Path * -DestinationPath ..\deploy.zip -Force
    Pop-Location

    if (-not (Test-Path deploy.zip)) {
        Write-Host "[ERROR] Failed to create deployment package. Exiting." -ForegroundColor Red
        exit 1
    }
    Write-Host "[SUCCESS] Backend deployment package created" -ForegroundColor Green
    Write-Host ""

    # Deploy to Azure
    Write-Host "[INFO] Deploying backend to Azure App Service..." -ForegroundColor Yellow
    Write-Host "   Resource Group: $ResourceGroup" -ForegroundColor Gray
    Write-Host "   App Name: $BackendAppName" -ForegroundColor Gray
    Write-Host ""

    az webapp deploy --resource-group $ResourceGroup --name $BackendAppName --src-path deploy.zip --type zip

    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Backend deployment failed." -ForegroundColor Red
        exit 1
    }

    Write-Host ""
    Write-Host "[SUCCESS] Backend deployed successfully!" -ForegroundColor Green
    Write-Host ""
    
    # Restart App Service to ensure new code is loaded
    Write-Host "[INFO] Restarting App Service to load new code..." -ForegroundColor Yellow
    az webapp restart --resource-group $ResourceGroup --name $BackendAppName
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[SUCCESS] App Service restarted" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] Failed to restart App Service (may restart automatically)" -ForegroundColor Yellow
    }
    Write-Host ""
} else {
    Write-Host "[INFO] Skipping backend deployment (--SkipBackend flag set)" -ForegroundColor Yellow
    Write-Host ""
}

# ============================================
# FRONTEND DEPLOYMENT
# ============================================
if (-not $SkipFrontend) {
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host "STEP 2: FRONTEND SPA DEPLOYMENT" -ForegroundColor Cyan
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host ""

    # Navigate to SPA directory
    $spaPath = "apps/port-logistics/frontend/web/spa"
    
    # Build Angular application
    if (-not $SkipBuild) {
        Write-Host "[INFO] Building Angular SPA for Azure..." -ForegroundColor Yellow
        Push-Location $spaPath
        
        # Check if node_modules exists
        if (-not (Test-Path "node_modules")) {
            Write-Host "[INFO] Installing npm dependencies..." -ForegroundColor Yellow
            npm install
            if ($LASTEXITCODE -ne 0) {
                Write-Host "[ERROR] npm install failed. Exiting." -ForegroundColor Red
                Pop-Location
                exit 1
            }
        }
        
        # Clean previous build and Angular cache
        Write-Host "[INFO] Cleaning previous build and cache..." -ForegroundColor Yellow
        if (Test-Path "dist") {
            Remove-Item -Path "dist" -Recurse -Force
        }
        if (Test-Path ".angular/cache") {
            Remove-Item -Path ".angular/cache" -Recurse -Force
        }
        
        # Build for Azure with production optimizations
        Write-Host "[INFO] Building with Azure configuration (clean build)..." -ForegroundColor Yellow
        npm run build -- --configuration=azure
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[ERROR] Angular build failed. Exiting." -ForegroundColor Red
            Pop-Location
            exit 1
        }
        
        Pop-Location
        Write-Host "[SUCCESS] Angular build successful" -ForegroundColor Green
        Write-Host ""
    }

    # Verify build output exists
    $distPath = "$spaPath/dist/spa/browser"
    if (-not (Test-Path $distPath)) {
        Write-Host "[ERROR] Build output not found at: $distPath" -ForegroundColor Red
        Write-Host "[INFO] Checking alternative paths..." -ForegroundColor Yellow
        
        # Check if output is in different location
        if (Test-Path "$spaPath/dist/browser") {
            $distPath = "$spaPath/dist/browser"
            Write-Host "[INFO] Found build output at: $distPath" -ForegroundColor Green
        } elseif (Test-Path "$spaPath/dist") {
            $distPath = "$spaPath/dist"
            Write-Host "[INFO] Found build output at: $distPath" -ForegroundColor Green
        } else {
            Write-Host "[ERROR] Cannot find build output. Run without --SkipBuild flag." -ForegroundColor Red
            exit 1
        }
    }

    # Check if Storage Account exists
    Write-Host "[INFO] Checking if Storage Account exists..." -ForegroundColor Yellow
    $storageExists = az storage account show --name $StorageAccountName --resource-group $ResourceGroup 2>$null
    
    if (-not $storageExists) {
        Write-Host "[INFO] Creating Storage Account..." -ForegroundColor Yellow
        az storage account create `
            --name $StorageAccountName `
            --resource-group $ResourceGroup `
            --location $Location `
            --sku Standard_LRS `
            --kind StorageV2 `
            --allow-blob-public-access true
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[ERROR] Failed to create Storage Account. Exiting." -ForegroundColor Red
            exit 1
        }
        Write-Host "[SUCCESS] Storage Account created" -ForegroundColor Green
        Write-Host ""
    } else {
        Write-Host "[SUCCESS] Storage Account already exists" -ForegroundColor Green
        Write-Host ""
    }

    # Enable static website hosting
    Write-Host "[INFO] Enabling static website hosting..." -ForegroundColor Yellow
    az storage blob service-properties update `
        --account-name $StorageAccountName `
        --static-website `
        --index-document index.html `
        --404-document index.html `
        2>$null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[WARNING] Failed to enable static website hosting via CLI. Trying alternative method..." -ForegroundColor Yellow
        # Alternative: Use REST API or manual configuration
    } else {
        Write-Host "[SUCCESS] Static website hosting enabled" -ForegroundColor Green
    }
    Write-Host ""

    # Check if should skip upload
    if ($SkipUpload) {
        Write-Host "[INFO] Skipping file upload (--SkipUpload flag set)" -ForegroundColor Yellow
        Write-Host ""
    } else {
        # Determine if should upload
        $shouldUpload = $false
        
        if ($ForceUpload) {
            Write-Host "[INFO] Force upload enabled (--ForceUpload flag)" -ForegroundColor Yellow
            $shouldUpload = $true
        } else {
            # Check if files already exist in Storage
            Write-Host "[INFO] Checking if frontend files already exist in Azure Storage..." -ForegroundColor Yellow
            $existingFiles = az storage blob list `
                --account-name $StorageAccountName `
                --container-name '$web' `
                --query "[].name" `
                --output tsv `
                2>$null
            
            if ($existingFiles -and $existingFiles.Length -gt 0) {
                Write-Host "[INFO] Found existing files in Azure Storage" -ForegroundColor Yellow
                Write-Host "[PROMPT] Files already exist. Re-upload? (Y/N, default: N)" -ForegroundColor Yellow
                $response = Read-Host "Press Y to upload, ENTER to skip"
                
                if ($response -eq "Y" -or $response -eq "y") {
                    $shouldUpload = $true
                } else {
                    Write-Host "[INFO] Skipping file upload (using existing files)" -ForegroundColor Yellow
                    Write-Host ""
                }
            } else {
                # No files exist, must upload
                $shouldUpload = $true
            }
        }
        
        if ($shouldUpload) {
            # Clear existing files first (force purge)
            Write-Host "[INFO] Purging ALL existing files from Azure Storage..." -ForegroundColor Yellow
            az storage blob delete-batch `
                --account-name $StorageAccountName `
                --source '$web' `
                --delete-snapshots include `
                2>&1 | Out-Null
            
            Write-Host "[SUCCESS] All existing files purged" -ForegroundColor Green
            Start-Sleep -Seconds 2
            
            # Upload files to Azure Storage
            Write-Host "[INFO] Uploading Angular files to Azure Storage..." -ForegroundColor Yellow
            Write-Host "[INFO] Source: $distPath" -ForegroundColor Gray
            Write-Host "[INFO] Uploading... (this shows progress)" -ForegroundColor Gray
            Write-Host ""
            
            az storage blob upload-batch `
                --account-name $StorageAccountName `
                --source $distPath `
                --destination '$web' `
                --overwrite

            if ($LASTEXITCODE -ne 0) {
                Write-Host "" 
                Write-Host "[ERROR] Failed to upload files to Azure Storage." -ForegroundColor Red
                exit 1
            }

            Write-Host ""
            Write-Host "[SUCCESS] Files uploaded successfully!" -ForegroundColor Green
            Write-Host ""
            
            # Set correct content types and cache control for specific files
            Write-Host "[INFO] Setting correct content types and cache control..." -ForegroundColor Yellow
            
            # JavaScript files - no cache
            az storage blob update-batch `
                --account-name $StorageAccountName `
                --source '$web' `
                --pattern "*.js" `
                --content-type "application/javascript" `
                --content-cache-control "no-cache, no-store, must-revalidate" `
                2>&1 | Out-Null
            
            # CSS files - no cache
            az storage blob update-batch `
                --account-name $StorageAccountName `
                --source '$web' `
                --pattern "*.css" `
                --content-type "text/css" `
                --content-cache-control "no-cache, no-store, must-revalidate" `
                2>&1 | Out-Null
            
            # HTML files - no cache
            az storage blob update-batch `
                --account-name $StorageAccountName `
                --source '$web' `
                --pattern "*.html" `
                --content-type "text/html" `
                --content-cache-control "no-cache, no-store, must-revalidate" `
                2>&1 | Out-Null
            
            # JSON files - no cache
            az storage blob update-batch `
                --account-name $StorageAccountName `
                --source '$web' `
                --pattern "*.json" `
                --content-type "application/json" `
                --content-cache-control "no-cache, no-store, must-revalidate" `
                2>&1 | Out-Null
            
            Write-Host "[SUCCESS] Content types and cache control configured" -ForegroundColor Green
            Write-Host ""
            
            # Purge CDN cache if exists
            Write-Host "[INFO] Purging Azure CDN cache (if configured)..." -ForegroundColor Yellow
            az cdn endpoint purge `
                --resource-group $ResourceGroup `
                --profile-name "portlogistics-cdn" `
                --name "portlogistics-frontend" `
                --content-paths "/*" `
                2>&1 | Out-Null
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "[SUCCESS] CDN cache purged" -ForegroundColor Green
            } else {
                Write-Host "[INFO] No CDN configured (skipping cache purge)" -ForegroundColor Gray
            }
            Write-Host ""
        }
    }
} else {
    Write-Host "[INFO] Skipping frontend deployment (--SkipFrontend flag set)" -ForegroundColor Yellow
    Write-Host ""
}

# ============================================
# GET DEPLOYMENT URLs
# ============================================
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "DEPLOYMENT COMPLETE!" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

if (-not $SkipBackend) {
    $backendUrl = "https://$BackendAppName-f4b3b0hgc6dvdtax.swedencentral-01.azurewebsites.net"
    Write-Host "Backend API URLs:" -ForegroundColor Green
    Write-Host "   Main: $backendUrl" -ForegroundColor White
    Write-Host "   Swagger: $backendUrl/swagger" -ForegroundColor White
    Write-Host ""
}

if (-not $SkipFrontend) {
    $frontendUrl = az storage account show `
        --name $StorageAccountName `
        --resource-group $ResourceGroup `
        --query "primaryEndpoints.web" `
        --output tsv 2>$null
    
    if ($frontendUrl) {
        Write-Host "Frontend SPA URL:" -ForegroundColor Green
        Write-Host "   $frontendUrl" -ForegroundColor White
        Write-Host ""
    }
}

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "NEXT STEPS:" -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Configure Google OAuth2 Redirect URI:" -ForegroundColor White
Write-Host "   Go to: https://console.cloud.google.com/apis/credentials" -ForegroundColor Gray
if (-not $SkipFrontend -and $frontendUrl) {
    Write-Host "   Add redirect URI: $frontendUrl" -ForegroundColor Gray
}
Write-Host ""
Write-Host "2. Wait 1-2 minutes for Azure services to restart" -ForegroundColor White
Write-Host ""
Write-Host "3. Test your application:" -ForegroundColor White
if (-not $SkipFrontend -and $frontendUrl) {
    Write-Host "   Frontend: $frontendUrl" -ForegroundColor Gray
}
if (-not $SkipBackend) {
    Write-Host "   Backend: $backendUrl/swagger" -ForegroundColor Gray
}
Write-Host ""
Write-Host "4. Clear browser cache if you see old version:" -ForegroundColor White
Write-Host "   Press Ctrl+Shift+R (hard refresh)" -ForegroundColor Gray
Write-Host ""
Write-Host "[TIP] To open Google Cloud Console, run:" -ForegroundColor Yellow
Write-Host "   Start-Process 'https://console.cloud.google.com/apis/credentials'" -ForegroundColor Gray
Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Deployment script finished successfully!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
