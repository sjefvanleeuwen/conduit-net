# Build the solution
Write-Host "Building ConduitNet Solution..." -ForegroundColor Cyan
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
        [string]$Title
    )

    $wssUrl = "wss://localhost:$Port"
    Write-Host "Starting $Title on $wssUrl (mTLS)..." -ForegroundColor Green
    
    # We pass Conduit:Port so our code configures Kestrel with mTLS
    # We pass Conduit:NodeUrl so the node knows its own address to register
    # We pass Conduit:DirectoryUrl pointing to WSS
    
    $args = "run --project $Project --Conduit:Port=$Port --Conduit:NodeUrl=$wssUrl --Conduit:DirectoryUrl=wss://localhost:5000/conduit"
    
    # Start in a new window
    Start-Process dotnet -ArgumentList $args -WorkingDirectory $PWD
}

# Start Directory Service (The Leader/Registry)
Start-ConduitService -Project "ConduitNet\ConduitNet.System\ConduitNet.Directory\ConduitNet.Directory.csproj" -Port "5000" -Title "Directory Service"

# Give the directory a moment to start
Start-Sleep -Seconds 2

# Start Telemetry Service
Start-ConduitService -Project "ConduitNet\ConduitNet.System\ConduitNet.Telemetry.Node\ConduitNet.Telemetry.Node.csproj" -Port "5001" -Title "Telemetry Service"

# Start User Service
Start-ConduitService -Project "ConduitNet\ConduitNet.System\ConduitNet.UserService\ConduitNet.UserService.csproj" -Port "5002" -Title "User Service"

# Start ACL Service
Start-ConduitService -Project "ConduitNet\ConduitNet.System\ConduitNet.AclService\ConduitNet.AclService.csproj" -Port "5003" -Title "ACL Service"

Write-Host "All services started in mTLS mode." -ForegroundColor Cyan
Write-Host "Directory: wss://localhost:5000"
Write-Host "Telemetry: wss://localhost:5001"
Write-Host "User:      wss://localhost:5002"
Write-Host "ACL:       wss://localhost:5003"
