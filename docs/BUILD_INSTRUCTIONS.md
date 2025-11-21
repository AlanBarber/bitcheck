# BitCheck Build Instructions

## Quick Build (Windows x64)

### Using PowerShell Script
```powershell
.\build-release.ps1
```

Output: `BitCheck\bin\Release\net10.0\win-x64\publish\BitCheck.exe` (~14 MB)

### Manual Command
```powershell
dotnet publish BitCheck -c Release
```

## What You Get

✅ **Single executable file** - No DLL dependencies
✅ **Self-contained** - Includes .NET runtime
✅ **Compressed** - Optimized for size
✅ **Trimmed** - Unused code removed
✅ **Ready-to-run** - Pre-compiled for faster startup

**File size:** ~14 MB (includes entire .NET runtime)

## Build Configuration

The project is configured in `BitCheck.csproj` with these settings:

```xml
<PublishSingleFile>true</PublishSingleFile>              <!-- Bundle into single file -->
<SelfContained>true</SelfContained>                      <!-- Include .NET runtime -->
<RuntimeIdentifier>win-x64</RuntimeIdentifier>           <!-- Target platform -->
<PublishTrimmed>true</PublishTrimmed>                    <!-- Remove unused code -->
<PublishReadyToRun>true</PublishReadyToRun>              <!-- Pre-compile for speed -->
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
```

## Building for Other Platforms

### Windows ARM64
```powershell
dotnet publish BitCheck -c Release -r win-arm64
```

### Linux x64
```powershell
dotnet publish BitCheck -c Release -r linux-x64
```

### Linux ARM64 (Raspberry Pi, etc.)
```powershell
dotnet publish BitCheck -c Release -r linux-arm64
```

### macOS x64 (Intel)
```powershell
dotnet publish BitCheck -c Release -r osx-x64
```

### macOS ARM64 (Apple Silicon)
```powershell
dotnet publish BitCheck -c Release -r osx-arm64
```

### Build All Platforms
```powershell
.\build-all-platforms.ps1
```

Output: `releases\` folder with executables for all platforms

## Output Locations

### Default (Windows x64)
```
BitCheck\bin\Release\net10.0\win-x64\publish\
├── BitCheck.exe    (~14 MB) ← This is what you need
└── BitCheck.pdb    (~13 KB) ← Debug symbols (optional)
```

### Other Platforms
```
BitCheck\bin\Release\net10.0\<runtime-id>\publish\
└── BitCheck or BitCheck.exe
```

## Deployment

### Option 1: Copy Executable
```powershell
# Copy to desired location
Copy-Item "BitCheck\bin\Release\net10.0\win-x64\publish\BitCheck.exe" "C:\Tools\"

# Run from anywhere
C:\Tools\BitCheck.exe --help
```

### Option 2: Add to PATH
```powershell
# Add directory to PATH
$env:PATH += ";C:\Tools"

# Now run from anywhere
BitCheck --help
```

### Option 3: System-Wide Installation
```powershell
# Copy to Windows directory (requires admin)
Copy-Item "BitCheck.exe" "C:\Windows\System32\"
```

## Verification

Test the executable works:

```powershell
# Show help
.\BitCheck.exe --help

# Test in a folder
cd C:\TestFolder
.\BitCheck.exe --add
.\BitCheck.exe --check
```

## Size Optimization

Current size: ~14 MB

### Further Optimization Options

#### 1. Framework-Dependent (Smaller but requires .NET installed)
Remove from `.csproj`:
```xml
<SelfContained>false</SelfContained>
```

Result: ~200 KB (but requires .NET 10.0 installed)

#### 2. Disable Ready-to-Run (Smaller but slower startup)
Remove from `.csproj`:
```xml
<PublishReadyToRun>false</PublishReadyToRun>
```

Result: ~12 MB (slightly slower startup)

#### 3. More Aggressive Trimming
Add to `.csproj`:
```xml
<TrimMode>full</TrimMode>
```

Result: ~10-12 MB (may break reflection-based code)

**Recommendation:** Keep current settings for best balance of size, performance, and compatibility.

## Troubleshooting

### Build Fails with Trimming Warnings

The project includes suppressions for safe JSON serialization. If you see warnings:

```csharp
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "...")]
```

These are safe to suppress for our simple POCO types.

### Executable Won't Run

**Windows SmartScreen:**
- Right-click → Properties → Check "Unblock"
- Or: Run anyway when prompted

**Antivirus:**
- May flag self-contained executables
- Add exception or sign the executable

### Large File Size

14 MB includes:
- Your application code (~1 MB)
- .NET runtime (~10 MB)
- System libraries (~3 MB)

This is normal for self-contained executables. Users don't need to install .NET separately.

## Distribution

### For End Users

Provide just the executable:
```
bitcheck-v1.0.0-win-x64.exe
```

No installation needed. Just run it.

### With Documentation

```
bitcheck-v1.0.0-win-x64.zip
├── BitCheck.exe
├── README.md
├── QUICKSTART.md
└── LICENSE.txt
```

### GitHub Release

1. Build all platforms
2. Create release on GitHub
3. Attach executables:
   - `bitcheck-v1.0.0-win-x64.exe`
   - `bitcheck-v1.0.0-linux-x64`
   - `bitcheck-v1.0.0-osx-arm64`
   - etc.

## Continuous Integration

### GitHub Actions Example

```yaml
name: Build Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - run: dotnet publish -c Release -r win-x64
      - uses: actions/upload-artifact@v3
        with:
          name: bitcheck-win-x64
          path: BitCheck/bin/Release/net10.0/win-x64/publish/BitCheck.exe
```

## Performance Notes

### Self-Contained vs Framework-Dependent

| Aspect | Self-Contained | Framework-Dependent |
|--------|----------------|---------------------|
| Size | ~14 MB | ~200 KB |
| Startup | Fast (R2R) | Fast |
| Deployment | Copy & run | Requires .NET 10.0 |
| Updates | Redeploy all | Runtime updates separate |

**Recommendation:** Self-contained for maximum compatibility.

### Trimming Impact

- **Build time:** +2-3 seconds
- **Size reduction:** ~30%
- **Runtime:** No impact
- **Compatibility:** Safe for this application

## Advanced Options

### Code Signing (Windows)

```powershell
# Sign the executable
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com BitCheck.exe
```

### Compression

```powershell
# Create compressed archive
Compress-Archive -Path BitCheck.exe -DestinationPath bitcheck-v1.0.0-win-x64.zip
```

### Portable Package

```powershell
# Create portable package with docs
New-Item -ItemType Directory -Path "bitcheck-portable"
Copy-Item "BitCheck.exe" "bitcheck-portable\"
Copy-Item "README.md" "bitcheck-portable\"
Copy-Item "QUICKSTART.md" "bitcheck-portable\"
Compress-Archive -Path "bitcheck-portable" -DestinationPath "bitcheck-v1.0.0-portable.zip"
```

## Summary

✅ **Single command:** `.\build-release.ps1`
✅ **Single file:** `BitCheck.exe` (~14 MB)
✅ **No dependencies:** Everything included
✅ **Cross-platform:** Build for Windows, Linux, macOS
✅ **Production ready:** Optimized and tested

The executable is ready to distribute and run on any Windows x64 machine without requiring .NET installation.
