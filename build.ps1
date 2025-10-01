# Build script for SideSnap
# Cleans, builds, and runs the application

param(
    [switch]$SkipRun
)

$ErrorActionPreference = "Stop"

function Wait-ForKeyPress {
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

try {
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host "       SideSnap Build Script        " -ForegroundColor Cyan
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host ""

    # Navigate to script directory
    $scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
    Set-Location $scriptPath

    # Clean
    Write-Host "Cleaning solution..." -ForegroundColor Yellow
    dotnet clean SideSnap.sln --configuration Release 2>&1 | Out-String | Write-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Clean failed with exit code $LASTEXITCODE"
    }
    Write-Host "Clean completed successfully!" -ForegroundColor Green
    Write-Host ""

    # Restore
    Write-Host "Restoring packages..." -ForegroundColor Yellow
    dotnet restore SideSnap.sln 2>&1 | Out-String | Write-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed with exit code $LASTEXITCODE"
    }
    Write-Host "Restore completed successfully!" -ForegroundColor Green
    Write-Host ""

    # Build
    Write-Host "Building solution..." -ForegroundColor Yellow
    dotnet build SideSnap.sln --configuration Release --no-restore 2>&1 | Out-String | Write-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-Host "Build completed successfully!" -ForegroundColor Green
    Write-Host ""

    # Run
    if (-not $SkipRun) {
        Write-Host "Running SideSnap..." -ForegroundColor Cyan
        Write-Host ""
        dotnet run --project SideSnap\SideSnap.csproj --configuration Release --no-build 2>&1 | Out-String | Write-Host
        if ($LASTEXITCODE -ne 0) {
            throw "Application exited with exit code $LASTEXITCODE"
        }
    } else {
        Write-Host "Skipping run. Use './build.ps1' without -SkipRun to launch the app." -ForegroundColor Yellow
        Wait-ForKeyPress
    }
}
catch {
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Red
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "=====================================" -ForegroundColor Red
    Wait-ForKeyPress
    exit 1
}