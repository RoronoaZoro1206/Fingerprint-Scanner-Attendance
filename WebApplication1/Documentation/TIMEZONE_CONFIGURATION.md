# Timezone Configuration - Philippines Standard Time (PHT)

## Overview

This application enforces **Philippines Standard Time (PHT, UTC+8)** across all datetime operations to ensure consistent attendance tracking regardless of server location.

## Why This Matters

- **Server Independence**: Server may run on UTC, US time, or any timezone
- **Attendance Accuracy**: Late detection, absence marking must use PHT
- **Data Consistency**: Database timestamps need consistent interpretation
- **User Experience**: Displays must show PHT, not user's local browser time

## Implementation

### Centralized Service: `PhilippineTimeService`

Located at: `Services/PhilippineTimeService.cs`

```csharp
// Inject the service
private readonly PhilippineTimeService _pht;

// Get current Philippine time (instead of DateTime.Now)
var now = _pht.Now;
var today = _pht.Today;
var dayOfWeek = _pht.DayOfWeek;

// Parse dates as PHT
var targetDate = _pht.ParseAsPhilippineTime(dateString);

// Check if late
bool isLate = _pht.IsLate(scanTime, classStartTime, gracePeriodMinutes: 15);
```

### Static Helper: `PhilippineTime`

For Razor views and places where DI is not available:

```csharp
// In Razor views
@using Fingerprint_Attendance_System.Services
var today = PhilippineTime.Today;
var todayString = PhilippineTime.TodayString;
```

## Usage Guidelines

### ❌ DO NOT USE
```csharp
DateTime.Now          // Uses server's local time
DateTime.Today        // Uses server's local time
DateTime.UtcNow       // Requires manual conversion
```

### ✅ USE INSTEAD
```csharp
// With DI (Controllers, Services)
_philippineTimeService.Now
_philippineTimeService.Today

// Without DI (Razor Views)
PhilippineTime.Now
PhilippineTime.Today
```

## Files Updated for PHT Compliance

### Controllers
- `FacultyAttendanceController.cs` - Attendance date handling
- `AttendanceScanController.cs` - Time In/Out recording
- `AttendanceController.cs` - API attendance endpoints

### Services
- `AttendanceProcessingService.cs` - Schedule validation
- `AbsenceDetectionService.cs` - End-of-day processing
- `DailyAttendanceService.cs` - Daily log management
- `FingerprintMatchingService.cs` - Status determination

### Views
- `FacultyAttendance.cshtml` - Date picker defaults
- All attendance-related views

## Database Considerations

### Storage Strategy
- Store all timestamps in **UTC** in the database
- Convert to PHT when reading for display/logic
- Convert from PHT to UTC when writing

### Example
```csharp
// Writing to database
var utcTime = _pht.ToUtc(philippineDateTime);
await SaveToDatabase(utcTime);

// Reading from database
var utcFromDb = await GetFromDatabase();
var phtTime = _pht.FromUtc(utcFromDb);
```

## Frontend Handling

### JavaScript
When sending dates from frontend, always use explicit format:
```javascript
// Send as ISO string without timezone conversion
const dateStr = document.getElementById('dateSelect').value; // "2025-11-26"
// Send as-is, backend interprets as PHT
```

### Razor Views
```html
<!-- Use PHT for date picker defaults -->
<input type="date" value="@PhilippineTime.TodayString" max="@PhilippineTime.TodayString" />
```

## Cross-Platform Compatibility

The service handles timezone ID differences:
- **Windows**: `Singapore Standard Time`
- **Linux/macOS**: `Asia/Manila`
- **Fallback**: Custom UTC+8 timezone

## Registration

Add to `Program.cs`:
```csharp
builder.Services.AddSingleton<PhilippineTimeService>();
```

## Testing

To verify PHT is working correctly:
1. Check `/health` endpoint - timestamp should show PHT
2. Create attendance record - verify time matches PHT
3. Test on server with different timezone - should still show PHT

---

*Timezone Configuration for Fingerprint Attendance System v1.0.0*
