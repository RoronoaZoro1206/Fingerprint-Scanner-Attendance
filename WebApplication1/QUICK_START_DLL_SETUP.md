# Quick Start: Copy DLL Files

## ?? IMPORTANT: Copy DLL Files Before Running

Based on your screenshots, I can see you have the DLL files in your project root. You need to ensure they're in the correct location.

### Step 1: Locate Your DLL Files

From your screenshots, the DLLs are at:
```
C:\Users\Lawrence Ramirez\source\repos\RoronoaZoro1206\Fingerprint-Scanner-Attendance\WebApplication1\
```

### Step 2: Verify DLL Files Exist

Run this PowerShell command to check:

```powershell
Get-ChildItem "C:\Users\Lawrence Ramirez\source\repos\RoronoaZoro1206\Fingerprint-Scanner-Attendance\WebApplication1" -Filter "*.dll" | Select-Object Name, FullName
```

You should see:
- ? libzkfp.dll
- ? libzkfpcsharp.dll
- ? ZKFPCap.dll
- ? zkfpslibLow.dll

### Step 3: Build and Run

The updated `.csproj` file will automatically copy these DLLs to the output directory when you build.

```bash
# Clean the solution
dotnet clean

# Build the solution
dotnet build

# Run the application
dotnet run
```

### Step 4: Verify DLLs Were Copied

After building, check:
```
WebApplication1\bin\Debug\net9.0\
```

All 4 DLL files should be there.

### Step 5: Test the Scanner

1. Start the application
2. Navigate to `/Admin/AdminRegisterFingerprint`
3. You should see "Scanner Status: Online" (if device is connected)

## ?? Troubleshooting

### If DLLs Are Not Being Copied:

**Option 1: Copy Manually**
```powershell
# Copy to Debug folder
Copy-Item "C:\Users\Lawrence Ramirez\source\repos\RoronoaZoro1206\Fingerprint-Scanner-Attendance\WebApplication1\*.dll" `
  -Destination "C:\Users\Lawrence Ramirez\source\repos\RoronoaZoro1206\Fingerprint-Scanner-Attendance\WebApplication1\bin\Debug\net9.0\" `
  -Force
```

**Option 2: Add Post-Build Event**

Add this to your `.csproj` file:
```xml
<Target Name="CopyDLLs" AfterTargets="Build">
  <Copy SourceFiles="$(ProjectDir)libzkfp.dll" DestinationFolder="$(OutDir)" Condition="Exists('$(ProjectDir)libzkfp.dll')" />
  <Copy SourceFiles="$(ProjectDir)ZKFPCap.dll" DestinationFolder="$(OutDir)" Condition="Exists('$(ProjectDir)ZKFPCap.dll')" />
  <Copy SourceFiles="$(ProjectDir)zkfpslibLow.dll" DestinationFolder="$(OutDir)" Condition="Exists('$(ProjectDir)zkfpslibLow.dll')" />
  <Copy SourceFiles="$(ProjectDir)libzkfpcsharp.dll" DestinationFolder="$(OutDir)" Condition="Exists('$(ProjectDir)libzkfpcsharp.dll')" />
</Target>
```

**Option 3: Create DLL Folder**

If DLLs are in a subfolder:
```
WebApplication1/
  ??? DLL/
  ?   ??? libzkfp.dll
  ?   ??? ZKFPCap.dll
  ?   ??? zkfpslibLow.dll
  ?   ??? libzkfpcsharp.dll
```

Update `.csproj`:
```xml
<ItemGroup>
  <None Include="DLL\*.dll">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### If Scanner Still Shows "Offline":

1. **Check Device Connection**
   - Open Device Manager (Win + X ? Device Manager)
   - Look for "ZKTeco" or "USB Device (VID_1B55 PID_0120)"
   - If you see a yellow warning, reinstall drivers

2. **Check Application Logs**
   - View Visual Studio Output window
   - Look for initialization errors
   - Check for "No fingerprint devices found"

3. **Test with Diagnostic**
   Create a simple test page:
   ```csharp
   var service = new FingerprintService(logger);
   var initialized = service.Initialize();
   var deviceOpen = service.TurnOnDevice();
 ```

### Visual Studio Build Steps:

1. **Clean Solution**: Build ? Clean Solution
2. **Rebuild Solution**: Build ? Rebuild Solution
3. **Check Output**: View ? Output ? Show output from: Build
4. **Verify DLLs**: Look for "1 file(s) copied" messages

## ?? Expected Results

After successful setup:

1. **Build Output**:
   ```
   Build succeeded.
   0 Warning(s)
   0 Error(s)
   ```

2. **DLL Location**:
   ```
   WebApplication1\bin\Debug\net9.0\libzkfp.dll
   WebApplication1\bin\Debug\net9.0\ZKFPCap.dll
   WebApplication1\bin\Debug\net9.0\zkfpslibLow.dll
   WebApplication1\bin\Debug\net9.0\libzkfpcsharp.dll (optional)
   ```

3. **Scanner Status**:
   - Page loads without errors
   - "Scanner Status: Online" appears
   - "Registration Status: Not Registered" shows

4. **Scan Test**:
   - Click "Scan Fingerprint"
   - Green LED lights up on scanner
   - Beep sound confirms capture
   - "Fingerprint captured successfully!" message

## ?? Quick Test Script

Run this in PowerShell to test everything:

```powershell
# Navigate to project directory
cd "C:\Users\Lawrence Ramirez\source\repos\RoronoaZoro1206\Fingerprint-Scanner-Attendance\WebApplication1"

# Check if DLLs exist in root
Write-Host "Checking DLL files..." -ForegroundColor Yellow
$dlls = @("libzkfp.dll", "ZKFPCap.dll", "zkfpslibLow.dll", "libzkfpcsharp.dll")
foreach ($dll in $dlls) {
    if (Test-Path $dll) {
        Write-Host "? Found: $dll" -ForegroundColor Green
    } else {
        Write-Host "? Missing: $dll" -ForegroundColor Red
    }
}

# Clean and rebuild
Write-Host "`nCleaning solution..." -ForegroundColor Yellow
dotnet clean

Write-Host "`nBuilding solution..." -ForegroundColor Yellow
dotnet build

# Check if DLLs were copied to output
Write-Host "`nChecking output directory..." -ForegroundColor Yellow
$outputPath = "bin\Debug\net9.0"
foreach ($dll in $dlls) {
    $fullPath = Join-Path $outputPath $dll
    if (Test-Path $fullPath) {
Write-Host "? Copied: $dll" -ForegroundColor Green
    } else {
        Write-Host "?? Not copied: $dll" -ForegroundColor Yellow
 }
}

Write-Host "`nReady to run!" -ForegroundColor Green
Write-Host "Execute: dotnet run" -ForegroundColor Cyan
```

Save this as `test-setup.ps1` and run it.

## ? Final Checklist

Before testing:
- [ ] All 4 DLL files exist in project root
- [ ] Project builds without errors
- [ ] DLLs are in bin\Debug\net9.0 folder
- [ ] Scanner is plugged in via USB
- [ ] Device Manager shows ZKTeco device
- [ ] No other applications are using the scanner

Ready to test! ??
