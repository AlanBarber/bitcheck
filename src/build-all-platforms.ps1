# BitCheck Multi-Platform Release Build Script
# Creates self-contained single-file executables for multiple platforms

Write-Host "Building BitCheck for Multiple Platforms..." -ForegroundColor Cyan

# Clean previous builds
Write-Host "`nCleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "BitCheck\bin\Release") {
    Remove-Item "BitCheck\bin\Release" -Recurse -Force
}
if (Test-Path "releases") {
    Remove-Item "releases" -Recurse -Force
}
New-Item -ItemType Directory -Path "releases" | Out-Null

$platforms = @(
    @{Name="Windows x64"; RID="win-x64"; Ext=".exe"},
    @{Name="Windows ARM64"; RID="win-arm64"; Ext=".exe"},
    @{Name="Linux x64"; RID="linux-x64"; Ext=""},
    @{Name="Linux ARM64"; RID="linux-arm64"; Ext=""},
    @{Name="macOS x64"; RID="osx-x64"; Ext=""},
    @{Name="macOS ARM64"; RID="osx-arm64"; Ext=""}
)

$successful = @()
$failed = @()

foreach ($platform in $platforms) {
    Write-Host "`nBuilding for $($platform.Name)..." -ForegroundColor Yellow
    
    # Temporarily update RuntimeIdentifier in project file
    $csprojPath = "BitCheck\BitCheck.csproj"
    $csprojContent = Get-Content $csprojPath -Raw
    $csprojContent = $csprojContent -replace '<RuntimeIdentifier>.*?</RuntimeIdentifier>', "<RuntimeIdentifier>$($platform.RID)</RuntimeIdentifier>"
    Set-Content $csprojPath $csprojContent
    
    # Build
    dotnet publish BitCheck -c Release -r $platform.RID 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        $exeName = "BitCheck$($platform.Ext)"
        $sourcePath = "BitCheck\bin\Release\net10.0\$($platform.RID)\publish\$exeName"
        $destPath = "releases\bitcheck-$($platform.RID)$($platform.Ext)"
        
        if (Test-Path $sourcePath) {
            Copy-Item $sourcePath $destPath
            $fileSize = (Get-Item $destPath).Length / 1MB
            Write-Host "  Success! Size: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Green
            $successful += $platform.Name
        } else {
            Write-Host "  Failed - executable not found" -ForegroundColor Red
            $failed += $platform.Name
        }
    } else {
        Write-Host "  Failed - build error" -ForegroundColor Red
        $failed += $platform.Name
    }
}

# Restore original RuntimeIdentifier
$csprojContent = $csprojContent -replace '<RuntimeIdentifier>.*?</RuntimeIdentifier>', '<RuntimeIdentifier>win-x64</RuntimeIdentifier>'
Set-Content $csprojPath $csprojContent

# Summary
Write-Host "`n=== Build Summary ===" -ForegroundColor Cyan
Write-Host "Successful: $($successful.Count)" -ForegroundColor Green
foreach ($name in $successful) {
    Write-Host "  ✓ $name" -ForegroundColor Green
}

if ($failed.Count -gt 0) {
    Write-Host "`nFailed: $($failed.Count)" -ForegroundColor Red
    foreach ($name in $failed) {
        Write-Host "  ✗ $name" -ForegroundColor Red
    }
}

Write-Host "`nOutput directory: releases\" -ForegroundColor Cyan
Get-ChildItem "releases" | Select-Object Name, @{Name="Size (MB)";Expression={[math]::Round($_.Length/1MB, 2)}} | Format-Table -AutoSize
