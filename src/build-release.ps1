# BitCheck Release Build Script
# Creates a self-contained single-file executable

Write-Host "Building BitCheck Release..." -ForegroundColor Cyan

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "BitCheck\bin\Release") {
    Remove-Item "BitCheck\bin\Release" -Recurse -Force
}

# Build for Windows x64
Write-Host "`nBuilding for Windows x64..." -ForegroundColor Yellow
dotnet publish BitCheck -c Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild successful!" -ForegroundColor Green
    
    $exePath = "BitCheck\bin\Release\net10.0\win-x64\publish\BitCheck.exe"
    $fileSize = (Get-Item $exePath).Length / 1MB
    
    Write-Host "`nOutput:" -ForegroundColor Cyan
    Write-Host "  Location: $exePath" -ForegroundColor White
    Write-Host "  Size: $([math]::Round($fileSize, 2)) MB" -ForegroundColor White
    
    # Test the executable
    Write-Host "`nTesting executable..." -ForegroundColor Yellow
    & $exePath --help
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nExecutable is working correctly!" -ForegroundColor Green
    }
    
    Write-Host "`nTo use the executable:" -ForegroundColor Cyan
    Write-Host "  1. Copy BitCheck.exe to your desired location" -ForegroundColor White
    Write-Host "  2. Add to PATH or run directly" -ForegroundColor White
    Write-Host "  3. Run: BitCheck.exe --add --recursive" -ForegroundColor White
    
} else {
    Write-Host "`nBuild failed!" -ForegroundColor Red
    exit 1
}
