# ============================================================================
# Finance & Supply Chain Demo - Clean and Run Script
# ============================================================================
# This script starts all required services for the Finance & Supply Chain demo:
# 1. Core System Services (Directory, Telemetry, User, ACL, Registry)
# 2. Finance1 Services (GeneralLedger, AccountsPayable, AccountsReceivable)
# 3. Finance2 Services (Treasury, Forecasting)
# 4. SupplyChain Services (Procurement, Inventory)
# 5. Admin Interface (port 3001)
# 6. Finance App (port 3002)
# ============================================================================

$ErrorActionPreference = "Stop"

# Get the script's directory (where this .ps1 file lives)
$ScriptDir = $PSScriptRoot
if (-not $ScriptDir) {
    $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
}
if (-not $ScriptDir) {
    $ScriptDir = Get-Location
}

# Calculate paths relative to script location
$FinanceDir = $ScriptDir
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$ConduitDir = Join-Path $RootDir "ConduitNet"
$AdminDir = Join-Path $RootDir "admin"
$AppDir = Join-Path $FinanceDir "app"

# Change to root directory for consistent path resolution
Set-Location $RootDir

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "  Finance & Supply Chain Demo - Startup Script" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""

# Kill any existing dotnet processes
Write-Host "Stopping existing processes..." -ForegroundColor Yellow
Stop-Process -Name "dotnet" -ErrorAction SilentlyContinue
Stop-Process -Name "node" -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# ============================================================================
# Build Core ConduitNet Solution
# ============================================================================
Write-Host ""
Write-Host "Building Core ConduitNet Solution..." -ForegroundColor Yellow
Push-Location $ConduitDir
dotnet clean ConduitNet.sln -v q
dotnet build ConduitNet.sln -v q
if ($LASTEXITCODE -ne 0) {
    Write-Error "Core ConduitNet build failed."
    Pop-Location
    exit 1
}
Pop-Location
Write-Host "Core ConduitNet build successful." -ForegroundColor Green

# ============================================================================
# Build Finance & Supply Chain Solution
# ============================================================================
Write-Host ""
Write-Host "Building Finance & Supply Chain Solution..." -ForegroundColor Yellow
Push-Location $FinanceDir
dotnet clean FinanceSupplyChain.sln -v q
dotnet build FinanceSupplyChain.sln -v q
if ($LASTEXITCODE -ne 0) {
    Write-Error "Finance & Supply Chain build failed."
    Pop-Location
    exit 1
}
Pop-Location
Write-Host "Finance & Supply Chain build successful." -ForegroundColor Green

# ============================================================================
# Helper Function to Start Services
# ============================================================================
function Start-ConduitService {
    param (
        [string]$Project,
        [string]$Port,
        [string]$Title,
        [string]$DirectoryUrl = "ws://localhost:5000/",
        [string]$WorkingDir = $RootDir
    )

    Write-Host "  Starting $Title on port $Port..." -ForegroundColor Gray
    
    $Args = "run --project `"$Project`" -- --Conduit:Port=$Port"
    if ($DirectoryUrl) {
        $Args += " --Conduit:DirectoryUrl=$DirectoryUrl"
    }
    
    Start-Process dotnet -ArgumentList $Args -WorkingDirectory $WorkingDir -WindowStyle Minimized
}

# ============================================================================
# Start Core System Services
# ============================================================================
Write-Host ""
Write-Host "Starting Core System Services..." -ForegroundColor Cyan

# Directory Service (must start first - no DirectoryUrl)
Start-ConduitService `
    -Project "ConduitNet\ConduitNet.System\ConduitNet.Directory\ConduitNet.Directory.csproj" `
    -Port "5000" `
    -Title "Directory Service" `
    -DirectoryUrl ""

Start-Sleep -Seconds 2

# Telemetry Service
Start-ConduitService `
    -Project "ConduitNet\ConduitNet.System\ConduitNet.Telemetry.Node\ConduitNet.Telemetry.Node.csproj" `
    -Port "5001" `
    -Title "Telemetry Service"

# User Service
Start-ConduitService `
    -Project "ConduitNet\ConduitNet.System\ConduitNet.UserService\ConduitNet.UserService.csproj" `
    -Port "5002" `
    -Title "User Service"

# ACL Service
Start-ConduitService `
    -Project "ConduitNet\ConduitNet.System\ConduitNet.AclService\ConduitNet.AclService.csproj" `
    -Port "5003" `
    -Title "ACL Service"

# Registry Service
Start-ConduitService `
    -Project "ConduitNet\ConduitNet.System\ConduitNet.Registry\ConduitNet.Registry.csproj" `
    -Port "5004" `
    -Title "Registry Service"

Write-Host "Core System Services started." -ForegroundColor Green

# ============================================================================
# Start Finance1 Services (General Ledger, AP, AR)
# ============================================================================
Write-Host ""
Write-Host "Starting Finance1 Services..." -ForegroundColor Cyan

Start-Sleep -Seconds 1

Start-ConduitService `
    -Project "Examples\FinanceSupplyChain\src\Finance1\GeneralLedger.Node\GeneralLedger.Node.csproj" `
    -Port "5101" `
    -Title "General Ledger"

Start-ConduitService `
    -Project "Examples\FinanceSupplyChain\src\Finance1\AccountsPayable.Node\AccountsPayable.Node.csproj" `
    -Port "5102" `
    -Title "Accounts Payable"

Start-ConduitService `
    -Project "Examples\FinanceSupplyChain\src\Finance1\AccountsReceivable.Node\AccountsReceivable.Node.csproj" `
    -Port "5103" `
    -Title "Accounts Receivable"

Write-Host "Finance1 Services started." -ForegroundColor Green

# ============================================================================
# Start Finance2 Services (Treasury, Forecasting)
# ============================================================================
Write-Host ""
Write-Host "Starting Finance2 Services..." -ForegroundColor Cyan

Start-ConduitService `
    -Project "Examples\FinanceSupplyChain\src\Finance2\Treasury.Node\Treasury.Node.csproj" `
    -Port "5111" `
    -Title "Treasury"

Start-ConduitService `
    -Project "Examples\FinanceSupplyChain\src\Finance2\Forecasting.Node\Forecasting.Node.csproj" `
    -Port "5113" `
    -Title "Forecasting"

Write-Host "Finance2 Services started." -ForegroundColor Green

# ============================================================================
# Start SupplyChain Services (Procurement, Inventory)
# ============================================================================
Write-Host ""
Write-Host "Starting SupplyChain Services..." -ForegroundColor Cyan

Start-ConduitService `
    -Project "Examples\FinanceSupplyChain\src\SupplyChain\Procurement.Node\Procurement.Node.csproj" `
    -Port "5121" `
    -Title "Procurement"

Start-ConduitService `
    -Project "Examples\FinanceSupplyChain\src\SupplyChain\Inventory.Node\Inventory.Node.csproj" `
    -Port "5122" `
    -Title "Inventory"

Write-Host "SupplyChain Services started." -ForegroundColor Green

# ============================================================================
# Start Admin Interface (port 3001)
# ============================================================================
Write-Host ""
Write-Host "Starting Admin Interface..." -ForegroundColor Cyan

if (-not (Test-Path (Join-Path $AdminDir "node_modules"))) {
    Write-Host "  Installing Admin dependencies..." -ForegroundColor Yellow
    Push-Location $AdminDir
    npm install
    Pop-Location
}

Push-Location $AdminDir
Start-Process powershell -ArgumentList "-NoExit -Command `"npm run dev`"" -WindowStyle Minimized
Pop-Location

Write-Host "Admin Interface started on http://localhost:3001" -ForegroundColor Green

# ============================================================================
# Start Finance App (port 3002)
# ============================================================================
Write-Host ""
Write-Host "Starting Finance App..." -ForegroundColor Cyan

if (-not (Test-Path (Join-Path $AppDir "node_modules"))) {
    Write-Host "  Installing Finance App dependencies..." -ForegroundColor Yellow
    Push-Location $AppDir
    npm install
    Pop-Location
}

Push-Location $AppDir
Start-Process powershell -ArgumentList "-NoExit -Command `"npm run dev`"" -WindowStyle Minimized
Pop-Location

Write-Host "Finance App started on http://localhost:3002" -ForegroundColor Green

# ============================================================================
# Summary
# ============================================================================
Write-Host ""
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "  All Services Started Successfully!" -ForegroundColor Green
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  CORE SYSTEM SERVICES:" -ForegroundColor White
Write-Host "    Directory:    ws://localhost:5000" -ForegroundColor Gray
Write-Host "    Telemetry:    ws://localhost:5001" -ForegroundColor Gray
Write-Host "    User:         ws://localhost:5002" -ForegroundColor Gray
Write-Host "    ACL:          ws://localhost:5003" -ForegroundColor Gray
Write-Host "    Registry:     ws://localhost:5004" -ForegroundColor Gray
Write-Host ""
Write-Host "  FINANCE1 SERVICES:" -ForegroundColor White
Write-Host "    GeneralLedger:     ws://localhost:5101" -ForegroundColor Gray
Write-Host "    AccountsPayable:   ws://localhost:5102" -ForegroundColor Gray
Write-Host "    AccountsReceivable:ws://localhost:5103" -ForegroundColor Gray
Write-Host ""
Write-Host "  FINANCE2 SERVICES:" -ForegroundColor White
Write-Host "    Treasury:     ws://localhost:5111" -ForegroundColor Gray
Write-Host "    Forecasting:  ws://localhost:5113" -ForegroundColor Gray
Write-Host ""
Write-Host "  SUPPLY CHAIN SERVICES:" -ForegroundColor White
Write-Host "    Procurement:  ws://localhost:5121" -ForegroundColor Gray
Write-Host "    Inventory:    ws://localhost:5122" -ForegroundColor Gray
Write-Host ""
Write-Host "  WEB INTERFACES:" -ForegroundColor White
Write-Host "    Admin:        http://localhost:3001" -ForegroundColor Yellow
Write-Host "    Finance App:  http://localhost:3002" -ForegroundColor Yellow
Write-Host ""
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "  Press any key to stop all services..." -ForegroundColor Magenta
Write-Host "============================================================================" -ForegroundColor Cyan

$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Cleanup
Write-Host ""
Write-Host "Stopping all services..." -ForegroundColor Yellow
Stop-Process -Name "dotnet" -ErrorAction SilentlyContinue
Stop-Process -Name "node" -ErrorAction SilentlyContinue
Write-Host "All services stopped." -ForegroundColor Green
