# Finance & Supply Chain Demo - Run Script
# This script starts all the required services and the frontend application

$ErrorActionPreference = "Stop"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$conduitNetPath = Join-Path $scriptPath "..\..\..\ConduitNet"
$examplePath = $scriptPath

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     Finance & Supply Chain Demo - Starting Services        ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Kill any existing processes on our ports
$ports = @(5000, 5101, 5102, 5103, 5111, 5113, 5121, 5122, 3002)
foreach ($port in $ports) {
    $process = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -ErrorAction SilentlyContinue
    if ($process) {
        Stop-Process -Id $process -Force -ErrorAction SilentlyContinue
        Write-Host "Stopped process on port $port" -ForegroundColor Yellow
    }
}

Start-Sleep -Seconds 1

# Function to start a dotnet project in a new terminal
function Start-DotnetService {
    param(
        [string]$Name,
        [string]$Path,
        [int]$Port
    )
    
    Write-Host "Starting $Name on port $Port..." -ForegroundColor Green
    $fullPath = Join-Path $scriptPath $Path
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$fullPath'; dotnet run"
    Start-Sleep -Milliseconds 500
}

# 1. Start the Directory Service (from main ConduitNet)
Write-Host ""
Write-Host "Step 1: Starting Core Services" -ForegroundColor Magenta
Write-Host "─────────────────────────────────────────" -ForegroundColor DarkGray

$directoryPath = Join-Path $conduitNetPath "ConduitNet.System\ConduitNet.Directory"
Write-Host "Starting Directory Service on port 5000..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$directoryPath'; dotnet run"
Start-Sleep -Seconds 2

# 2. Start Finance1 Services
Write-Host ""
Write-Host "Step 2: Starting Finance1 Services" -ForegroundColor Magenta
Write-Host "─────────────────────────────────────────" -ForegroundColor DarkGray

Start-DotnetService -Name "GeneralLedger" -Path "src\Finance1\GeneralLedger.Node" -Port 5101
Start-DotnetService -Name "AccountsPayable" -Path "src\Finance1\AccountsPayable.Node" -Port 5102
Start-DotnetService -Name "AccountsReceivable" -Path "src\Finance1\AccountsReceivable.Node" -Port 5103

# 3. Start Finance2 Services
Write-Host ""
Write-Host "Step 3: Starting Finance2 Services" -ForegroundColor Magenta
Write-Host "─────────────────────────────────────────" -ForegroundColor DarkGray

Start-DotnetService -Name "Treasury" -Path "src\Finance2\Treasury.Node" -Port 5111
Start-DotnetService -Name "Forecasting" -Path "src\Finance2\Forecasting.Node" -Port 5113

# 4. Start Supply Chain Services
Write-Host ""
Write-Host "Step 4: Starting Supply Chain Services" -ForegroundColor Magenta
Write-Host "─────────────────────────────────────────" -ForegroundColor DarkGray

Start-DotnetService -Name "Procurement" -Path "src\SupplyChain\Procurement.Node" -Port 5121
Start-DotnetService -Name "Inventory" -Path "src\SupplyChain\Inventory.Node" -Port 5122

# 5. Start the Frontend App
Write-Host ""
Write-Host "Step 5: Starting Frontend Application" -ForegroundColor Magenta
Write-Host "─────────────────────────────────────────" -ForegroundColor DarkGray

$appPath = Join-Path $scriptPath "app"
Write-Host "Starting Frontend on port 3002..." -ForegroundColor Green

# Check if node_modules exists
if (-not (Test-Path (Join-Path $appPath "node_modules"))) {
    Write-Host "Installing npm dependencies..." -ForegroundColor Yellow
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$appPath'; npm install; npm run dev" -Wait:$false
} else {
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$appPath'; npm run dev"
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "All services are starting!" -ForegroundColor Green
Write-Host ""
Write-Host "Services:" -ForegroundColor White
Write-Host "  • Directory Service:     ws://localhost:5000/conduit" -ForegroundColor Gray
Write-Host "  • GeneralLedger:         ws://localhost:5101/conduit" -ForegroundColor Gray
Write-Host "  • AccountsPayable:       ws://localhost:5102/conduit" -ForegroundColor Gray
Write-Host "  • AccountsReceivable:    ws://localhost:5103/conduit" -ForegroundColor Gray
Write-Host "  • Treasury:              ws://localhost:5111/conduit" -ForegroundColor Gray
Write-Host "  • Forecasting:           ws://localhost:5113/conduit" -ForegroundColor Gray
Write-Host "  • Procurement:           ws://localhost:5121/conduit" -ForegroundColor Gray
Write-Host "  • Inventory:             ws://localhost:5122/conduit" -ForegroundColor Gray
Write-Host ""
Write-Host "Frontend:" -ForegroundColor White
Write-Host "  • App:                   http://localhost:3002" -ForegroundColor Cyan
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to open the app in your browser..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Start-Process "http://localhost:3002"
