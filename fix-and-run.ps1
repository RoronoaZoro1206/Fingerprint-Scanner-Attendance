# Fix Build and Run Script
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Fix 'Unable to start program' Error" -ForegroundColor Cyan
Write-Host "================================================`n" -ForegroundColor Cyan

$projectDir = "C:\Users\Lawrence Ramirez\source\repos\RoronoaZoro1206\Fingerprint-Scanner-Attendance\WebApplication1"
$exePath = Join-Path $projectDir "bin\Debug\net9.0\Fingerprint_Attendance_System.exe"

# Step 1: Clean
Write-Host "🔧 Step 1: Cleaning solution..." -ForegroundColor Yellow
Set-Location $projectDir
dotnet clean
Write-Host "✅ Clean completed`n" -ForegroundColor Green

# Step 2: Restore (in case dependencies are missing)
Write-Host "📦 Step 2: Restoring dependencies..." -ForegroundColor Yellow
dotnet restore
Write-Host "✅ Restore completed`n" -ForegroundColor Green

# Step 3: Build
Write-Host "🔨 Step 3: Building solution..." -ForegroundColor Yellow
dotnet build --configuration Debug

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build successful!`n" -ForegroundColor Green
    
    # Step 4: Verify executable exists
    Write-Host "🔍 Step 4: Verifying executable..." -ForegroundColor Yellow
    if (Test-Path $exePath) {
        Write-Host "✅ Executable found: $exePath`n" -ForegroundColor Green
        
        # List key output files
        Write-Host "📁 Output files:" -ForegroundColor Cyan
        Get-ChildItem "bin\Debug\net9.0\" -Filter "*.exe" | ForEach-Object {
            Write-Host "  ✅ $($_.Name) ($($_.Length) bytes)" -ForegroundColor White
        }
        Get-ChildItem "bin\Debug\net9.0\" -Filter "*.dll" | Where-Object { $_.Name -like "Fingerprint*" -or $_.Name -like "libzkfp*" } | ForEach-Object {
            Write-Host "  ✅ $($_.Name) ($($_.Length) bytes)" -ForegroundColor White
        }
        
        Write-Host "`n================================================" -ForegroundColor Cyan
        Write-Host "  ✅ BUILD SUCCESSFUL - Ready to Run!" -ForegroundColor Green
        Write-Host "================================================`n" -ForegroundColor Cyan
        
        Write-Host "Choose an option:" -ForegroundColor Yellow
        Write-Host "  1. Run with dotnet run (recommended)" -ForegroundColor White
        Write-Host "  2. Open in Visual Studio and press F5" -ForegroundColor White
        Write-Host "  3. Exit`n" -ForegroundColor White
        
        $choice = Read-Host "Enter choice (1-3)"
        
        switch ($choice) {
            "1" {
                Write-Host "`n🚀 Starting application with dotnet run..." -ForegroundColor Yellow
                Write-Host "Press Ctrl+C to stop`n" -ForegroundColor Gray
                dotnet run
            }
            "2" {
                Write-Host "`n✅ Open Visual Studio and press F5 to run" -ForegroundColor Green
                Write-Host "The executable is now available and F5 should work!`n" -ForegroundColor White
            }
            "3" {
                Write-Host "`n👋 Exiting...`n" -ForegroundColor Yellow
            }
            default {
                Write-Host "`n❌ Invalid choice. Exiting...`n" -ForegroundColor Red
            }
        }
        
    } else {
        Write-Host "❌ ERROR: Executable not found!" -ForegroundColor Red
        Write-Host "Expected: $exePath" -ForegroundColor Yellow
        Write-Host "`nPossible causes:" -ForegroundColor Yellow
        Write-Host "  1. Build succeeded but output path is wrong" -ForegroundColor White
        Write-Host "  2. Antivirus blocked the .exe creation" -ForegroundColor White
        Write-Host "  3. Disk space full" -ForegroundColor White
        Write-Host "`nTry running in Visual Studio instead (Build → Rebuild Solution → F5)`n" -ForegroundColor White
    }
} else {
    Write-Host "`n❌ BUILD FAILED!`n" -ForegroundColor Red
    
    Write-Host "Common issues and fixes:" -ForegroundColor Yellow
    Write-Host "`n1. Missing dependencies:" -ForegroundColor Cyan
    Write-Host "   Fix: dotnet restore" -ForegroundColor White
    
    Write-Host "`n2. Compilation errors:" -ForegroundColor Cyan
    Write-Host "   Fix: Check Error List in Visual Studio (View → Error List)" -ForegroundColor White
    Write-Host "        Look for errors starting with 'CS' (ignore SQL warnings)" -ForegroundColor White
    
    Write-Host "`n3. Corrupted project:" -ForegroundColor Cyan
    Write-Host "   Fix: Delete bin/ and obj/ folders, then rebuild" -ForegroundColor White
    
    Write-Host "`n4. .NET SDK issue:" -ForegroundColor Cyan
    Write-Host "   Check: dotnet --version (should be 9.0.x)" -ForegroundColor White
    
    Write-Host "`nFor detailed diagnostics, open Visual Studio and check:" -ForegroundColor Yellow
    Write-Host "  - View → Output (select 'Build' from dropdown)" -ForegroundColor White
    Write-Host "  - View → Error List`n" -ForegroundColor White
}

Write-Host "================================================`n" -ForegroundColor Cyan

