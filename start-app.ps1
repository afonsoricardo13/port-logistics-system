Write-Host "Starting Port Logistics Application..." -ForegroundColor Green

$processes = @()

Write-Host "Starting Operations Execution Backend..." -ForegroundColor Yellow
$processes += Start-Process dotnet -ArgumentList "run", "--launch-profile", "https" -WorkingDirectory "apps\operations-execution\backend\WebApi" -WindowStyle Hidden -PassThru

Write-Host "Starting Port Logistics Backend..." -ForegroundColor Yellow
$processes += Start-Process dotnet -ArgumentList "run", "--launch-profile", "https" -WorkingDirectory "apps\port-logistics\backend\WebApi" -WindowStyle Hidden -PassThru

Write-Host "Starting Scheduling Planning Backend..." -ForegroundColor Yellow
$processes += Start-Process powershell -ArgumentList "./start_server.ps1" -WorkingDirectory "apps\scheduling-planning\backend" -WindowStyle Hidden -PassThru

Write-Host "Starting Frontend SPA..." -ForegroundColor Yellow
$processes += Start-Process cmd -ArgumentList "/c", "npm", "start" -WorkingDirectory "apps\port-logistics\frontend\web\spa" -WindowStyle Hidden -PassThru

Start-Sleep -Seconds 5

Write-Host "All services started!" -ForegroundColor Green
Write-Host "Frontend SPA: http://localhost:4200/" -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop all services" -ForegroundColor Red

try {
    while ($true) {
        Start-Sleep -Seconds 5
    }
} finally {
    Write-Host "`nStopping all services..." -ForegroundColor Red
    
    # Matar processos pais
    $processes | ForEach-Object { 
        if ($_ -and !$_.HasExited) { 
            Write-Host "  Killing parent process PID: $($_.Id)" -ForegroundColor Yellow
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
        }
    }
    
    # Matar processos filhos que possam ter ficado órfãos (por porta)
    Write-Host "  Cleaning up orphaned processes on ports..." -ForegroundColor Yellow
    $ports = @(4200, 5003, 7096, 7097, 7098)
    foreach ($port in $ports) {
        Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | 
            ForEach-Object { 
                Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue 
            }
    }
    
    Write-Host "`nAll services stopped!" -ForegroundColor Green
}