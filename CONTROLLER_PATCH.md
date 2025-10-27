# Quick Patch for Faculty and Student Controllers

## Apply these changes manually:

### 1. FacultyRegisterFingerprintController.cs

**Line 9**: Change from:
```csharp
public class FacultyRegisterFingerprintController : Controller
```

**To**:
```csharp
[Route("Faculty/FacultyRegisterFingerprint")]
public class FacultyRegisterFingerprintController : Controller
```

**Line 24-25**: Change from:
```csharp
[HttpGet]
public IActionResult Index()
```

**To**:
```csharp
[HttpGet("")]
[HttpGet("Index")]
public IActionResult Index()
```

**Line 31**: Change from:
```csharp
[HttpGet]
public IActionResult GetDeviceStatus()
```

**To**:
```csharp
[HttpGet("GetDeviceStatus")]
public IActionResult GetDeviceStatus()
```

**Line 53**: Change from:
```csharp
[HttpPost]
public IActionResult InitializeDevice()
```

**To**:
```csharp
[HttpPost("InitializeDevice")]
public IActionResult InitializeDevice()
```

**Line 69**: Change from:
```csharp
[HttpGet]
public async Task<IActionResult> GetRegistrationStatus()
```

**To**:
```csharp
[HttpGet("GetRegistrationStatus")]
public async Task<IActionResult> GetRegistrationStatus()
```

**Line 89**: Change from:
```csharp
[HttpPost]
public IActionResult CaptureFingerprint()
```

**To**:
```csharp
[HttpPost("CaptureFingerprint")]
public IActionResult CaptureFingerprint()
```

**Line 113**: Change from:
```csharp
[HttpPost]
public async Task<IActionResult> RegisterFingerprint([FromBody] FingerprintRegistrationDto dto)
```

**To**:
```csharp
[HttpPost("RegisterFingerprint")]
public async Task<IActionResult> RegisterFingerprint([FromBody] FingerprintRegistrationDto dto)
```

**Line 173**: Change from:
```csharp
[HttpPost]
public async Task<IActionResult> DeleteFingerprint()
```

**To**:
```csharp
[HttpPost("DeleteFingerprint")]
public async Task<IActionResult> DeleteFingerprint()
```

---

### 2. StudentRegisterFingerprontController.cs (note the typo in filename!)

**Apply the SAME changes**, but use this route:
```csharp
[Route("Student/StudentRegisterFingerpront")]
public class StudentRegisterFingerprontController : Controller
```

Then add route names to all methods:
- `[HttpGet("")]`
- `[HttpGet("Index")]`
- `[HttpGet("GetDeviceStatus")]`
- `[HttpPost("InitializeDevice")]`
- `[HttpGet("GetRegistrationStatus")]`
- `[HttpPost("CaptureFingerprint")]`
- `[HttpPost("RegisterFingerprint")]`
- `[HttpPost("DeleteFingerprint")]`

---

## OR Use Find & Replace

### For Faculty Controller:
1. Open `FacultyRegisterFingerprintController.cs`
2. Find: `public class FacultyRegisterFingerprintController : Controller`
3. Replace with: `[Route("Faculty/FacultyRegisterFingerprint")]\n    public class FacultyRegisterFingerprintController : Controller`

4. Find: `        [HttpGet]\n        public IActionResult Index()`
5. Replace with: `      [HttpGet("")]\n        [HttpGet("Index")]\n     public IActionResult Index()`

6. Find: `        [HttpGet]\n     public IActionResult GetDeviceStatus()`
7. Replace with: `        [HttpGet("GetDeviceStatus")]\n        public IActionResult GetDeviceStatus()`

8. Find: `        [HttpPost]\n        public IActionResult InitializeDevice()`
9. Replace with: `        [HttpPost("InitializeDevice")]\n        public IActionResult InitializeDevice()`

10. Find: `        [HttpGet]\n        public async Task<IActionResult> GetRegistrationStatus()`
11. Replace with: `     [HttpGet("GetRegistrationStatus")]\n        public async Task<IActionResult> GetRegistrationStatus()`

12. Find: `        [HttpPost]\n      public IActionResult CaptureFingerprint()`
13. Replace with: `        [HttpPost("CaptureFingerprint")]\n   public IActionResult CaptureFingerprint()`

14. Find: `  [HttpPost]\n        public async Task<IActionResult> RegisterFingerprint(`
15. Replace with: `        [HttpPost("RegisterFingerprint")]\n        public async Task<IActionResult> RegisterFingerprint(`

16. Find: `        [HttpPost]\n        public async Task<IActionResult> DeleteFingerprint()`
17. Replace with: `[HttpPost("DeleteFingerprint")]\n        public async Task<IActionResult> DeleteFingerprint()`

---

## After Applying Changes

Run:
```bash
dotnet build
```

Expected output:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Test All Three Controllers

1. **Admin**: `https://localhost:7048/Admin/AdminRegisterFingerprint`
2. **Faculty**: `https://localhost:7048/Faculty/FacultyRegisterFingerprint`
3. **Student**: `https://localhost:7048/Student/StudentRegisterFingerpront`

All three should work now!
