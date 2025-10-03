# Create placeholder icons for SideSnap
# This script generates simple colored icons in required sizes

$ErrorActionPreference = "Stop"

# Create Assets directory if it doesn't exist
$assetsDir = "SideSnap/Assets"
if (-not (Test-Path $assetsDir)) {
    New-Item -ItemType Directory -Path $assetsDir | Out-Null
}

# Icon sizes needed
$sizes = @(
    @{Name="Square44x44Logo"; Size=44},
    @{Name="Square71x71Logo"; Size=71},
    @{Name="Square150x150Logo"; Size=150},
    @{Name="Square310x310Logo"; Size=310},
    @{Name="Wide310x150Logo"; Width=310; Height=150},
    @{Name="StoreLogo"; Size=50},
    @{Name="SplashScreen"; Width=620; Height=300}
)

# Create C# code to generate icons
$code = @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class IconGenerator
{
    public static void CreateIcon(string path, int width, int height)
    {
        using (var bitmap = new Bitmap(width, height))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            // Background gradient (blue theme)
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, width, height),
                Color.FromArgb(52, 152, 219),  // #3498DB
                Color.FromArgb(44, 62, 80),     // #2C3E50
                45f))
            {
                graphics.FillRectangle(brush, 0, 0, width, height);
            }

            // Draw sidebar representation
            int sidebarWidth = width / 4;
            using (var sidebarBrush = new SolidBrush(Color.FromArgb(44, 62, 80)))
            {
                graphics.FillRectangle(sidebarBrush, 0, 0, sidebarWidth, height);
            }

            // Draw snap indicators (small circles)
            int indicatorSize = Math.Max(width / 20, 4);
            using (var indicatorBrush = new SolidBrush(Color.FromArgb(39, 174, 96))) // #27AE60
            {
                int gridX = sidebarWidth + (width - sidebarWidth) / 3;
                int gridY = height / 3;

                if (width >= 44)
                {
                    graphics.FillEllipse(indicatorBrush, gridX - indicatorSize/2, gridY - indicatorSize/2, indicatorSize, indicatorSize);
                    graphics.FillEllipse(indicatorBrush, gridX - indicatorSize/2 + (width - sidebarWidth)/3, gridY - indicatorSize/2, indicatorSize, indicatorSize);

                    if (height >= 44)
                    {
                        graphics.FillEllipse(indicatorBrush, gridX - indicatorSize/2, gridY*2 - indicatorSize/2, indicatorSize, indicatorSize);
                        graphics.FillEllipse(indicatorBrush, gridX - indicatorSize/2 + (width - sidebarWidth)/3, gridY*2 - indicatorSize/2, indicatorSize, indicatorSize);
                    }
                }
            }

            // Save as PNG with transparency
            bitmap.Save(path, ImageFormat.Png);
        }
    }
}
'@

# Compile and run C# code
Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing

Write-Host "Creating placeholder icons..." -ForegroundColor Cyan

foreach ($iconDef in $sizes) {
    $width = if ($iconDef.Width) { $iconDef.Width } else { $iconDef.Size }
    $height = if ($iconDef.Height) { $iconDef.Height } else { $iconDef.Size }
    $filename = "$assetsDir/$($iconDef.Name).png"

    [IconGenerator]::CreateIcon($filename, $width, $height)
    Write-Host "  Created: $filename ($width x $height)" -ForegroundColor Green
}

Write-Host "`nPlaceholder icons created successfully!" -ForegroundColor Green
Write-Host "Icons are located in: $assetsDir" -ForegroundColor Yellow
