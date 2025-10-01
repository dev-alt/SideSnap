# Build script for SideSnap
# Cleans, builds, and runs the application

param(
    [switch]$SkipRun
)

$ErrorActionPreference = "Stop"

# Create log file with timestamp
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$logFile = "build-log_$timestamp.txt"

function Write-LoggedHost {
    param(
        [string]$Message,
        [string]$ForegroundColor = "White"
    )
    
    # Write to console with color
    Write-Host $Message -ForegroundColor $ForegroundColor
    
    # Write to log file (without color codes)
    $Message | Out-File -FilePath $logFile -Append -Encoding UTF8
}

function Wait-ForKeyPress {
    Write-LoggedHost "" 
    Write-LoggedHost "Press any key to exit..." "Yellow"
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

try {
    # Initialize log file
    "SideSnap Build Log - $(Get-Date)" | Out-File -FilePath $logFile -Encoding UTF8
    "=" * 50 | Out-File -FilePath $logFile -Append -Encoding UTF8
    
    Write-LoggedHost "=====================================" "Cyan"
    Write-LoggedHost "       SideSnap Build Script        " "Cyan"
    Write-LoggedHost "=====================================" "Cyan"
    Write-LoggedHost "Log file: $logFile" "Gray"
    Write-LoggedHost ""

    # Navigate to script directory
    $scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
    Set-Location $scriptPath

    # Clean
    Write-LoggedHost "Cleaning solution..." "Yellow"
    $cleanOutput = dotnet clean SideSnap.sln --configuration Release 2>&1 | Out-String
    $cleanOutput | Out-File -FilePath $logFile -Append -Encoding UTF8
    Write-Host $cleanOutput
    
    if ($LASTEXITCODE -ne 0) {
        throw "Clean failed with exit code $LASTEXITCODE"
    }
    Write-LoggedHost "Clean completed successfully!" "Green"
    Write-LoggedHost ""

    # Restore
    Write-LoggedHost "Restoring packages..." "Yellow"
    $restoreOutput = dotnet restore SideSnap.sln 2>&1 | Out-String
    $restoreOutput | Out-File -FilePath $logFile -Append -Encoding UTF8
    Write-Host $restoreOutput
    
    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed with exit code $LASTEXITCODE"
    }
    Write-LoggedHost "Restore completed successfully!" "Green"
    Write-LoggedHost ""

    # Build
    Write-LoggedHost "Building solution..." "Yellow"
    $buildOutput = dotnet build SideSnap.sln --configuration Release --no-restore 2>&1 | Out-String
    $buildOutput | Out-File -FilePath $logFile -Append -Encoding UTF8
    Write-Host $buildOutput
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-LoggedHost "Build completed successfully!" "Green"
    Write-LoggedHost ""

    # Run
    if (-not $SkipRun) {
        Write-LoggedHost "Running SideSnap..." "Cyan"
        Write-LoggedHost ""
        $runOutput = dotnet run --project SideSnap\SideSnap.csproj --configuration Release --no-build 2>&1 | Out-String
        $runOutput | Out-File -FilePath $logFile -Append -Encoding UTF8
        Write-Host $runOutput
        
        if ($LASTEXITCODE -ne 0) {
            throw "Application exited with exit code $LASTEXITCODE"
        }
    } else {
        Write-LoggedHost "Skipping run. Use './build.ps1' without -SkipRun to launch the app." "Yellow"
        Wait-ForKeyPress
    }
}
catch {
    Write-LoggedHost ""
    Write-LoggedHost "=====================================" "Red"
    Write-LoggedHost "ERROR: $($_.Exception.Message)" "Red"
    Write-LoggedHost "=====================================" "Red"
    
    # Also log the error
    "" | Out-File -FilePath $logFile -Append -Encoding UTF8
    "ERROR: $($_.Exception.Message)" | Out-File -FilePath $logFile -Append -Encoding UTF8
    
    Wait-ForKeyPress
    exit 1
}

# Log completion
"Build script completed at $(Get-Date)" | Out-File -FilePath $logFile -Append -Encoding UTF8
Write-LoggedHost ""
Write-LoggedHost "Build log saved to: $logFile" "Green"
