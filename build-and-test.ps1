$ErrorActionPreference = "Continue"

# Setup Logging
$publicDir = Join-Path $PSScriptRoot "www/public"
if (-not (Test-Path $publicDir)) { New-Item -ItemType Directory -Path $publicDir -Force | Out-Null }
$logPath = Join-Path $publicDir "build.log"

# Start Transcript to capture build output
try { Start-Transcript -Path $logPath -Force -ErrorAction SilentlyContinue } catch { Write-Warning "Could not start transcript" }

$unitTestExitCode = 0
$e2eTestExitCode = 0

# Run Unit Tests
Write-Host "Running Unit Tests..."
dotnet test ConduitNet/ConduitNet.Tests.Unit/ConduitNet.Tests.Unit.csproj --nologo --results-directory $PSScriptRoot --logger "trx;LogFileName=unit_tests.trx"
$unitTestExitCode = $LASTEXITCODE

# Run E2E Tests
Write-Host "Running E2E Tests..."
dotnet test ConduitNet/ConduitNet.Tests.E2E/ConduitNet.Tests.E2E.csproj --nologo --results-directory $PSScriptRoot --logger "trx;LogFileName=e2e_tests.trx"
$e2eTestExitCode = $LASTEXITCODE

$tests = @()

# Function to parse TRX
function Import-TrxResults {
    param ($trxPath, $type)
    $localTests = @()
    if (Test-Path $trxPath) {
        [xml]$trx = Get-Content $trxPath
        $results = $trx.TestRun.Results.UnitTestResult
        if ($results -isnot [System.Array]) { $results = @($results) }
        
        foreach ($result in $results) {
            if ($null -eq $result) { continue }
            $localTests += @{
                name = "$type - $($result.testName)"
                outcome = $result.outcome
                duration = $result.duration
                errorMessage = $result.Output.ErrorInfo.Message
            }
        }
        Remove-Item $trxPath
    }
    return $localTests
}

$tests += Import-TrxResults -trxPath (Join-Path $PSScriptRoot "unit_tests.trx") -type "Unit"
$tests += Import-TrxResults -trxPath (Join-Path $PSScriptRoot "e2e_tests.trx") -type "E2E"

# Stop Transcript before reading it
try { Stop-Transcript -ErrorAction SilentlyContinue } catch {}

# Read Build Log
$buildLogContent = ""
if (Test-Path $logPath) {
    $buildLogContent = Get-Content $logPath -Raw
}

# Calculate summary
$total = $tests.Count
$passed = ($tests | Where-Object { $_.outcome -eq 'Passed' }).Count
$failed = ($tests | Where-Object { $_.outcome -eq 'Failed' }).Count
$skipped = ($tests | Where-Object { $_.outcome -eq 'NotExecuted' -or $_.outcome -eq 'Skipped' }).Count

# Capture Build Info
$buildInfo = @{
    runNumber = if ($env:GITHUB_RUN_NUMBER) { $env:GITHUB_RUN_NUMBER } else { "Local" }
    commitHash = if ($env:GITHUB_SHA) { $env:GITHUB_SHA.Substring(0, 7) } else { "N/A" }
    branch = if ($env:GITHUB_REF_NAME) { $env:GITHUB_REF_NAME } else { "local" }
    actor = if ($env:GITHUB_ACTOR) { $env:GITHUB_ACTOR } else { $env:USERNAME }
    workflow = if ($env:GITHUB_WORKFLOW) { $env:GITHUB_WORKFLOW } else { "Manual Run" }
}

$json = @{
    total = $total
    passed = $passed
    failed = $failed
    skipped = $skipped
    timestamp = (Get-Date).ToString("o")
    buildInfo = $buildInfo
    buildLog = $buildLogContent
    tests = $tests
} | ConvertTo-Json -Depth 10

$outputPath = "www/public/test-results.json"
$json | Set-Content -Path $outputPath

Write-Host "Test results saved to $outputPath"

Write-Host "Building Website..."
Set-Location www
npm run build
Set-Location ..

if ($unitTestExitCode -ne 0 -or $e2eTestExitCode -ne 0) {
    Write-Error "One or more tests failed."
    exit 1
}

