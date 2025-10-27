# Scanner Diagnostic Script
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ZKTeco Fingerprint Scanner Diagnostic" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if DLL exists in project
$projectPath = "C:\Users\Lawrence Ramirez\source\repos\RoronoaZoro1206\Fingerprint-Scanner-Attendance\WebApplication1"
$outputPath = "$projectPath\bin\Debug\net9.0"

Write-Host "Checking DLL files..." -ForegroundColor Yellow
Write-Host ""

$dlls = @{
    "libzkfpcsharp.dll" = @{
        "Project" = "$projectPath\libzkfpcsharp.dll"
  "Output" = "$outputPath\libzkfpcsharp.dll"
        "System32" = "C:\Windows\System32\libzkfpcsharp.dll"
    }
    "libzkfp.dll" = @{
      "System32" = "C:\Windows\System32\libzkfp.dll"
    }
    "ZKFPCap.dll" = @{
        "System32" = "C:\Windows\System32\ZKFPCap.dll"
    }
}

foreach ($dll in $dlls.Keys) {
    Write-Host "Checking $dll..." -ForegroundColor White
    foreach ($location in $dlls[$dll].Keys) {
        $path = $dlls[$dll][$location]
        if (Test-Path $path) {
      $file = Get-Item $path
            Write-Host "  ? $location : Found ($($file.Length) bytes)" -ForegroundColor Green
     } else {
          Write-Host "  ? $location : Not found" -ForegroundColor Red
        }
    }
    Write-Host ""
}

# Check Device Manager for ZKTeco device
Write-Host "Checking for ZKTeco device..." -ForegroundColor Yellow
$devices = Get-PnpDevice | Where-Object { $_.FriendlyName -like "*ZK*" -or $_.FriendlyName -like "*Finger*" }

if ($devices) {
    Write-Host "? Found devices:" -ForegroundColor Green
    foreach ($device in $devices) {
        Write-Host "  - $($device.FriendlyName) [$($device.Status)]" -ForegroundColor White
  }
} else {
    Write-Host "??  No ZKTeco or fingerprint devices found in Device Manager" -ForegroundColor Yellow
    Write-Host "  Checking USB devices..." -ForegroundColor Yellow
    
    $usbDevices = Get-PnpDevice | Where-Object { $_.Class -eq "USB" -and $_.Status -eq "OK" }
    Write-Host "  Found $($usbDevices.Count) active USB devices" -ForegroundColor White
}

Write-Host ""

# Check if application is running
Write-Host "Checking if application is running..." -ForegroundColor Yellow
$process = Get-Process | Where-Object { $_.ProcessName -like "*Fingerprint*" }
if ($process) {
    Write-Host "? Application is running (PID: $($process.Id))" -ForegroundColor Green
} else {
    Write-Host "? Application is not running" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Diagnostic Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Ensure the fingerprint scanner is plugged in via USB" -ForegroundColor White
Write-Host "2. Check Device Manager for any driver issues" -ForegroundColor White
Write-Host "3. Run the application with 'dotnet run'" -ForegroundColor White
Write-Host "4. Navigate to /Admin/AdminRegisterFingerprint" -ForegroundColor White
Write-Host "5. Check browser console (F12) for JavaScript errors" -ForegroundColor White
Write-Host "6. Check Visual Studio Output window for C# errors" -ForegroundColor White
Write-Host ""

# Offer to open Device Manager
$openDevMgr = Read-Host "Would you like to open Device Manager? (Y/N)"
if ($openDevMgr -eq "Y" -or $openDevMgr -eq "y") {
    Start-Process devmgmt.msc
}
