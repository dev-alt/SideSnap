# Build script for SideSnap
# Cleans, builds, and runs the application

param(
    [switch]$SkipRun
)

$ErrorActionPreference = "Stop"

# Setup logging
$logDir = Join-Path $PSScriptRoot "logs"
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir | Out-Null
}
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$logFile = Join-Path $logDir "build_$timestamp.log"

function Write-Log {
    param([string]$Message, [string]$Color = "White")
    $logMessage = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - $Message"
    Add-Content -Path $logFile -Value $logMessage
    Write-Host $Message -ForegroundColor $Color
}

function Wait-ForKeyPress {
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

try {
    Write-Log "=====================================" -Color Cyan
    Write-Log "       SideSnap Build Script        " -Color Cyan
    Write-Log "=====================================" -Color Cyan
    Write-Log "Log file: $logFile" -Color Gray
    Write-Log ""

    # Navigate to script directory
    $scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
    Set-Location $scriptPath

    # Git Pull
    Write-Log "Pulling latest changes from git..." -Color Yellow
    $pullOutput = git pull 2>&1 | Out-String
    Add-Content -Path $logFile -Value $pullOutput
    Write-Host $pullOutput
    if ($LASTEXITCODE -ne 0) {
        Write-Log "Git pull had issues (exit code $LASTEXITCODE), continuing anyway..." -Color Yellow
    } else {
        Write-Log "Git pull completed!" -Color Green
    }
    Write-Log ""

    # Clean
    Write-Log "Cleaning solution..." -Color Yellow
    $cleanOutput = dotnet clean SideSnap.sln --configuration Release 2>&1 | Out-String
    Add-Content -Path $logFile -Value $cleanOutput
    Write-Host $cleanOutput
    if ($LASTEXITCODE -ne 0) {
        throw "Clean failed with exit code $LASTEXITCODE"
    }
    Write-Log "Clean completed successfully!" -Color Green
    Write-Log ""

    # Restore
    Write-Log "Restoring packages..." -Color Yellow
    $restoreOutput = dotnet restore SideSnap.sln 2>&1 | Out-String
    Add-Content -Path $logFile -Value $restoreOutput
    Write-Host $restoreOutput
    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed with exit code $LASTEXITCODE"
    }
    Write-Log "Restore completed successfully!" -Color Green
    Write-Log ""

    # Build
    Write-Log "Building solution..." -Color Yellow
    $buildOutput = dotnet build SideSnap.sln --configuration Release --no-restore 2>&1 | Out-String
    Add-Content -Path $logFile -Value $buildOutput
    Write-Host $buildOutput
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-Log "Build completed successfully!" -Color Green
    Write-Log ""

    # Run
    if (-not $SkipRun) {
        Write-Log "Running SideSnap..." -Color Cyan
        Write-Log ""
        $runOutput = dotnet run --project SideSnap\SideSnap.csproj --configuration Release --no-build 2>&1 | Out-String
        Add-Content -Path $logFile -Value $runOutput
        Write-Host $runOutput
        if ($LASTEXITCODE -ne 0) {
            throw "Application exited with exit code $LASTEXITCODE"
        }
    } else {
        Write-Log "Skipping run. Use './build.ps1' without -SkipRun to launch the app." -Color Yellow
        Wait-ForKeyPress
    }
}
catch {
    Write-Log "" -Color Red
    Write-Log "=====================================" -Color Red
    Write-Log "ERROR: $($_.Exception.Message)" -Color Red
    Write-Log "=====================================" -Color Red
    Wait-ForKeyPress
    exit 1
}