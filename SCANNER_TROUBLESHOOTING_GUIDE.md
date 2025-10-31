# ?? Scanner Troubleshooting Quick Reference

**Use this guide when scanner issues occur after the stability fix.**

---

## ?? Common Issues & Solutions

### **Issue 1: "Failed to capture fingerprint" every time**

**Possible Causes:**
- No finger on scanner
- Scanner USB disconnected
- Finger too dry or dirty
- Scanner surface dirty

**Solutions:**
1. ? Verify finger is placed **flat** on scanner
2. ? Check USB cable is securely connected
3. ? Clean scanner surface with soft cloth
4. ? Slightly moisten finger if very dry
5. ? Check Device Manager for "ZKTeco" or "Fingerprint" device
6. ? Check Visual Studio Output logs for specific error codes

**Log to Look For:**
```
[Warning] ? Capture attempt 1 failed: No finger detected on scanner (Error code: -8)
```

---

### **Issue 2: Scanner works sometimes but not consistently**

**Possible Causes:**
- Finger placement varies
- Loose USB connection
- Scanner hardware issue

**Solutions:**
1. ? Use the **same finger** for all tests
2. ? Place finger in the **same position** each time
3. ? Try a **different USB port**
4. ? Check if USB cable is damaged
5. ? Test scanner with ZKTeco's official software

**Log to Look For:**
```
[Warning] Template size suspiciously small (45 bytes). Retrying...
```
*(Indicates poor finger contact)*

---

### **Issue 3: "Scanner busy" errors**

**Possible Causes:**
- Previous scan didn't complete
- Device handle is stale

**Solutions:**
1. ? Wait 2-3 seconds between scans
2. ? Check logs for automatic reset attempts
3. ? Restart the application if issue persists

**Log to Look For:**
```
[Warning] ? Capture attempt 1 failed: Scanner is busy (Error code: -12)
[Information] Scanner busy. Waiting for device to be ready...
```

**What the Fix Does:**
- Automatically waits 500ms when scanner is busy
- Retries capture after wait

---

### **Issue 4: "Invalid device handle" errors**

**Possible Causes:**
- Scanner was disconnected
- Device driver issue
- Application restarted mid-scan

**Solutions:**
1. ? Check logs for automatic reset attempts
2. ? Reconnect scanner USB cable
3. ? Restart application

**Log to Look For:**
```
[Error] Invalid device handle. Attempting to reset scanner...
[Information] Resetting scanner...
[Information] Scanner reset successful
```

**What the Fix Does:**
- Automatically resets scanner when invalid handle detected
- Reopens device connection

---

### **Issue 5: Application crashes when scanning**

**Possible Causes:**
- libzkfp.dll missing or corrupted
- Memory access violation
- Device driver conflict

**Solutions:**
1. ? Verify `libzkfp.dll` exists in `bin\Debug\net9.0\`
2. ? Copy DLL from project root if missing
3. ? Rebuild solution: `dotnet build`
4. ? Check Windows Event Viewer for crash details
5. ? Reinstall scanner drivers

**Files to Check:**
- `WebApplication1\libzkfp.dll`
- `WebApplication1\ZKFPCap.dll`
- `WebApplication1\bin\Debug\net9.0\libzkfp.dll`

---

## ?? Error Code Reference

| Error Code | Meaning | Solution |
|------------|---------|----------|
| `-8` | `ZKFP_ERR_CAPTURE` | No finger detected. Place finger on scanner. |
| `-9` | `ZKFP_ERR_EXTRACT_FP` | Failed to extract features. Try different finger or clean scanner. |
| `-12` | `ZKFP_ERR_BUSY` | Scanner busy. Wait 2-3 seconds and retry. |
| `-7` | `ZKFP_ERR_INVALID_HANDLE` | Invalid device handle. Scanner will auto-reset. |
| `-11` | `ZKFP_ERR_MEMORY_NOT_ENOUGH` | Out of memory. Restart application. |
| `-23` | `ZKFP_ERR_NOT_OPENED` | Device not opened. Check USB connection. |
| `-5` | `ZKFP_ERR_INVALID_PARAM` | Invalid parameter. Report to developer. |
| `-10` | `ZKFP_ERR_ABSORT` | Operation aborted. Retry scan. |

---

## ?? Quick Test Procedure

### **Test 1: Basic Scan**
1. Place finger on scanner
2. Click "Scan Fingerprint"
3. **Expected:** Success within 3 attempts

### **Test 2: No Finger**
1. **Do not** place finger
2. Click "Scan Fingerprint"
3. **Expected:** Error after 3 attempts: "No finger detected on scanner"

### **Test 3: Retry Logic**
1. Place finger very lightly (poor contact)
2. Click "Scan Fingerprint"
3. **Expected:** 
   - Attempt 1 fails
   - Waits 300ms
   - Attempt 2 succeeds (or attempt 3)

---

## ?? Log Monitoring Commands

### **View Logs in Visual Studio:**
1. Go to **View** ? **Output**
2. Select **Debug** from dropdown
3. Look for lines with:
   - `[Information] ? Fingerprint captured successfully`
   - `[Warning] ? Capture attempt X failed`
   - `[Information] Resetting scanner...`

### **Filter Logs for Errors:**
Search for:
- `? Capture attempt`
- `Failed to capture`
- `Error code:`

---

## ?? Advanced Troubleshooting

### **Reset Scanner Manually:**
```csharp
// In C# Immediate Window (Debug mode):
_fingerprintService.TurnOffDevice();
Thread.Sleep(500);
_fingerprintService.TurnOnDevice();
```

### **Check Scanner Status:**
```csharp
// In C# Immediate Window:
_fingerprintService.GetStatus();
// Should return: "Device Connected"
```

### **Force Device Re-initialization:**
1. Stop application
2. Unplug scanner USB
3. Wait 5 seconds
4. Plug in scanner
5. Start application

---

## ?? When to Report a Bug

**Report a bug if:**
- ? Scanner fails **every time** even with proper finger placement
- ? Logs show repeated "Scanner reset failed" messages
- ? Error code appears that's not in the reference table
- ? Application crashes during scan
- ? Scanner works with official ZKTeco software but not with this application

**Include in Bug Report:**
1. Full logs from Visual Studio Output window
2. Error codes observed
3. Steps to reproduce
4. Scanner model and USB connection type
5. Windows version

---

## ? Expected Success Rate

**After Fix:**
- Normal conditions: **>90%** success rate
- Poor finger placement: **>60%** success rate (with retries)
- Dirty scanner: **>50%** success rate (with retries)

**If success rate is lower:**
- Check scanner hardware
- Clean scanner surface
- Update scanner drivers
- Try different USB port

---

## ?? Quick Checklist

Before reporting an issue, verify:

- [ ] Scanner USB is plugged in securely
- [ ] Scanner shows in Device Manager (Windows Key + X ? Device Manager)
- [ ] `libzkfp.dll` exists in `bin\Debug\net9.0\`
- [ ] Finger is placed **flat** on scanner
- [ ] Scanner surface is clean
- [ ] Logs show retry attempts (not just single failure)
- [ ] Visual Studio Output shows detailed error messages
- [ ] Application was rebuilt after fix: `dotnet build`

---

## ?? Related Documentation

- **Full Fix Details:** `SCANNER_STABILITY_FIX_COMPLETE.md`
- **UI Feedback:** `SCANNING_FEEDBACK_IMPLEMENTATION.md`
- **ZKTeco SDK:** `WebApplication1\Services\ZKFPNative.cs`

---

**Last Updated:** December 31, 2024
**Fix Version:** v2.0 (Retry Logic + State Validation)
