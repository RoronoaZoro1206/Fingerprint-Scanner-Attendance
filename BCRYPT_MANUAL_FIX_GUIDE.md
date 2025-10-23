# BCrypt Compatibility Fix - Manual Steps

## Problem
The application is using `BCrypt.EnhancedHashPassword` and `BCrypt.EnhancedVerify` methods which have compatibility issues, causing the error:
> "Reading as 'System.Object' is not supported for fields having DataTypeName '-'"

## Solution
Replace Enhanced methods with standard BCrypt methods for better compatibility.

---

## Files to Fix

### 1. SuperAdminInitializer.cs ? FIXED
**Location:** `WebApplication1\Services\SuperAdminInitializer.cs`

**Line ~52** - Change:
```csharp
var hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(adminPassword, BCrypt.Net.HashType.SHA384);
```

To:
```csharp
var hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword, 12);
```

---

### 2. AuthController.cs ?? NEEDS MANUAL FIX
**Location:** `WebApplication1\Controllers\Auth\AuthController.cs`

#### Step 1: Add Logger Field (if missing)
At the top of the class (around line 13), ensure you have:
```csharp
private readonly ILogger<AuthController> _logger;
```

#### Step 2: Add Logger to Constructor
Around line 15-20, update constructor to:
```csharp
public AuthController(
    IUserRepository userRepository, 
    IStudentRepository studentRepository, 
    IStudentIdPoolRepository studentIdPoolRepository,
ILogger<AuthController> logger)
{
    _userRepository = userRepository;
    _studentRepository = studentRepository;
    _studentIdPoolRepository = studentIdPoolRepository;
    _logger = logger;
}
```

#### Step 3: Fix Password Verification (Login Method)
Around line 69-76, change:
```csharp
// OLD:
bool isPasswordValid = BCrypt.Net.BCrypt.EnhancedVerify(model.Password, user.UserPassword, BCrypt.Net.HashType.SHA384);

// NEW:
bool isPasswordValid = false;
try
{
    isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.UserPassword);
}
catch (Exception ex)
{
    _logger.LogError(ex, "BCrypt verification failed for user {Email}", model.Email);
    
    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
    {
        return Json(new { success = false, errors = new[] { "Invalid email or password." } });
    }
    ModelState.AddModelError("", "Invalid email or password.");
    return View("Index", model);
}
```

#### Step 4: Fix Password Hashing (Register Method)
Around line 238, change:
```csharp
// OLD:
string hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(model.Password, BCrypt.Net.HashType.SHA384);

// NEW:
string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password, 12);
```

---

### 3. AdminUserController.cs ? FIXED
**Location:** `WebApplication1\Controllers\Admin\AdminUserController.cs`

#### In Create Method (around line 90):
```csharp
// OLD:
user.UserPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(user.UserPassword, BCrypt.Net.HashType.SHA384);

// NEW:
user.UserPassword = BCrypt.Net.BCrypt.HashPassword(user.UserPassword, 12);
```

#### In Update Method (around line 157):
```csharp
// OLD:
user.UserPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(user.UserPassword, BCrypt.Net.HashType.SHA384);

// NEW:
user.UserPassword = BCrypt.Net.BCrypt.HashPassword(user.UserPassword, 12);
```

---

## After Making Changes

### 1. Build the Solution
Press `Ctrl+Shift+B` or run:
```bash
dotnet build
```

### 2. Delete Existing Admin from Database
Run this SQL in pgAdmin or your PostgreSQL client:
```sql
-- Delete existing admin with incorrect password hash
DELETE FROM administration WHERE user_id IN (
    SELECT user_id FROM users WHERE user_email = 'admin@fingerprint.com'
);

DELETE FROM users WHERE user_email = 'admin@fingerprint.com';
```

### 3. Restart the Application
The application will automatically create a new admin with the correct password hash.

### 4. Verify the Password Hash
Run this SQL to confirm:
```sql
SELECT 
    user_email,
    LEFT(user_password, 7) as hash_prefix,
 LENGTH(user_password) as hash_length
FROM users 
WHERE user_email = 'admin@fingerprint.com';
```

**Expected Result:**
- `hash_prefix`: `$2a$12$` or `$2b$12$`
- `hash_length`: ~60 characters

### 5. Test Login
- Navigate to: `http://localhost:7048/Auth/Index`
- Email: `admin@fingerprint.com`
- Password: `Admin@123` (from your `.env` file)

---

## Why This Works

- **Standard BCrypt Methods:** More widely compatible across platforms
- **Work Factor 12:** Provides 2^12 = 4096 iterations (secure and fast)
- **Backward Compatible:** Can verify passwords hashed with `$2a$`, `$2b$`, or `$2y$` prefixes
- **No SHA-384 Mixing:** Eliminates the compatibility layer causing database type issues

---

## Troubleshooting

### If Build Fails
1. Clean the solution: `dotnet clean`
2. Restore packages: `dotnet restore`
3. Build again: `dotnet build`

### If Login Still Fails
1. Check application logs for BCrypt errors
2. Verify password hash in database starts with `$2`
3. Ensure `.env` file is loaded (check console output on startup)
4. Try creating a new test user through the admin panel

### If You See "Stream was not readable" Error
- Close Visual Studio
- Delete `bin` and `obj` folders
- Reopen Visual Studio and rebuild

---

## Summary

The fix changes:
? `EnhancedHashPassword(password, SHA384)` ? `HashPassword(password, 12)`  
? `EnhancedVerify(password, hash, SHA384)` ? `Verify(password, hash)`  

This maintains the same security level while eliminating compatibility issues.
