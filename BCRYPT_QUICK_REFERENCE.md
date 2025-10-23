# ?? BCrypt Quick Reference Card

## Password Hashing (Registration / User Creation)
```csharp
// ALWAYS use explicit work factor
private const int BCRYPT_WORK_FACTOR = 12;

string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, BCRYPT_WORK_FACTOR);

_logger.LogInformation("Password hashed with BCrypt work factor: {WorkFactor}", BCRYPT_WORK_FACTOR);
```

## Password Verification (Login)
```csharp
bool isValid = false;
try
{
    isValid = BCrypt.Net.BCrypt.Verify(password, storedHash);
    
    if (!isValid)
    {
        _logger.LogWarning("Failed login attempt for user {Email}", email);
    }
}
catch (BCrypt.Net.SaltParseException ex)
{
    _logger.LogError(ex, "Invalid BCrypt hash format for user {Email}", email);
    return "Invalid email or password."; // Generic error
}
catch (Exception ex)
{
    _logger.LogError(ex, "BCrypt verification failed for user {Email}", email);
    return "Invalid email or password."; // Generic error
}
```

## Work Factor Reference
| Work Factor | Iterations | Time | Use Case |
|-------------|------------|------|----------|
| 10 | 1,024 | ~50ms | High-traffic sites |
| **12** ? | **4,096** | **~250ms** | **Recommended (2024)** |
| 13 | 8,192 | ~500ms | High-security apps |
| 14 | 16,384 | ~1s | Maximum security |

## Security Checklist
- [ ] Work factor = 12
- [ ] Explicit work factor in all hashing
- [ ] Exception handling implemented
- [ ] Structured logging configured
- [ ] Generic error messages (no enumeration)
- [ ] No passwords in logs

## Quick Test
```sql
-- Verify hash format in database
SELECT 
    user_email,
    LEFT(user_password, 10) as prefix,  -- Should be $2a$12$ or $2y$12$
    LENGTH(user_password) as length     -- Should be ~60
FROM users LIMIT 5;
```

## Emergency Fixes
```bash
# Reset admin password
DELETE FROM administration WHERE user_id IN (
    SELECT user_id FROM users WHERE user_email = 'admin@fingerprint.com'
);
DELETE FROM users WHERE user_email = 'admin@fingerprint.com';
# Restart app - admin will be recreated
```

---

**Work Factor:** 12 (4,096 iterations)  
**Hash Time:** ~250ms  
**Security:** ? Production-Ready  
**Package:** BCrypt.Net-Next 4.0.3
