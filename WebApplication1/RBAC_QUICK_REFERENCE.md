# RBAC Quick Reference Guide

## Adding Authorization to New Controllers

### Step 1: Add Using Statements
```csharp
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models.Enums;
```

### Step 2: Add Attribute to Controller
```csharp
// For Admin-only controllers
[RoleAuthorize(USER_ROLES.ADMIN)]
public class YourController : Controller { }

// For Faculty-only controllers
[RoleAuthorize(USER_ROLES.FACULTY)]
public class YourController : Controller { }

// For Student-only controllers
[RoleAuthorize(USER_ROLES.STUDENT)]
public class YourController : Controller { }

// For multiple roles
[RoleAuthorize(USER_ROLES.ADMIN, USER_ROLES.FACULTY)]
public class YourController : Controller { }
```

## Available Roles
- `USER_ROLES.ADMIN` (RoleId = 1)
- `USER_ROLES.FACULTY` (RoleId = 2)
- `USER_ROLES.STUDENT` (RoleId = 3)

## Session Variables
The following are stored in session after login:
- `UserId` (int)
- `RoleId` (int)
- `UserEmail` (string)
- `UserName` (string)
- `UserProfileImage` (string)

## Accessing Session in Controllers
```csharp
var userId = HttpContext.Session.GetInt32("UserId");
var roleId = HttpContext.Session.GetInt32("RoleId");
var userEmail = HttpContext.Session.GetString("UserEmail");
```

## Error Pages
- `/Error/AccessDenied` - 403 Forbidden (unauthorized role)
- `/Auth/Index` - Login page (not authenticated)

## Testing Authorization
1. Login with different roles
2. Try accessing URLs directly in browser
3. Verify proper redirects occur

## Common Patterns

### Verify User Owns Resource
```csharp
[RoleAuthorize(USER_ROLES.STUDENT)]
public class StudentController : Controller
{
    public async Task<IActionResult> MyData()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var student = await _studentRepo.GetByUserIdAsync(userId.Value);
        
        // Only show data for this student
        return View(student);
    }
}
```

### Allow Multiple Roles with Different Logic
```csharp
[RoleAuthorize(USER_ROLES.ADMIN, USER_ROLES.FACULTY)]
public class ReportsController : Controller
{
    public async Task<IActionResult> Index()
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        var role = (USER_ROLES)roleId.Value;
        
        if (role == USER_ROLES.ADMIN)
        {
            // Show all reports
        }
        else if (role == USER_ROLES.FACULTY)
        {
            // Show only faculty's reports
        }
        
        return View();
    }
}
```

## Troubleshooting

### Issue: Always redirected to login
**Solution**: Check if session is configured in Program.cs
```csharp
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// And in middleware
app.UseSession();
```

### Issue: Authorization not working
**Solution**: Ensure attribute is applied to controller class
```csharp
[RoleAuthorize(USER_ROLES.ADMIN)]  // ← Must be here
public class AdminController : Controller
```

### Issue: Need to exclude specific action from authorization
**Solution**: Create a separate controller without authorization for public actions
