# Kill any existing dotnet processes (brute force but effective for dev)
Stop-Process -Name "dotnet" -ErrorAction SilentlyContinue
# Kill any existing node processes (for the admin interface)
Stop-Process -Name "node" -ErrorAction SilentlyContinue

# Clean the solution
Write-Host "Cleaning Solution..." -ForegroundColor Yellow
dotnet clean .\ConduitNet\ConduitNet.sln

# Build the solution
Write-Host "Building Solution..." -ForegroundColor Cyan
dotnet build .\ConduitNet\ConduitNet.sln

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit 1
}

# Function to start a service
function Start-ConduitService {
    param (
        [string]$Project,
        [string]$Port,
        [string]$Title,
        [string]$DirectoryUrl = "ws://localhost:5000/"
    )

    Write-Host "Starting $Title on port $Port..." -ForegroundColor Green
    
    # Start in a new window
    $Args = "run --project $Project -- --Conduit:Port=$Port"
    if ($DirectoryUrl) {
        $Args += " --Conduit:DirectoryUrl=$DirectoryUrl"
    }
    
    Start-Process dotnet -ArgumentList $Args -WorkingDirectory $PWD
}

# Start Directory Service (The Leader/Registry)
Start-ConduitService -Project "ConduitNet\ConduitNet.System\ConduitNet.Directory\ConduitNet.Directory.csproj" -Port "5000" -Title "Directory Service" -DirectoryUrl ""

# Give the directory a moment to start
Start-Sleep -Seconds 2

# Start Telemetry Service
Start-ConduitService -Project "ConduitNet\ConduitNet.System\ConduitNet.Telemetry.Node\ConduitNet.Telemetry.Node.csproj" -Port "5001" -Title "Telemetry Service"

# Start User Service
Start-ConduitService -Project "ConduitNet\ConduitNet.System\ConduitNet.UserService\ConduitNet.UserService.csproj" -Port "5002" -Title "User Service"

# Start ACL Service
Start-ConduitService -Project "ConduitNet\ConduitNet.System\ConduitNet.AclService\ConduitNet.AclService.csproj" -Port "5003" -Title "ACL Service"

# Start Registry Service
Start-ConduitService -Project "ConduitNet\ConduitNet.System\ConduitNet.Registry\ConduitNet.Registry.csproj" -Port "5004" -Title "Registry Service"

# Start Admin Interface
Write-Host "Starting Admin Interface..." -ForegroundColor Green
if (-not (Test-Path "admin\node_modules")) {
    Write-Host "Installing Admin dependencies..." -ForegroundColor Yellow
    Push-Location admin
    npm install
    Pop-Location
}

# Start Admin Interface in a new console window
Push-Location admin
Start-Process powershell -ArgumentList "-NoExit -Command `"npm run dev`""
Pop-Location

Write-Host "All services started." -ForegroundColor Cyan
Write-Host "Directory: ws://localhost:5000"
Write-Host "Telemetry: ws://localhost:5001"
Write-Host "User:      ws://localhost:5002"
Write-Host "ACL:       ws://localhost:5003"
Write-Host "Registry:  ws://localhost:5004"
Write-Host "Admin:     http://localhost:3001"
