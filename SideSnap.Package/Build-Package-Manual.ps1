# Manual MSIX Package Builder for SideSnap
# This script bypasses MSBuild issues by manually creating the package structure

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$OutputPath = ".\ManualPackage"
)

Write-Host "Manual MSIX Package Builder for SideSnap" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir

# Step 1: Build the main project
Write-Host "`n[1/5] Building SideSnap project..." -ForegroundColor Yellow
$projectPath = Join-Path $projectRoot "SideSnap\SideSnap.csproj"
$appOutput = Join-Path $scriptDir "$OutputPath\temp\app"

Write-Host "  Project: $projectPath" -ForegroundColor Gray
Write-Host "  Output: $appOutput" -ForegroundColor Gray

dotnet publish $projectPath -c $Configuration -r win-x64 --self-contained false -o $appOutput

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to build SideSnap project" -ForegroundColor Red
    exit 1
}
Write-Host "Build successful!" -ForegroundColor Green

# Step 2: Create package directory structure
Write-Host "`n[2/5] Creating package directory structure..." -ForegroundColor Yellow
$packageRoot = Join-Path $scriptDir "$OutputPath\Package"
$imagesSource = Join-Path $scriptDir "Images"
$imagesDest = Join-Path $packageRoot "Images"

# Create directories
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
New-Item -ItemType Directory -Path $imagesDest -Force | Out-Null

# Step 3: Copy application files
Write-Host "`n[3/5] Copying application files..." -ForegroundColor Yellow
Copy-Item -Path "$appOutput\*" -Destination $packageRoot -Recurse -Force
Write-Host "Application files copied" -ForegroundColor Green

# Step 4: Copy images
Write-Host "`n[4/5] Copying package images..." -ForegroundColor Yellow
$imageFiles = @(
    "SplashScreen.scale-200.png",
    "LockScreenLogo.scale-200.png",
    "Square150x150Logo.scale-200.png",
    "Square44x44Logo.scale-200.png",
    "SmallTile.scale-200.png",
    "LargeTile.scale-200.png",
    "Square44x44Logo.targetsize-24_altform-unplated.png",
    "StoreLogo.png",
    "Wide310x150Logo.scale-200.png"
)

foreach ($image in $imageFiles) {
    $source = Join-Path $imagesSource $image
    $dest = Join-Path $imagesDest $image
    
    if (Test-Path $source) {
        Copy-Item -Path $source -Destination $dest -Force
        Write-Host "  ? Copied $image" -ForegroundColor Gray
    } else {
        Write-Host "  ? Missing $image" -ForegroundColor Red
    }
}

# Step 5: Copy and fix manifest
Write-Host "`n[5/5] Copying and fixing manifest..." -ForegroundColor Yellow
$manifestSource = Join-Path $scriptDir "Package.appxmanifest"
$manifestDest = Join-Path $packageRoot "AppxManifest.xml"

# Read manifest content
$manifestContent = Get-Content $manifestSource -Raw

# Replace tokens
$manifestContent = $manifestContent -replace '\$targetnametoken\$', 'SideSnap'
$manifestContent = $manifestContent -replace '\$targetentrypoint\$', 'SideSnap.App'

# Save fixed manifest
$manifestContent | Out-File -FilePath $manifestDest -Encoding UTF8 -NoNewline
Write-Host "Manifest copied and fixed" -ForegroundColor Green

# Create the package using MakeAppx.exe
Write-Host "`n[6/6] Creating MSIX package..." -ForegroundColor Yellow
$makeAppxPath = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22621.0\x64\makeappx.exe"

if (-not (Test-Path $makeAppxPath)) {
    # Try to find any available version
    $sdkPath = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
    if (Test-Path $sdkPath) {
        $versions = Get-ChildItem $sdkPath -Directory | Sort-Object Name -Descending
        foreach ($ver in $versions) {
            $testPath = Join-Path $ver.FullName "x64\makeappx.exe"
            if (Test-Path $testPath) {
                $makeAppxPath = $testPath
                Write-Host "  Found MakeAppx: $makeAppxPath" -ForegroundColor Gray
                break
            }
        }
    }
}

if (-not (Test-Path $makeAppxPath)) {
    Write-Host "ERROR: MakeAppx.exe not found. Please install Windows SDK." -ForegroundColor Red
    Write-Host "Package structure created in: $packageRoot" -ForegroundColor Yellow
    Write-Host "You can manually create the package from there." -ForegroundColor Yellow
    exit 1
}

$outputMsix = Join-Path $scriptDir "$OutputPath\SideSnap.msix"
& $makeAppxPath pack /d $packageRoot /p $outputMsix /o

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n? Package created successfully!" -ForegroundColor Green
    Write-Host "Package location: $outputMsix" -ForegroundColor Cyan
    
    # Show package info
    Write-Host "`nPackage Information:" -ForegroundColor Yellow
    Write-Host "  Location: $(Resolve-Path $outputMsix)" -ForegroundColor Gray
    $size = (Get-Item $outputMsix).Length / 1MB
    Write-Host "  Size: $([math]::Round($size, 2)) MB" -ForegroundColor Gray
    
    Write-Host "`nNext Steps:" -ForegroundColor Yellow
    Write-Host "1. Sign the package (for local testing):" -ForegroundColor Gray
    Write-Host "   SignTool sign /fd SHA256 /a /f YourCertificate.pfx $outputMsix" -ForegroundColor DarkGray
    Write-Host "2. Install the package:" -ForegroundColor Gray
    Write-Host "   Add-AppxPackage -Path $outputMsix" -ForegroundColor DarkGray
    Write-Host "3. Or upload to Partner Center for Store submission" -ForegroundColor Gray
    
} else {
    Write-Host "`nERROR: Failed to create package" -ForegroundColor Red
    Write-Host "Package structure is available at: $packageRoot" -ForegroundColor Yellow
    exit 1
}

# Cleanup temp files
Write-Host "`nCleaning up temporary files..." -ForegroundColor Yellow
$tempPath = Join-Path $scriptDir "$OutputPath\temp"
if (Test-Path $tempPath) {
    Remove-Item -Path $tempPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Temporary build files removed" -ForegroundColor Gray
}

Write-Host "`n? Done!" -ForegroundColor Green
