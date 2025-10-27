# Quick Test All Controllers
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Testing All Fingerprint Controllers" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if app is running
$process = Get-Process -Name "Fingerprint_Attendance_System" -ErrorAction SilentlyContinue
if (-not $process) {
    Write-Host "??  Application is not running!" -ForegroundColor Yellow
    Write-Host "Please start the application first:" -ForegroundColor Yellow
    Write-Host "  cd WebApplication1" -ForegroundColor White
    Write-Host "  dotnet run" -ForegroundColor White
    Write-Host ""
    exit
}

Write-Host "? Application is running" -ForegroundColor Green
Write-Host ""

# Test each controller
$controllers = @(
  @{
        Name = "Admin"
        URL = "https://localhost:7048/Admin/AdminRegisterFingerprint/GetDeviceStatus"
   Role = "Administrator"
    },
    @{
        Name = "Faculty"
        URL = "https://localhost:7048/Faculty/FacultyRegisterFingerprint/GetDeviceStatus"
        Role = "Faculty Member"
    },
    @{
        Name = "Student"
   URL = "https://localhost:7048/Student/StudentRegisterFingerpront/GetDeviceStatus"
        Role = "Student"
    }
)

foreach ($controller in $controllers) {
    Write-Host "Testing $($controller.Name) Controller..." -ForegroundColor Yellow
    
    try {
        # Skip SSL certificate validation for localhost testing
        [Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
        
        $response = Invoke-WebRequest -Uri $controller.URL -Method GET -UseBasicParsing -ErrorAction Stop
        
        if ($response.StatusCode -eq 200) {
    $json = $response.Content | ConvertFrom-Json
 
            Write-Host "  ? Status Code: 200 OK" -ForegroundColor Green
    Write-Host "  ? Response: Valid JSON" -ForegroundColor Green
     Write-Host "  ?? Device Status: $($json.status)" -ForegroundColor Cyan
            
    if ($json.isDeviceConnected) {
            Write-Host "  ?? Device: Connected" -ForegroundColor Green
        } else {
        Write-Host "  ?? Device: Not Connected (SDK may need initialization)" -ForegroundColor Yellow
    }
        }
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 404) {
            Write-Host "  ? Status Code: 404 Not Found" -ForegroundColor Red
    Write-Host "  ? Route not configured properly!" -ForegroundColor Red
        }
    elseif ($_.Exception.Response.StatusCode -eq 401) {
  Write-Host "  ??  Status Code: 401 Unauthorized" -ForegroundColor Yellow
        Write-Host "  ??  This is normal - you need to login as $($controller.Role)" -ForegroundColor White
 }
        else {
            Write-Host "  ? Error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Test Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. If all controllers show 200 OK ? Routes are working! ?" -ForegroundColor White
Write-Host "2. If you see 401 Unauthorized ? Login first" -ForegroundColor White
Write-Host "3. If you see 404 Not Found ? Check CONTROLLERS_FIXED_VERIFICATION.md" -ForegroundColor White
Write-Host "4. Open browser and test manually:" -ForegroundColor White
Write-Host "- Admin: https://localhost:7048/Admin/AdminRegisterFingerprint" -ForegroundColor Gray
Write-Host "   - Faculty: https://localhost:7048/Faculty/FacultyRegisterFingerprint" -ForegroundColor Gray
Write-Host "   - Student: https://localhost:7048/Student/StudentRegisterFingerpront" -ForegroundColor Gray
Write-Host ""
