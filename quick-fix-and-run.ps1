# 🚀 Quick Fix & Run Script
# Run this in PowerShell from the WebApplication1 directory

Write-Host "===== FINGERPRINT ATTENDANCE SYSTEM - QUICK FIX & RUN =====" -ForegroundColor Cyan
Write-Host ""

# Navigate to project directory
$projectPath = "C:\Users\Lawrence Ramirez\source\repos\RoronoaZoro1206\Fingerprint-Scanner-Attendance\WebApplication1"
Set-Location $projectPath

Write-Host "📂 Project Directory: $projectPath" -ForegroundColor Yellow
Write-Host ""

# Step 1: Clean solution
Write-Host "🧹 Step 1: Cleaning solution..." -ForegroundColor Yellow
dotnet clean
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Clean succeeded" -ForegroundColor Green
} else {
    Write-Host "❌ Clean failed" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 2: Restore packages
Write-Host "📦 Step 2: Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Restore succeeded" -ForegroundColor Green
} else {
    Write-Host "❌ Restore failed" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 3: Build solution
Write-Host "🔨 Step 3: Building solution..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build succeeded - No errors!" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed - Check errors above" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 4: Ask if user wants to run
Write-Host "🎉 BUILD SUCCESSFUL! Ready to run." -ForegroundColor Green
Write-Host ""
$response = Read-Host "Do you want to run the application now? (Y/N)"

if ($response -eq 'Y' -or $response -eq 'y') {
    Write-Host ""
    Write-Host "🚀 Starting application..." -ForegroundColor Cyan
    Write-Host "📍 URL: https://localhost:7048/" -ForegroundColor Yellow
    Write-Host "⚡ Press Ctrl+C to stop" -ForegroundColor Yellow
    Write-Host ""
    
    # Run the application
    dotnet run
} else {
    Write-Host ""
    Write-Host "✅ Build complete. Run 'dotnet run' when ready." -ForegroundColor Green
    Write-Host "📍 URL: https://localhost:7048/" -ForegroundColor Yellow
}
