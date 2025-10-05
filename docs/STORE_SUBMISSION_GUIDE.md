# Microsoft Store Submission Guide for SideSnap

## Prerequisites

### 1. Developer Account
- Register at [Microsoft Partner Center](https://partner.microsoft.com/dashboard)
- One-time registration fee: $19 for individual developers
- Verification may take 1-3 business days

### 2. Tools Required
- Visual Studio 2022 (Community Edition or higher)
- Windows SDK 10.0.22621.0 or later
- Windows Application Packaging Project extension

## Project Structure

```
SideLaunch/
├── SideSnap/                          # Main WPF application
│   ├── Assets/                        # Application icons (created ✓)
│   │   ├── Square44x44Logo.png
│   │   ├── Square71x71Logo.png
│   │   ├── Square150x150Logo.png
│   │   ├── Square310x310Logo.png
│   │   ├── Wide310x150Logo.png
│   │   ├── StoreLogo.png
│   │   └── SplashScreen.png
│   └── Package.appxmanifest          # MSIX manifest
├── SideSnap.Package/                  # MSIX Packaging project (created ✓)
│   ├── Images/                        # Package images (copied ✓)
│   ├── Package.appxmanifest          # Package manifest
│   └── SideSnap.Package.wapproj      # Packaging project file
├── PRIVACY_POLICY.md                  # Privacy policy (created ✓)
└── docs/
    ├── MSSTORE_SETUP.md              # Setup guide
    ├── ICON_DESIGN.md                # Icon design spec
    └── STORE_SUBMISSION_GUIDE.md     # This file
```

## Step-by-Step Submission Process

### Identifiers (for SideSnap)

- Package/Identity/Name: `Dev-Alt.SideSnap`
- Package/Identity/Publisher: `CN=72D846BF-7EF4-4A7E-AB29-7DDF7B1E4916`
- Package/Properties/PublisherDisplayName: `Dev-Alt`
- Package Family Name (PFN): `Dev-Alt.SideSnap_n5bevhycpgrya`
- Package SID: `S-1-15-2-2479084337-1392210777-1530537585-3357496515-3183028803-3683225135-2979958439`
- Store ID: `9NH1LJHQS8GS`
- Web Store URL: Available after the product is live

### Step 1: Configure Publisher Identity

1. **Get your Publisher ID** from Partner Center:
   - Go to https://partner.microsoft.com/dashboard
   - Navigate to Account Settings → Publisher Profile
   - Copy your Publisher ID (format: `CN=XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX`)

2. **Update Package.appxmanifest**:
   ```xml
   <Identity
     Name="[YourUniqueName].SideSnap"
     Publisher="CN=[YourPublisherId]"
     Version="1.0.0.0" />

   <Properties>
     <PublisherDisplayName>[Your Display Name]</PublisherDisplayName>
   </Properties>
   ```

3. **Reserve App Name** in Partner Center:
   - Dashboard → Apps and games → New product → MSIX or PWA app
   - Enter "SideSnap" or your preferred name
   - Check availability and reserve

### Step 2: Build Release Package

#### Option A: Visual Studio (Recommended)

1. **Open Solution in Visual Studio**:
   ```bash
   # From Windows
   start SideLaunch.sln
   ```

2. **Set Package Project as Startup**:
   - Right-click `SideSnap.Package` → Set as Startup Project

3. **Configure for Release**:
   - Build → Configuration Manager
   - Set to `Release` configuration
   - Platform: `x64`

4. **Create App Package**:
   - Right-click `SideSnap.Package` → Publish → Create App Packages
   - Select "Microsoft Store using a new app name"
   - Sign in with Partner Center account
   - Select your reserved app name
   - Configure version number and architecture (x64)
   - Choose output location
   - Click Create

#### Option B: Command Line (MSBuild)

```powershell
# Restore dependencies
dotnet restore SideSnap/SideSnap.csproj

# Build in Release mode
dotnet publish SideSnap/SideSnap.csproj -c Release -r win10-x64 --self-contained false

# Build MSIX package (requires Visual Studio)
msbuild SideSnap.Package/SideSnap.Package.wapproj /p:Configuration=Release /p:Platform=x64 /p:AppxBundle=Always /p:UapAppxPackageBuildMode=StoreUpload
```

### Step 3: Test with WACK (Windows App Certification Kit)

1. **Run WACK** on the package:
   ```powershell
   # Install WACK (comes with Windows SDK)
   # Run from Windows
   "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\appcert.exe"
   ```

2. **Select Package**:
   - Browse to `SideSnap.Package\AppPackages\SideSnap_1.0.0.0_Test\`
   - Select the `.msixbundle` or `.appxbundle` file

3. **Run Tests**:
   - Wait for all tests to complete (10-15 minutes)
   - Review results - must pass all tests

4. **Fix Common Issues**:
   - **Performance**: Ensure startup time < 5 seconds
   - **Resources**: Check for resource leaks
   - **API Usage**: Verify no deprecated APIs used

### Step 4: Create Store Listing

1. **Log into Partner Center**:
   - https://partner.microsoft.com/dashboard

2. **Select Your App**:
   - Dashboard → Apps and games → Your reserved app

3. **Complete Store Listing** (under "Store listings" → English (United States)):

   **App name**: `SideSnap - Smart Sidebar & Window Manager`

   **Description** (10,000 chars max):
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

   **Short description** (200 chars max):
   ```
   Smart sidebar for quick folder access, PowerShell commands, and powerful window management with snap zones, layouts, and automation rules.
   ```

   **Screenshots** (at least 1, recommended 4-8):
   - Take screenshots at 1920x1080 or higher
   - Show key features:
     1. Main sidebar with folders
     2. Window snap zones
     3. Settings panel
     4. Window layouts
   - Upload as PNG or JPG

   **App features** (up to 20):
   - Quick folder shortcuts
   - Window snap zones
   - PowerShell launcher
   - Multi-monitor layouts
   - Auto-hide sidebar
   - Clipboard history
   - Quick notes
   - Dark mode

   **Keywords** (7 max):
   - sidebar
   - productivity
   - window manager
   - quick access
   - launcher
   - snap
   - organizer

   **Category**: Productivity

   **Subcategory**: Personal productivity

4. **Age Rating**:
   - Complete the questionnaire
   - SideSnap should receive "Everyone" rating

5. **Privacy Policy**:
   - **Required**: Must provide URL
   - **Privacy Policy URL**: `https://dev-alt.github.io/SideSnap/privacy-policy.html`

   **Setup Instructions**:
   - HTML files are already created in `docs/` folder
   - Enable GitHub Pages in repository settings:
     1. Go to: `https://github.com/dev-alt/SideSnap/settings/pages`
     2. Source: Deploy from a branch
     3. Branch: `master`, Folder: `/docs`
     4. Click Save
   - Site will be live at: `https://dev-alt.github.io/SideSnap/`
   - See `docs/GITHUB_PAGES_SETUP.md` for detailed instructions

### Step 5: Pricing and Availability

1. **Markets**: Select markets (recommended: all markets)
2. **Pricing**:
   - Free (recommended for initial release)
   - Or set price (supports 140+ currencies)
3. **Availability Date**: Choose "As soon as possible" or schedule

### Step 6: Properties

1. **Category**: Productivity
2. **Subcategory**: Personal productivity
3. **App declarations**:
   - Check if app uses any restricted capabilities
   - SideSnap uses: runFullTrust
4. **System requirements**:
   - Minimum: Windows 10 version 1809 (Build 17763)
   - Recommended: Windows 11

### Step 7: Upload Package

1. **Navigate to Packages**:
   - Dashboard → Your app → Packages

2. **Upload Package**:
   - Drag and drop `.msixbundle` or `.appxbundle` file
   - Wait for validation (5-10 minutes)

3. **Verify Package Details**:
   - Version number
   - Supported architectures (x64)
   - Capabilities listed

### Step 8: Submit for Certification

1. **Review All Sections**:
   - ✅ Store listing complete
   - ✅ Pricing set
   - ✅ Properties configured
   - ✅ Package uploaded
   - ✅ Age rating completed
   - ✅ Privacy policy URL provided

2. **Notes to Testers** (optional):
   ```
   Test Account: Not required (no login needed)

   Testing Instructions:
   1. Launch SideSnap from Start Menu
   2. Sidebar appears on left edge of screen
   3. Add folder shortcuts by clicking + button
   4. Test window snap zones by dragging windows near edges
   5. Access settings via system tray icon

   Known Limitations:
   - Requires Windows 10 version 1809 or later
   - PowerShell commands require appropriate permissions
   ```

3. **Click Submit**:
   - Certification process begins
   - Typically takes 24-48 hours

### Step 9: Monitor Certification

1. **Check Status**:
   - Dashboard → Your app → Certification status

2. **Certification Stages**:
   - ✅ Pre-processing (minutes)
   - ✅ Security tests (hours)
   - ✅ Technical compliance (hours)
   - ✅ Content compliance (hours)
   - ✅ Publishing (minutes)

3. **If Failed**:
   - Review failure report
   - Fix issues
   - Resubmit with fixes

## Post-Submission

### Monitor Performance
- **Analytics**: Dashboard → Analytics
  - Downloads
  - Usage
  - Ratings and reviews
  - Crashes

### Respond to Reviews
- Reply to user reviews within 24-48 hours
- Address bugs and feature requests

### Update Cadence
- **Critical bugs**: Immediate update
- **Features**: Monthly releases
- **Minor fixes**: Bi-weekly

### Update Process
1. Increment version in Package.appxmanifest
2. Build new package
3. Run WACK
4. Upload to existing submission
5. Submit for certification

## Troubleshooting

### Common Certification Failures

**1. WACK Performance Test Fails**
```
Error: App takes too long to launch
Fix: Optimize App.xaml.cs startup, defer heavy initialization
```

**2. Privacy Policy Not Accessible**
```
Error: Privacy policy URL returns 404
Fix: Verify URL is public and accessible
```

**3. Icon Size Incorrect**
```
Error: Icon does not meet size requirements
Fix: Regenerate icons with exact pixel dimensions
```

**4. Package Validation Failed**
```
Error: Publisher identity mismatch
Fix: Update Publisher ID in manifest to match Partner Center
```

**5. Startup Task Not Declared**
```
Error: Uses desktop:Extension but not declared
Fix: Already configured in Package.appxmanifest
```

### Testing Locally

**Install Package Locally**:
```powershell
# Install certificate (first time only)
Add-AppxPackage -Path "SideSnap_1.0.0.0_x64.msixbundle" -DependencyPath "Dependencies\x64" -ForceApplicationShutdown

# Uninstall
Get-AppxPackage SideSnap | Remove-AppxPackage
```

**Debug Installed Package**:
- Visual Studio → Debug → Other Debug Targets → Debug Installed App Package
- Select SideSnap
- Set breakpoints and debug

## Checklist Before Submission

- [ ] Publisher ID updated in manifest
- [ ] App name reserved in Partner Center
- [ ] All icons created and correct sizes
- [ ] Privacy policy URL accessible
- [ ] Package built in Release mode
- [ ] WACK tests passed
- [ ] App tested on clean Windows install
- [ ] Screenshots captured (1920x1080)
- [ ] Store listing description written
- [ ] Age rating completed
- [ ] Pricing configured
- [ ] Package uploaded successfully
- [ ] All sections show green checkmarks

## Resources

- **Partner Center**: https://partner.microsoft.com/dashboard
- **MSIX Documentation**: https://docs.microsoft.com/windows/msix/
- **Store Policies**: https://docs.microsoft.com/windows/uwp/publish/store-policies
- **WACK Guide**: https://docs.microsoft.com/windows/uwp/debug-test-perf/windows-app-certification-kit

## Quick Commands Reference

```powershell
# Build release
dotnet publish SideSnap/SideSnap.csproj -c Release -r win10-x64

# Create package (requires Visual Studio)
msbuild SideSnap.Package/SideSnap.Package.wapproj /p:Configuration=Release /p:Platform=x64

# Run WACK
& "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\appcert.exe"

# Install locally for testing
Add-AppxPackage -Path "SideSnap_1.0.0.0_x64.msixbundle"

# View installed packages
Get-AppxPackage | Where-Object {$_.Name -like "*SideSnap*"}
```

---

**Next Steps**:
1. Register Microsoft Partner Center account ($19)
2. Reserve "SideSnap" app name
3. Update Publisher ID in Package.appxmanifest
4. Build release package
5. Run WACK tests
6. Create store listing
7. Submit for certification

Good luck with your submission! 🚀
