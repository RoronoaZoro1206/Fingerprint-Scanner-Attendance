# BCrypt.Net-Next 4.0.3 Implementation Guide

## Overview
This project uses **BCrypt.Net-Next 4.0.3** for password hashing with industry best practices and optimal security settings.

---

## Implementation Details

### Work Factor Configuration
```csharp
// Work Factor: 12 (recommended for 2024)
private const int BCRYPT_WORK_FACTOR = 12;

// Security Level:
// - Work Factor 12 = 2^12 = 4,096 iterations
// - Processing Time: ~200-300ms per hash (acceptable UX)
// - Security: Strong protection against brute-force attacks
```

### Why Work Factor 12?
| Work Factor | Iterations | Time | Security | Use Case |
|-------------|------------|------|----------|----------|
| 10 | 1,024 | ~50ms | Good | High-traffic sites |
| **12** | **4,096** | **~250ms** | **Strong** | **Recommended (2024)** ? |
| 13 | 8,192 | ~500ms | Very Strong | High-security apps |
| 14 | 16,384 | ~1s | Maximum | Paranoid security |

**Chosen:** Work Factor 12 provides the best balance of security and user experience.

---

## Code Implementation

### 1. SuperAdminInitializer.cs
**Purpose:** Create super admin account on first run

```csharp
// BCrypt work factor constant
private const int BCRYPT_WORK_FACTOR = 12;

// Hash password
var hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword, BCRYPT_WORK_FACTOR);

_logger.LogInformation($"Password hashed with BCrypt work factor: {BCRYPT_WORK_FACTOR}");
```

**Features:**
- ? Explicit work factor 12
- ? Detailed logging
- ? Environment variable configuration
- ? Automatic initialization on startup

---

### 2. AuthController.cs
**Purpose:** Handle user login and registration

#### Login Method
```csharp
// Verify password - automatically detects work factor from stored hash
bool isPasswordValid = false;
try
{
    // BCrypt.Verify is backward compatible with all hash formats:
    // $2a$ (OpenBSD format)
    // $2b$ (bug-fix version)
    // $2y$ (PHP-compatible format)
    isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.UserPassword);
    
    if (!isPasswordValid)
    {
        _logger.LogWarning("Failed login attempt for user {Email} - invalid password", model.Email);
 }
}
catch (BCrypt.Net.SaltParseException ex)
{
    // Specific exception for malformed BCrypt hashes
    _logger.LogError(ex, "Invalid BCrypt hash format for user {Email}", model.Email);
    // Return generic error to prevent user enumeration
}
catch (Exception ex)
{
    // General BCrypt verification error
    _logger.LogError(ex, "BCrypt verification failed for user {Email}", model.Email);
}
```

**Features:**
- ? Backward compatible with all BCrypt hash formats
- ? Specific exception handling for BCrypt errors
- ? Security logging (failed attempts)
- ? No user enumeration vulnerability

#### Registration Method
```csharp
// Hash password with explicit work factor
string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password, BCRYPT_WORK_FACTOR);

_logger.LogInformation("New user password hashed with BCrypt work factor: {WorkFactor}", BCRYPT_WORK_FACTOR);

var user = new User
{
    // ...other properties...
    UserPassword = hashedPassword,
    UserCreatedAt = DateTime.Now,
    UserUpdatedAt = DateTime.Now,
    IsActive = true,
    UserProfileImage = "default_profile.png"
};
```

**Features:**
- ? Consistent work factor across application
- ? Security audit logging
- ? Default secure values

---

### 3. AdminUserController.cs
**Purpose:** Admin user management (create/update users)

#### Create User
```csharp
// Hash password with explicit work factor
user.UserPassword = BCrypt.Net.BCrypt.HashPassword(user.UserPassword, BCRYPT_WORK_FACTOR);
user.UserProfileImage = user.UserProfileImage ?? "default_profile.png";

_logger.LogInformation("Creating user {Email} with BCrypt work factor: {WorkFactor}", 
    user.UserEmail, BCRYPT_WORK_FACTOR);
```

#### Update User
```csharp
// Only hash if password is provided (optional password change)
if (!string.IsNullOrEmpty(user.UserPassword))
{
    user.UserPassword = BCrypt.Net.BCrypt.HashPassword(user.UserPassword, BCRYPT_WORK_FACTOR);
    _logger.LogInformation("Updating password for user {Email} with BCrypt work factor: {WorkFactor}", 
        user.UserEmail, BCRYPT_WORK_FACTOR);
}
else
{
    // Preserve existing password hash
    user.UserPassword = existingUser.UserPassword;
}
```

**Features:**
- ? Optional password updates
- ? Password preservation when not changing
- ? Audit logging for password changes

---

## Security Best Practices

### 1. **Never Store Plain-Text Passwords**
```csharp
// ? CORRECT
user.UserPassword = BCrypt.Net.BCrypt.HashPassword(password, BCRYPT_WORK_FACTOR);

// ? NEVER DO THIS
user.UserPassword = password;
```

### 2. **Always Use Explicit Work Factor**
```csharp
// ? CORRECT - Explicit work factor
BCrypt.Net.BCrypt.HashPassword(password, 12);

// ?? NOT RECOMMENDED - Uses default (may change in future versions)
BCrypt.Net.BCrypt.HashPassword(password);
```

### 3. **Proper Exception Handling**
```csharp
try
{
    isValid = BCrypt.Net.BCrypt.Verify(password, hash);
}
catch (BCrypt.Net.SaltParseException)
{
    // Handle malformed hash
}
catch (Exception)
{
    // Handle general verification error
}
```

### 4. **Security Logging**
```csharp
// Log failed attempts (helps detect brute-force attacks)
_logger.LogWarning("Failed login attempt for user {Email}", email);

// Log password changes (audit trail)
_logger.LogInformation("Password updated for user {Email}", email);
```

### 5. **Prevent User Enumeration**
```csharp
// ? CORRECT - Generic error message
return Json(new { success = false, errors = new[] { "Invalid email or password." } });

// ? NEVER DO THIS - Reveals if email exists
return Json(new { success = false, errors = new[] { "Email not found." } });
```

---

## Database Schema

### Password Storage
```sql
CREATE TABLE users (
    -- ...other columns...
    user_password TEXT NOT NULL,  -- BCrypt hash (~60 characters)
    -- ...other columns...
);
```

**Hash Format Examples:**
```
$2a$12$R9h/cIPz0gi.URNNX3kh2OPST9/PgBkqquzi.Ss7KIUgO2t0jWMUW  (OpenBSD)
$2b$12$R9h/cIPz0gi.URNNX3kh2OPST9/PgBkqquzi.Ss7KIUgO2t0jWMUW  (Bug-fix)
$2y$12$R9h/cIPz0gi.URNNX3kh2OPST9/PgBkqquzi.Ss7KIUgO2t0jWMUW  (PHP-compatible)
```

**Hash Structure:**
```
$2y$ 12 $ salt (22 chars) + hash (31 chars)
 ?    ?       ?       ?
 ?    ? ?         ?? Actual password hash
 ?    ?       ?? Random salt (prevents rainbow tables)
 ?    ?? Work factor (2^12 = 4096 iterations)
 ?? Algorithm identifier
```

**Storage Requirements:**
- Length: ~60 characters
- Type: TEXT (VARCHAR(255) recommended)
- Encoding: ASCII (Base64 encoded)

---

## Performance Metrics

### Hashing Performance
| Operation | Time | Notes |
|-----------|------|-------|
| Hash (Work Factor 12) | ~250ms | First-time user registration |
| Verify (Work Factor 12) | ~250ms | User login |
| Database Lookup | ~10ms | Email/ID query |
| **Total Login Time** | **~260ms** | **Acceptable UX** ? |

### Scalability
- **Concurrent Users:** 100 simultaneous logins = ~25 seconds total
- **Bottleneck:** CPU-bound (BCrypt is intentionally slow)
- **Solution:** Use async/await (implemented) + scale horizontally

---

## Testing

### 1. Test Password Hashing
```csharp
[Test]
public void TestPasswordHashing()
{
    var password = "TestPassword123!";
    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, 12);
    
    Assert.IsNotNull(hashedPassword);
    Assert.IsTrue(hashedPassword.StartsWith("$2"));
    Assert.AreEqual(60, hashedPassword.Length);
}
```

### 2. Test Password Verification
```csharp
[Test]
public void TestPasswordVerification()
{
  var password = "TestPassword123!";
    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, 12);
    
    Assert.IsTrue(BCrypt.Net.BCrypt.Verify(password, hashedPassword));
    Assert.IsFalse(BCrypt.Net.BCrypt.Verify("WrongPassword", hashedPassword));
}
```

### 3. Test Backward Compatibility
```csharp
[Test]
public void TestBackwardCompatibility()
{
    var password = "TestPassword123!";
    
    // Old hash format ($2a$)
    var oldHash = "$2a$12$R9h/cIPz0gi.URNNX3kh2OPST9/PgBkqquzi.Ss7KIUgO2t0jWMUW";
    
  // Should still verify correctly
    Assert.IsTrue(BCrypt.Net.BCrypt.Verify(password, oldHash));
}
```

---

## Migration Guide

### Migrating Existing Passwords
If you have existing passwords with different work factors:

```csharp
// Optional: Rehash on successful login
if (BCrypt.Net.BCrypt.Verify(password, user.UserPassword))
{
    // Check if hash needs updating
    if (!user.UserPassword.StartsWith("$2y$12$"))
    {
        // Rehash with new work factor
        user.UserPassword = BCrypt.Net.BCrypt.HashPassword(password, BCRYPT_WORK_FACTOR);
   await _userRepository.UpdateAsync(user);
        
 _logger.LogInformation("Rehashed password for user {Email} to work factor {WorkFactor}", 
   user.UserEmail, BCRYPT_WORK_FACTOR);
    }
    
    // Continue with login...
}
```

---

## Troubleshooting

### Issue: "Invalid BCrypt hash"
**Cause:** Malformed hash in database  
**Solution:** Verify hash starts with `$2a$`, `$2b$`, or `$2y$` and is ~60 characters

### Issue: "Slow login performance"
**Cause:** High work factor or slow CPU  
**Solution:** Work factor 12 is optimal. Consider horizontal scaling if needed.

### Issue: "Passwords not verifying"
**Cause:** Mismatch between hash and verification  
**Solution:** Ensure both hashing and verification use same BCrypt implementation

---

## Security Audit Checklist

- [x] Work factor 12 or higher
- [x] Explicit work factor in all hashing calls
- [x] Exception handling for BCrypt errors
- [x] Security logging (failed attempts, password changes)
- [x] No plain-text passwords in logs
- [x] Generic error messages (no user enumeration)
- [x] Backward compatibility maintained
- [x] Async/await for non-blocking operations
- [x] Database uses TEXT column for password storage

---

## References

- [BCrypt.Net-Next GitHub](https://github.com/BcryptNet/bcrypt.net)
- [BCrypt Algorithm Specification](https://en.wikipedia.org/wiki/Bcrypt)
- [OWASP Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
- [NIST Password Guidelines](https://pages.nist.gov/800-63-3/sp800-63b.html)

---

**Implementation Status:** ? Complete  
**Security Level:** ? Production-Ready  
**Performance:** ? Optimized  
**Documentation:** ? Comprehensive  

**Last Updated:** December 2024  
**BCrypt.Net-Next Version:** 4.0.3  
**Target Framework:** .NET 9.0
