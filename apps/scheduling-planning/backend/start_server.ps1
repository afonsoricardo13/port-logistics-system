# Start Prolog Scheduling Server
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "  Prolog Scheduling Server" -ForegroundColor Cyan
Write-Host "============================================`n" -ForegroundColor Cyan

# Try to find SWI-Prolog
$swiplPath = $null

# Check if swipl is in PATH
if (Get-Command swipl -ErrorAction SilentlyContinue) {
    $swiplPath = "swipl"
    Write-Host "Found SWI-Prolog in PATH" -ForegroundColor Green
}
# Check common installation location
elseif (Test-Path "C:\Program Files\swipl\bin\swipl.exe") {
    $swiplPath = "C:\Program Files\swipl\bin\swipl.exe"
    Write-Host "Found SWI-Prolog at: $swiplPath" -ForegroundColor Green
}
else {
    Write-Host "ERROR: SWI-Prolog not found!" -ForegroundColor Red
    Write-Host "Install from: https://www.swi-prolog.org/Download.html" -ForegroundColor Yellow
    Write-Host "`nThen restart VS Code or run:" -ForegroundColor Yellow
    Write-Host '  $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine")' -ForegroundColor Gray
    exit 1
}

# Check if scheduling_server.pl exists
if (-not (Test-Path "scheduling_server.pl")) {
    Write-Host "ERROR: scheduling_server.pl not found!" -ForegroundColor Red
    Write-Host "Run this script from PrologScheduling directory.`n" -ForegroundColor Yellow
    exit 1
}

Write-Host "Starting server on port 5003..." -ForegroundColor Cyan
Write-Host "Endpoints:" -ForegroundColor White
Write-Host "  GET  http://localhost:5003/api/scheduling/health" -ForegroundColor Gray
Write-Host "  POST http://localhost:5003/api/scheduling/generate" -ForegroundColor Gray
Write-Host "  POST http://localhost:5003/api/scheduling/daily" -ForegroundColor Gray
Write-Host "`nPress Ctrl+C to stop`n" -ForegroundColor Yellow

# Start SWI-Prolog using found path
& $swiplPath -s scheduling_server.pl -g start_scheduling_server
