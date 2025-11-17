Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  HOME SCANNER 'NO CLASS SCHEDULED' FIX - BUILD & TEST" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Navigate to project directory
$projectPath = "C:\Users\Lawrence Ramirez\source\repos\RoronoaZoro1206\Fingerprint-Scanner-Attendance\WebApplication1"
Set-Location $projectPath

Write-Host "📁 Project Directory: $projectPath" -ForegroundColor Yellow
Write-Host ""

# Step 1: Clean build
Write-Host "🧹 Step 1: Cleaning previous build..." -ForegroundColor Yellow
dotnet clean --nologo --verbosity quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Clean complete" -ForegroundColor Green
} else {
    Write-Host "   ❌ Clean failed!" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 2: Build project
Write-Host "🔨 Step 2: Building project..." -ForegroundColor Yellow
$buildOutput = dotnet build --nologo --verbosity minimal 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Build successful" -ForegroundColor Green
} else {
    Write-Host "   ❌ Build failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Build Output:" -ForegroundColor Red
    Write-Host $buildOutput
    exit 1
}
Write-Host ""

# Step 3: Display test instructions
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  ✅ BUILD COMPLETE - READY TO TEST" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "🚀 Next Steps:" -ForegroundColor Yellow
Write-Host ""
Write-Host "   1. Run the application:" -ForegroundColor White
Write-Host "      dotnet run" -ForegroundColor Cyan
Write-Host ""
Write-Host "   2. Open browser:" -ForegroundColor White
Write-Host "      https://localhost:7048/" -ForegroundColor Cyan
Write-Host ""
Write-Host "   3. Test 'No Class Scheduled' error:" -ForegroundColor White
Write-Host "      - Scan a student fingerprint when no class is scheduled" -ForegroundColor Gray
Write-Host "      - Verify alert shows:" -ForegroundColor Gray
Write-Host "        ✅ Full student name: 'Hello, [First] [Middle] [Last]'" -ForegroundColor Gray
Write-Host "        ✅ Day of week: e.g., 'Monday'" -ForegroundColor Gray
Write-Host "        ✅ Time: e.g., '9:27 am'" -ForegroundColor Gray
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  🔍 VISUAL CHECKS (Critical)" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "  Background Color Timing:" -ForegroundColor White
Write-Host ""
Write-Host "  Before Scan:  ┌─────────────────────────────┐" -ForegroundColor White
Write-Host "                │ Scanner ready...            │ ← GREEN" -ForegroundColor Green
Write-Host "                └─────────────────────────────┘" -ForegroundColor White
Write-Host ""
Write-Host "  Alert Open:   ┌─────────────────────────────┐" -ForegroundColor White
Write-Host "                │ Scanner ready...            │ ← WHITE/GRAY" -ForegroundColor Gray
Write-Host "                └─────────────────────────────┘" -ForegroundColor White
Write-Host "                    ↓ (Behind SweetAlert2 modal)" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  After OK:     ┌─────────────────────────────┐" -ForegroundColor White
Write-Host "                │ Scanner ready...            │ ← GREEN" -ForegroundColor Green
Write-Host "                └─────────────────────────────┘" -ForegroundColor White
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  ✅ Expected Alert Content" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "  ┌──────────────────────────────────────────────────┐" -ForegroundColor White
Write-Host "  │       ❌ Cannot Record Attendance                │" -ForegroundColor Red
Write-Host "  │                                                  │" -ForegroundColor White
Write-Host "  │  📅 No class scheduled at this time.            │" -ForegroundColor White
Write-Host "  │                                                  │" -ForegroundColor White
Write-Host "  │  Hello, Cristen Ungab Reyes                     │" -ForegroundColor Green
Write-Host "  │                                                  │" -ForegroundColor White
Write-Host "  │  You have no subject/course assigned for        │" -ForegroundColor White
Write-Host "  │  Monday at 9:27 am.                             │" -ForegroundColor Green
Write-Host "  │                                                  │" -ForegroundColor White
Write-Host "  │                      [OK]                        │" -ForegroundColor White
Write-Host "  └──────────────────────────────────────────────────┘" -ForegroundColor White
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  📝 Pass/Fail Checklist" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "  [ ] Student full name displayed (not just 'Hello')" -ForegroundColor White
Write-Host "  [ ] Day of week shown in message" -ForegroundColor White
Write-Host "  [ ] Time of scan shown in message" -ForegroundColor White
Write-Host "  [ ] Background is WHITE while alert is open" -ForegroundColor White
Write-Host "  [ ] Background returns to GREEN after clicking OK" -ForegroundColor White
Write-Host "  [ ] No console errors (F12 → Console)" -ForegroundColor White
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  📄 Fix Documentation" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "  📖 Complete Guide: HOME_SCANNER_NO_SCHEDULE_FIX_COMPLETE.md" -ForegroundColor White
Write-Host "  🎯 Quick Test:     QUICK_VISUAL_TEST_NO_SCHEDULE_FIX.md" -ForegroundColor White
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to start the application..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
Write-Host ""

Write-Host "🚀 Starting application..." -ForegroundColor Green
Write-Host ""
dotnet run

