# Microsoft Store Release Guide for SideSnap

## Required Assets

### Application Icons
Create the following icon sizes in `/SideSnap/Assets/`:

1. **App Icon (Square)**
   - 16x16px - Taskbar/File Explorer
   - 32x32px - Taskbar
   - 48x48px - Alt+Tab dialog
   - 256x256px - High DPI displays

2. **Microsoft Store Tiles**
   - 44x44px - Store logo (small)
   - 50x50px - Store logo
   - 150x150px - Medium tile
   - 310x150px - Wide tile
   - 310x310px - Large tile
   - 71x71px - Small tile

3. **Store Listing Images**
   - 1240x600px - Hero image (required)
   - 2400x1200px - Hero image (optional, higher quality)
   - 1366x768px - Screenshots (at least 1, up to 10)
   - 1920x1080px - Screenshots (recommended for clarity)

### Splash Screen
   - 620x300px - Splash screen image

## Icon Design Recommendations

### SideSnap Icon Concept
**Design Ideas:**
1. **Minimalist Sidebar**: A simple vertical bar with snap indicators
2. **Window + Sidebar**: A window icon with a highlighted sidebar
3. **Magnetic Snap**: Geometric shapes snapping together
4. **Letter "S"**: Stylized "S" with sidebar/snap motif

**Color Scheme:**
- Primary: Modern Blue (#3498DB)
- Accent: Success Green (#27AE60)
- Background: Clean White or Dark (#2C3E50)

## MSIX Packaging Requirements

### Package.appxmanifest Structure
```xml
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities">

  <Identity
    Name="YourPublisher.SideSnap"
    Publisher="CN=YourPublisherName"
    Version="1.0.0.0" />

  <Properties>
    <DisplayName>SideSnap</DisplayName>
    <PublisherDisplayName>Your Publisher Name</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22621.0" />
  </Dependencies>

  <Resources>
    <Resource Language="en-US" />
  </Resources>

  <Applications>
    <Application Id="SideSnap" Executable="SideSnap.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements
        DisplayName="SideSnap"
        Description="Smart sidebar for quick access and window management"
        BackgroundColor="transparent"
        Square150x150Logo="Assets\Square150x150Logo.png"
        Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile
          Wide310x150Logo="Assets\Wide310x150Logo.png"
          Square310x310Logo="Assets\LargeTile.png"
          Square71x71Logo="Assets\SmallTile.png">
        </uap:DefaultTile>
        <uap:SplashScreen Image="Assets\SplashScreen.png" />
      </uap:VisualElements>
    </Application>
  </Applications>

  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
```

## Store Listing Information

### App Title
**SideSnap - Smart Sidebar & Window Manager**

### Short Description (100 chars max)
Quick access sidebar with folder shortcuts, commands, and powerful window management.

### Description (10,000 chars max)
```
SideSnap is a powerful productivity tool that brings quick access and advanced window management to your Windows desktop.

KEY FEATURES:

📁 QUICK ACCESS
• Favorite folder shortcuts with custom icons
• PowerShell command launcher
• Project groups with dropdown menus
• Drag-and-drop support for folders, scripts, and executables

🪟 WINDOW MANAGEMENT
• 10 snap zones for instant window positioning
• Save and restore multi-monitor layouts
• Auto-positioning rules for applications
• Window state detection (minimized/maximized/normal)

🎨 CUSTOMIZATION
• 5 visual styles: Solid, Glass, Acrylic, Gradient, Neumorphism
• Dark/Light mode support
• Custom gradient colors with live preview
• 4 icon pack themes
• Lock/unlock sidebar for rearranging

📝 PRODUCTIVITY TOOLS
• Quick notes scratchpad with auto-save
• Clipboard history (up to 100 items)
• Pin important notes and clipboard items
• Folder preview on hover

⚙️ SMART FEATURES
• Auto-hide sidebar with mouse detection
• Screen space reservation
• Start with Windows
• System tray integration
• Resizable sidebar

Perfect for power users, developers, and anyone who wants to boost their Windows productivity!
```

### Keywords (7 max)
- sidebar
- productivity
- window manager
- quick access
- launcher
- snap
- organizer

### Category
**Productivity**

### Age Rating
**Everyone**

### Privacy Policy URL
Required - create a simple privacy policy page

### Support Contact
Your support email

## Building for Release

### Create Release Build
```powershell
dotnet publish SideSnap/SideSnap.csproj -c Release -r win10-x64 --self-contained false
```

### Create MSIX Package
1. Install Windows Application Packaging Project
2. Add reference to SideSnap project
3. Configure Package.appxmanifest
4. Build in Release mode
5. Create package: Project → Publish → Create App Packages

## Testing Checklist

- [ ] All features work in Release build
- [ ] Startup with Windows functions correctly
- [ ] Icons display properly at all sizes
- [ ] App installs cleanly from MSIX
- [ ] App uninstalls without leaving files
- [ ] Settings persist after restart
- [ ] No crashes or errors in Event Viewer
- [ ] Performance is acceptable
- [ ] Screen DPI scaling works
- [ ] Multi-monitor support works

## Submission Requirements

1. **Publisher Account**: Register at partner.microsoft.com
2. **App Certification**: Pass Windows App Certification Kit (WACK)
3. **Age Rating**: Complete IARC questionnaire
4. **Privacy Policy**: Provide URL to privacy policy
5. **Screenshots**: At least 1, recommended 3-5
6. **App Package**: MSIX or MSIXBUNDLE
7. **Testing**: Test on clean Windows installation

## Post-Release

- Monitor crash reports in Partner Center
- Respond to user reviews
- Plan update cadence
- Track download metrics
