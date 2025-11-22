$ErrorActionPreference = "Stop"

Write-Host "Running Unit Tests..."
$trxFileName = "test_results.trx"
$trxPath = Join-Path $PSScriptRoot $trxFileName

# Remove old trx if exists
if (Test-Path $trxPath) { Remove-Item $trxPath }

# Run tests with TRX logger
# We use Continue for ErrorAction because we want to process the results even if tests fail
# Use --results-directory to ensure we know where the file lands
dotnet test ConduitNet/ConduitNet.Tests.Unit/ConduitNet.Tests.Unit.csproj --nologo --results-directory $PSScriptRoot --logger "trx;LogFileName=$trxFileName"

# Parse TRX
if (Test-Path $trxPath) {
    [xml]$trx = Get-Content $trxPath

    $tests = @()
    
    # Handle single result or array of results
    $results = $trx.TestRun.Results.UnitTestResult
    if ($results -isnot [System.Array]) {
        $results = @($results)
    }

    foreach ($result in $results) {
        if ($null -eq $result) { continue }

        $testName = $result.testName
        $outcome = $result.outcome
        $duration = $result.duration
        $errorMessage = $null
        
        if ($result.Output.ErrorInfo.Message) {
            $errorMessage = $result.Output.ErrorInfo.Message
        }

        $tests += @{
            name = $testName
            outcome = $outcome
            duration = $duration
            errorMessage = $errorMessage
        }
    }

    # Calculate summary from the parsed results
    $total = $tests.Count
    $passed = ($tests | Where-Object { $_.outcome -eq 'Passed' }).Count
    $failed = ($tests | Where-Object { $_.outcome -eq 'Failed' }).Count
    $skipped = ($tests | Where-Object { $_.outcome -eq 'NotExecuted' -or $_.outcome -eq 'Skipped' }).Count

    $json = @{
        total = $total
        passed = $passed
        failed = $failed
        skipped = $skipped
        timestamp = (Get-Date).ToString("o")
        tests = $tests
    } | ConvertTo-Json -Depth 10

    $outputPath = "www/public/test-results.json"
    $json | Set-Content -Path $outputPath

    # Cleanup
    Remove-Item $trxPath

    Write-Host "Test results saved to $outputPath"
} else {
    Write-Error "TRX file was not generated."
}

Write-Host "Building Website..."
Set-Location www
npm run build
Set-Location ..
