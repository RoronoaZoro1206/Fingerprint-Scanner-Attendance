# Quick Fix Script for Scanner Detection
# Run this as Administrator

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " ZKTeco Scanner Quick Fix" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
 Write-Host "??  This script needs to run as Administrator!" -ForegroundColor Yellow
    Write-Host "Please right-click and select 'Run as Administrator'" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit
}

Write-Host "? Running as Administrator" -ForegroundColor Green
Write-Host ""

# Step 1: Rescan for hardware changes
Write-Host "Step 1: Rescanning for hardware changes..." -ForegroundColor Yellow
try {
    pnputil /scan-devices
    Write-Host "? Hardware scan complete" -ForegroundColor Green
} catch {
    Write-Host "? Failed to scan for devices" -ForegroundColor Red
}
Write-Host ""

# Step 2: Check for ZKTeco device
Write-Host "Step 2: Looking for ZKTeco device..." -ForegroundColor Yellow
$devices = Get-PnpDevice | Where-Object { 
$_.FriendlyName -like "*ZK*" -or 
    $_.FriendlyName -like "*Finger*" -or
    $_.FriendlyName -like "*1B55*"  # ZKTeco VID
}

if ($devices) {
    Write-Host "? Found device(s):" -ForegroundColor Green
    foreach ($device in $devices) {
      $status = if ($device.Status -eq "OK") { "?" } else { "??" }
        Write-Host "  $status $($device.FriendlyName) [$($device.Status)]" -ForegroundColor White
        
        if ($device.Status -ne "OK") {
 Write-Host "     Attempting to restart device..." -ForegroundColor Yellow
            try {
       Disable-PnpDevice -InstanceId $device.InstanceId -Confirm:$false
        Start-Sleep -Seconds 2
    Enable-PnpDevice -InstanceId $device.InstanceId -Confirm:$false
    Write-Host "     ? Device restarted" -ForegroundColor Green
   } catch {
    Write-Host "     ? Failed to restart device" -ForegroundColor Red
          }
        }
    }
} else {
    Write-Host "??  No ZKTeco device found" -ForegroundColor Yellow
    Write-Host ""
 Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
    Write-Host "1. Unplug the scanner" -ForegroundColor White
 Write-Host "2. Wait 5 seconds" -ForegroundColor White
    Write-Host "3. Plug into a different USB port" -ForegroundColor White
    Write-Host "4. Run this script again" -ForegroundColor White
}

Write-Host ""

# Step 3: Check DLL files
Write-Host "Step 3: Verifying DLL files..." -ForegroundColor Yellow
$dlls = @(
    "C:\Windows\System32\libzkfp.dll",
    "C:\Windows\System32\ZKFPCap.dll",
    "C:\Windows\System32\libzkfpcsharp.dll"
)

$allDllsPresent = $true
foreach ($dll in $dlls) {
    if (Test-Path $dll) {
        Write-Host "  ? $(Split-Path $dll -Leaf)" -ForegroundColor Green
    } else {
        Write-Host "  ? $(Split-Path $dll -Leaf) - MISSING!" -ForegroundColor Red
$allDllsPresent = $false
    }
}

if (-not $allDllsPresent) {
    Write-Host ""
    Write-Host "??  Some DLL files are missing!" -ForegroundColor Yellow
    Write-Host "Please reinstall the ZKTeco SDK" -ForegroundColor Yellow
}

Write-Host ""

# Step 4: Check for conflicting services
Write-Host "Step 4: Checking for conflicts..." -ForegroundColor Yellow
$bioServices = Get-Service | Where-Object { $_.Name -like "*bio*" -or $_.Name -like "*finger*" }

if ($bioServices) {
    foreach ($service in $bioServices) {
        $status = if ($service.Status -eq "Running") { "??" } else { "?" }
        Write-Host "  $status $($service.DisplayName) [$($service.Status)]" -ForegroundColor White
    }
    
    Write-Host ""
    Write-Host "Note: Running biometric services may conflict with the scanner" -ForegroundColor Yellow
} else {
    Write-Host "  ? No conflicting services found" -ForegroundColor Green
}

Write-Host ""

# Step 5: Test with demo application
Write-Host "Step 5: Recommendations..." -ForegroundColor Yellow
Write-Host "  1. Test with SDK demo application first" -ForegroundColor White
Write-Host "     Location: Downloads\SDK WINDOWS\...\C#\Demo2" -ForegroundColor Gray
Write-Host ""
Write-Host "2. If demo works, run your application:" -ForegroundColor White
Write-Host "     cd WebApplication1" -ForegroundColor Gray
Write-Host "     dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Navigate to:" -ForegroundColor White
Write-Host "     https://localhost:7048/Admin/AdminRegisterFingerprint" -ForegroundColor Gray
Write-Host ""

# Final status
Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Fix Attempt Complete" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$openDevMgr = Read-Host "Open Device Manager to check device status? (Y/N)"
if ($openDevMgr -eq "Y" -or $openDevMgr -eq "y") {
    Start-Process devmgmt.msc
}

Write-Host ""
Write-Host "If scanner still doesn't work:" -ForegroundColor Yellow
Write-Host "1. Check Device Manager for yellow warning icons" -ForegroundColor White
Write-Host "2. Update/reinstall drivers" -ForegroundColor White
Write-Host "3. Try a different USB port" -ForegroundColor White
Write-Host "4. Restart your computer" -ForegroundColor White
Write-Host ""

Read-Host "Press Enter to exit"
