# BCrypt.Net-Next 4.0.3 Upgrade Guide

## Overview
This project has been upgraded to use **BCrypt.Net-Next 4.0.3**, which includes enhanced security features and improved API methods.

## What Changed

### Package Version
- **Old:** BCrypt.Net-Next < 4.0
- **New:** BCrypt.Net-Next 4.0.3

### API Changes

#### 1. Password Hashing
**Old API:**
```csharp
var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
```

**New API (Enhanced):**
```csharp
var hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(password, BCrypt.Net.HashType.SHA384);
```

**Benefits:**
- Uses SHA-384 for enhanced security
- Configurable work factor (default: 12)
- Better protection against rainbow table attacks

#### 2. Password Verification
**Old API:**
```csharp
bool isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
```

**New API (Enhanced):**
```csharp
bool isValid = BCrypt.Net.BCrypt.EnhancedVerify(password, hashedPassword, BCrypt.Net.HashType.SHA384);
```

**Benefits:**
- More efficient verification
- Consistent with enhanced hashing
- Better error handling

## Updated Files

### 1. SuperAdminInitializer.cs
- Updated to use `EnhancedHashPassword()` with SHA-384
- Creates super admin with enhanced security

### 2. AuthController.cs
- **Login:** Updated to use `EnhancedVerify()` for password verification
- **Register:** Updated to use `EnhancedHashPassword()` for new student accounts

### 3. AdminUserController.cs
- **Create User:** Updated to use `EnhancedHashPassword()` when creating users
- **Update User:** Updated to use `EnhancedHashPassword()` when password is changed

## Security Improvements

### Hash Type Options
The new API supports multiple hash types:
- `HashType.SHA256` - Standard security (default)
- `HashType.SHA384` - Enhanced security (recommended) ?
- `HashType.SHA512` - Maximum security

**Current Implementation:** Using `SHA384` for optimal balance of security and performance.

### Work Factor
- Default work factor: **12** (4096 iterations)
- Can be customized for higher security needs
- Higher values = more secure but slower

```csharp
// Custom work factor example
var hash = BCrypt.Net.BCrypt.EnhancedHashPassword(password, BCrypt.Net.HashType.SHA384, workFactor: 13);
```

## Backward Compatibility

? **Good News:** BCrypt.Net-Next 4.0.3 can still verify passwords hashed with older versions.

If you have existing passwords in the database hashed with the old API:
```csharp
// This will still work for old hashes
bool isValid = BCrypt.Net.BCrypt.Verify(password, oldHash);

// Or use EnhancedVerify - it handles both old and new hashes
bool isValid = BCrypt.Net.BCrypt.EnhancedVerify(password, hashFromDatabase, BCrypt.Net.HashType.SHA384);
```

## Migration Strategy

### For Existing Users
1. **No immediate action needed** - Old password hashes still work
2. **Gradual migration** - Passwords will be rehashed on next login (if implemented)
3. **Optional:** Implement password rehashing on successful login:

```csharp
// After successful password verification
if (BCrypt.Net.BCrypt.EnhancedVerify(password, user.UserPassword, BCrypt.Net.HashType.SHA384))
{
    // Check if password needs rehashing (optional enhancement)
    if (!user.UserPassword.StartsWith("$2y$12$")) // Example check
    {
        // Rehash with new algorithm
        user.UserPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(password, BCrypt.Net.HashType.SHA384);
        await _userRepository.UpdateAsync(user);
    }
    
    // Continue with login...
}
```

## Testing Recommendations

### 1. Test Login with Existing Accounts
```csharp
// Super Admin from .env file
Email: admin@fingerprint.com (or from ADMIN_EMAIL env var)
Password: admin@123 (or from ADMIN_PASSWORD env var)
```

### 2. Test New User Registration
- Register a new student account
- Verify password is hashed with enhanced algorithm
- Test login with new account

### 3. Test Password Changes
- Change user password through admin panel
- Verify new password hash uses enhanced algorithm
- Test login with updated password

## Best Practices

### 1. Never Store Plain-Text Passwords
```csharp
// ? Good
user.UserPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(password, BCrypt.Net.HashType.SHA384);

// ? Bad
user.UserPassword = password; // NEVER do this!
```

### 2. Always Use Enhanced Methods for New Passwords
```csharp
// ? Recommended for new passwords
BCrypt.Net.BCrypt.EnhancedHashPassword(password, BCrypt.Net.HashType.SHA384);

// ?? Deprecated (still works but not recommended)
BCrypt.Net.BCrypt.HashPassword(password);
```

### 3. Use Consistent Hash Type
Keep the hash type consistent across your application (currently using SHA384).

## Environment Variables

The super admin password is configured in `.env`:
```env
ADMIN_EMAIL=admin@fingerprint.com
ADMIN_PASSWORD=admin@123  # Will be hashed with EnhancedHashPassword
```

**Security Note:** Change the default admin password on first login!

## Performance Considerations

### Work Factor Impact
- Work factor 12 (default): ~200-300ms per hash
- Work factor 13: ~400-600ms per hash
- Work factor 14: ~800-1200ms per hash

**Recommendation:** Keep work factor at 12 for good balance of security and user experience.

### Database Storage
- Old BCrypt hashes: ~60 characters
- Enhanced BCrypt hashes: ~60 characters (same)
- No database schema changes required ?

## Troubleshooting

### Issue: "Invalid password" after upgrade
**Cause:** Mismatch between hash type used for hashing vs verification
**Solution:** Ensure both hashing and verification use the same `HashType` (SHA384)

### Issue: Slow password verification
**Cause:** High work factor or slow server
**Solution:** Consider reducing work factor or upgrading server resources

### Issue: Existing passwords don't work
**Cause:** Unlikely - backward compatibility is maintained
**Solution:** Verify you're using `EnhancedVerify()` correctly with try-catch

## Additional Resources

- [BCrypt.Net-Next GitHub](https://github.com/BcryptNet/bcrypt.net)
- [BCrypt.Net-Next Documentation](https://github.com/BcryptNet/bcrypt.net/blob/master/README.md)
- [OWASP Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)

## Summary

? All password hashing now uses `EnhancedHashPassword()` with SHA-384
? All password verification now uses `EnhancedVerify()` with SHA-384
? Backward compatible with existing password hashes
? Enhanced security with minimal performance impact
? No database migrations required

**Next Steps:**
1. Test all authentication flows (login, register, password change)
2. Monitor performance (hashing should be fast enough for good UX)
3. Consider implementing gradual password rehashing for existing users
4. Update any custom authentication code to use enhanced methods

---

**Last Updated:** December 2024
**BCrypt.Net-Next Version:** 4.0.3
**Target Framework:** .NET 9.0
