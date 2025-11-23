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

    $url = "http://localhost:$Port"
    Write-Host "Starting $Title on $url..." -ForegroundColor Green
    
    # Start in a new window
    Start-Process dotnet -ArgumentList "run --project $Project --urls=$url" -WorkingDirectory $PWD
}

# Start Directory Service (The Leader/Registry)
Start-ConduitService -Project "Examples\ConduitNet.Examples.Directory\ConduitNet.Examples.Directory.csproj" -Port "5000" -Title "Directory Service"

# Give the directory a moment to start
Start-Sleep -Seconds 2

# Start Telemetry Service
Start-ConduitService -Project "ConduitNet\ConduitNet.Telemetry.Node\ConduitNet.Telemetry.Node.csproj" -Port "5001" -Title "Telemetry Service"

# Start User Service
Start-ConduitService -Project "Examples\ConduitNet.Examples.UserService\ConduitNet.Examples.UserService.csproj" -Port "5002" -Title "User Service"

# Start ACL Service
Start-ConduitService -Project "Examples\ConduitNet.Examples.AclService\ConduitNet.Examples.AclService.csproj" -Port "5003" -Title "ACL Service"

Write-Host "All services started." -ForegroundColor Cyan
Write-Host "Directory: http://localhost:5000"
Write-Host "Telemetry: http://localhost:5001"
Write-Host "User:      http://localhost:5002"
Write-Host "ACL:       http://localhost:5003"
