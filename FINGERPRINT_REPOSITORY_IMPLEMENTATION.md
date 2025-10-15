# Fingerprint Template Repository Implementation

## Overview
Clean repository pattern implementation for managing fingerprint templates with proper separation of concerns.

## Architecture

```
Controller → Repository Interface → Repository Implementation → Database
```

### Benefits
- ✅ Separation of concerns (business logic vs data access)
- ✅ Testable code (can mock repository)
- ✅ Reusable data access methods
- ✅ Type-safe operations
- ✅ Consistent error handling

## Files Created

### 1. Model - `Models/FingerprintTemplate.cs`
```csharp
public class FingerprintTemplate
{
    public int FtempId { get; set; }
    public int UserId { get; set; }
    public byte[] FtempData { get; set; }
    public string? FtempName { get; set; }
    public DateTime FtempCreatedAt { get; set; }
}
```

### 2. Interface - `Repositories/Interfaces/IFingerprintTemplateRepository.cs`
Defines the contract for fingerprint data access:
- `HasFingerprintAsync(userId)` - Check if user has fingerprint
- `GetByUserIdAsync(userId)` - Get fingerprint by user ID
- `GetByIdAsync(ftempId)` - Get fingerprint by template ID
- `CreateAsync(template)` - Create new fingerprint
- `UpdateAsync(template)` - Update existing fingerprint
- `DeleteByUserIdAsync(userId)` - Delete fingerprint
- `GetAllAsync()` - Get all fingerprints

### 3. Implementation - `Repositories/Implementations/FingerprintTemplateRepository.cs`
Concrete implementation using `DatabaseHelper` for PostgreSQL operations.

### 4. Controller Update - `Controllers/FingerprintStatusController.cs`
Updated to use repository instead of direct database access.

## Usage Examples

### Check if User Has Fingerprint
```csharp
var hasFingerprint = await _fingerprintRepo.HasFingerprintAsync(userId);
```

### Get Fingerprint by User ID
```csharp
var template = await _fingerprintRepo.GetByUserIdAsync(userId);
if (template != null)
{
    // Process fingerprint data
}
```

### Create New Fingerprint
```csharp
var template = new FingerprintTemplate
{
    UserId = userId,
    FtempData = fingerprintData,
    FtempName = "Right Index",
    FtempCreatedAt = DateTime.UtcNow
};

var newId = await _fingerprintRepo.CreateAsync(template);
```

### Update Fingerprint
```csharp
var template = await _fingerprintRepo.GetByUserIdAsync(userId);
if (template != null)
{
    template.FtempData = newFingerprintData;
    template.FtempName = "Updated Name";
    await _fingerprintRepo.UpdateAsync(template);
}
```

### Delete Fingerprint
```csharp
var success = await _fingerprintRepo.DeleteByUserIdAsync(userId);
```

## Database Schema

### Table: fingerprint_templates
```sql
CREATE TABLE fingerprint_templates (
    ftemp_id          SERIAL PRIMARY KEY,
    user_id           INT UNIQUE REFERENCES users(user_id) ON DELETE CASCADE,
    ftemp_data        BYTEA NOT NULL,
    ftemp_name        VARCHAR(20),
    ftemp_created_at  TIMESTAMP NOT NULL DEFAULT NOW()
);
```

### Key Points
- `user_id` references `users(user_id)` - supports all user types
- `UNIQUE` constraint ensures one fingerprint per user
- `CASCADE` delete removes fingerprint when user is deleted
- `ftemp_data` stores binary fingerprint template data
- `ftemp_name` optional label (e.g., "Right Index", "Left Thumb")

## Dependency Injection

### Registration in Program.cs
```csharp
builder.Services.AddScoped<IFingerprintTemplateRepository, FingerprintTemplateRepository>();
```

### Controller Injection
```csharp
public class FingerprintStatusController : ControllerBase
{
    private readonly IFingerprintTemplateRepository _fingerprintRepo;

    public FingerprintStatusController(IFingerprintTemplateRepository fingerprintRepo)
    {
        _fingerprintRepo = fingerprintRepo;
    }
}
```

## API Endpoint

### Check Fingerprint Status
**Endpoint**: `GET /api/fingerprint/check-status`

**Response**:
```json
{
  "hasFingerprint": true
}
```

**Implementation**:
```csharp
[HttpGet("check-status")]
public async Task<IActionResult> CheckFingerprintStatus()
{
    var userId = HttpContext.Session.GetInt32("UserId");
    if (userId == null)
        return Unauthorized(new { hasFingerprint = true });

    var hasFingerprint = await _fingerprintRepo.HasFingerprintAsync(userId.Value);
    return Ok(new { hasFingerprint });
}
```

## Error Handling

### Repository Level
- Returns `null` for not found items
- Throws exceptions for database errors
- Returns `false` for failed operations

### Controller Level
- Catches exceptions and logs them
- Returns safe defaults to avoid breaking UI
- Returns `true` on error to prevent annoying users

## Testing Considerations

### Unit Testing
Mock the repository interface:
```csharp
var mockRepo = new Mock<IFingerprintTemplateRepository>();
mockRepo.Setup(r => r.HasFingerprintAsync(It.IsAny<int>()))
    .ReturnsAsync(true);

var controller = new FingerprintStatusController(mockRepo.Object);
```

### Integration Testing
Use test database:
```csharp
var repo = new FingerprintTemplateRepository(testDbHelper);
var template = new FingerprintTemplate { /* test data */ };
var id = await repo.CreateAsync(template);
Assert.True(id > 0);
```

## Performance Considerations

### Indexing
Ensure index on `user_id` for fast lookups:
```sql
CREATE INDEX idx_fingerprint_templates_user_id ON fingerprint_templates(user_id);
```

### Caching (Future Enhancement)
Consider caching fingerprint status:
```csharp
// Cache for 5 minutes
var cacheKey = $"fingerprint_status_{userId}";
var hasFingerprint = await _cache.GetOrCreateAsync(cacheKey, async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
    return await _fingerprintRepo.HasFingerprintAsync(userId);
});
```

## Security Considerations

### Data Protection
- Fingerprint data (`ftemp_data`) is stored as binary (BYTEA)
- Consider encrypting fingerprint data at rest
- Use HTTPS for all API calls
- Validate user session before operations

### Access Control
- Only allow users to access their own fingerprint data
- Admins can view all fingerprints (if needed)
- Log all fingerprint access for audit trail

## Future Enhancements

### Potential Features
- [ ] Support multiple fingerprints per user
- [ ] Fingerprint quality score tracking
- [ ] Fingerprint enrollment history
- [ ] Fingerprint verification attempts logging
- [ ] Fingerprint template versioning
- [ ] Bulk fingerprint operations
- [ ] Fingerprint data encryption
- [ ] Fingerprint matching service integration

### Schema Extensions
```sql
-- Multiple fingerprints per user
ALTER TABLE fingerprint_templates DROP CONSTRAINT fingerprint_templates_user_id_key;
ALTER TABLE fingerprint_templates ADD COLUMN finger_position VARCHAR(20);
ALTER TABLE fingerprint_templates ADD COLUMN is_primary BOOLEAN DEFAULT FALSE;

-- Quality tracking
ALTER TABLE fingerprint_templates ADD COLUMN quality_score INT;
ALTER TABLE fingerprint_templates ADD COLUMN enrollment_attempts INT DEFAULT 1;

-- Versioning
ALTER TABLE fingerprint_templates ADD COLUMN version INT DEFAULT 1;
ALTER TABLE fingerprint_templates ADD COLUMN replaced_by INT REFERENCES fingerprint_templates(ftemp_id);
```

## Related Files
- `Controllers/FingerprintStatusController.cs` - API endpoint
- `wwwroot/js/fingerprint-prompt.js` - Frontend dialog
- `wwwroot/css/fingerprint-prompt.css` - Dialog styling
- `Views/Shared/_MainLayout.cshtml` - Layout integration
- `FINGERPRINT_PROMPT_IMPLEMENTATION.md` - Overall feature documentation

## Conclusion
This repository implementation provides a clean, maintainable, and testable approach to managing fingerprint templates. The separation of concerns makes it easy to extend functionality and maintain code quality.
