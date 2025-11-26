# Fingerprint Attendance System - Technical Architecture

## 📋 Table of Contents
1. [System Architecture](#system-architecture)
2. [Service Layer Details](#service-layer-details)
3. [Repository Pattern](#repository-pattern)
4. [Middleware Pipeline](#middleware-pipeline)
5. [Real-time Communication](#real-time-communication)
6. [Background Services](#background-services)
7. [Security Implementation](#security-implementation)
8. [Fingerprint Integration](#fingerprint-integration)
9. [Database Design](#database-design)
10. [Configuration Management](#configuration-management)

---

## 🏗 System Architecture

### High-Level Architecture
```
┌─────────────────────────────────────────────────────────────────┐
│                         CLIENT LAYER                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │   Browser   │  │  Mobile App │  │  External Systems (API) │  │
│  └──────┬──────┘  └──────┬──────┘  └────────────┬────────────┘  │
└─────────┼────────────────┼──────────────────────┼───────────────┘
          │                │                      │
          ▼                ▼                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                      PRESENTATION LAYER                          │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │  MVC Controllers │  │  API Controllers │  │  SignalR Hub   │  │
│  │  (Views/Razor)   │  │  (REST/JSON)     │  │  (WebSocket)   │  │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘  │
└───────────┼────────────────────┼────────────────────┼───────────┘
            │                    │                    │
            ▼                    ▼                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                       SERVICE LAYER                              │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  FingerprintService │ AttendanceProcessingService │ etc. │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Interfaces: IFingerprintService, IFingerprintMatchingService│
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     REPOSITORY LAYER                             │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  IUserRepository │ IAttendanceRepository │ IClassRepository│  │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  UserRepository │ AttendanceRepository │ ClassRepository  │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        DATA LAYER                                │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              DatabaseHelper (Dapper ORM)                  │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                    PostgreSQL Database                    │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### Component Interaction Flow
```
User Request → Middleware Pipeline → Controller → Service → Repository → Database
                    │
                    ├── GlobalExceptionHandler
                    ├── RequestResponseLogging
                    ├── Session
                    ├── RateLimiter
                    └── Authorization
```

---

## ⚙️ Service Layer Details

### Core Services Architecture

#### FingerprintService (Singleton)
```csharp
public class FingerprintService : IFingerprintService
{
    // Device Management
    - Initialize()           // SDK initialization
    - TurnOnDevice()         // Open device connection
    - TurnOffDevice()        // Close device connection
    - GetStatus()            // Device status string
    - IsDeviceConnected      // Connection state property
    
    // Fingerprint Capture
    - CaptureTemplate()                    // Basic capture
    - CaptureTemplateBase64()              // Base64 encoded
    - CaptureFingerprintWithImage()        // Full capture with image
    
    // Matching
    - MatchFingerprints()    // Compare two templates
    - GetMatchScore()        // Get similarity score
    - IdentifyFingerprint()  // Identify against DB
    
    // Hardware Control
    - SetLedLight()          // Control LED colors
    - ActivateSuccessLED()   // Green LED sequence
    - ActivateErrorLED()     // Red LED sequence
    - Beep()                 // Buzzer control
}
```

#### FingerprintMatchingService (Scoped)
```csharp
public class FingerprintMatchingService : IFingerprintMatchingService
{
    // Main Matching Logic
    - MatchAndValidateFingerprintAsync(template, requireStudentRole, cancellationToken)
      → Matches fingerprint against all templates in database
      → Validates user role (Admin/Faculty cannot use attendance scanner)
      → Returns FingerprintMatchResult with user/student info
    
    // Support Methods
    - LogScanAttemptAsync()           // Log to scan_logs table
    - ProvideLedFeedbackAsync()       // Visual/audio feedback
    - DetermineAttendanceStatus()     // Present vs Late calculation
}
```

#### AttendanceProcessingService (Scoped)
```csharp
public class AttendanceProcessingService
{
    // Main Pipeline
    ProcessAttendanceAsync(cancellationToken)
    {
        1. ValidateScannerAsync()      // Check device ready
        2. CaptureFingerprintAsync()   // Capture with timeout
        3. MatchFingerprintAsync()     // Identify student
        4. ValidateEnrollmentAsync()   // Check enrollments
        5. ValidateScheduleAsync()     // Check current schedule
        6. CheckIncompleteRecords()    // Flag forgot timeouts
        7. ProcessAttendanceScan()     // Record Time In/Out
    }
}
```

### Service Dependencies Graph
```
AttendanceProcessingService
├── IFingerprintService
├── IFingerprintMatchingService
├── IStudentRepository
├── IEnrollmentRepository
├── IClassScheduleRepository
├── DailyAttendanceService
├── MissedTimeOutService
├── LateDetectionService
└── AbsenceDetectionService

FingerprintMatchingService
├── IFingerprintService
├── IFingerprintTemplateRepository
├── IUserRepository
├── IStudentRepository
├── IScanLogRepository
└── AttendanceSettings (IOptions)
```

---

## 📦 Repository Pattern

### Interface Design
```csharp
// Base pattern for all repositories
public interface IRepository<T>
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<bool> AddAsync(T entity);
    Task<bool> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}

// Extended interfaces add domain-specific methods
public interface IAttendanceRepository : IRepository<Attendance>
{
    Task<IEnumerable<Attendance>> GetByClassIdAsync(int classId);
    Task<IEnumerable<Attendance>> GetByStudentIdAsync(int studId);
    Task<Attendance?> GetByClassStudentDateAsync(int classId, int studId, DateTime date);
    Task<ClassAttendanceDto?> GetClassAttendanceWithDetailsAsync(int classId, DateTime date, int facId);
}
```

### Repository List
| Repository | Entity | Special Methods |
|------------|--------|-----------------|
| UserRepository | User | GetByEmailAsync, GetByRoleAsync |
| StudentRepository | Student | GetByUserIdAsync, GetStudentInfoByUserIdAsync |
| FacultyRepository | Faculty | GetByUserIdAsync, GetAllWithDetailsAsync |
| AttendanceRepository | Attendance | GetClassAttendanceWithDetailsAsync |
| EnrollmentRepository | Enrollment | GetStudentEnrollmentsWithDetailsAsync |
| ClassRepository | Class | GetByFacultyIdAsync, GetClassSchedulesAsync |
| ClassScheduleRepository | ClassSchedule | GetByClassIdAsync, CheckFacultyScheduleConflictAsync |
| CourseRepository | Course | GetByProgramIdAsync |
| DepartmentRepository | Department | GetByCodeAsync |
| ProgramRepository | ProgramModel | GetByDepartmentIdAsync |
| SectionRepository | Section | GetByProgramIdAsync |
| RoomRepository | Room | GetByCodeAsync |
| AcademicTermRepository | AcademicTerm | GetActiveTermAsync, SetActiveTermAsync |
| FingerprintTemplateRepository | FingerprintTemplate | GetByUserIdAsync |
| DailyAttendanceLogRepository | DailyAttendanceLog | GetByStudentClassAndDateAsync |
| ScanLogRepository | ScanLog | GetRecentLogsAsync |
| StudentIdPoolRepository | StudentIdPool | GetAvailableAsync, MarkAsUsedAsync |
| AttendanceNotificationRepository | AttendanceNotification | GetUnreadByUserAsync |

---

## 🔄 Middleware Pipeline

### Pipeline Order (Program.cs)
```csharp
// 1. Global Exception Handler (catches all unhandled exceptions)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// 2. Request/Response Logging (audit trail)
app.UseMiddleware<RequestResponseLoggingMiddleware>();

// 3. Session Management
app.UseSession();

// 4. HTTPS Redirection
app.UseHttpsRedirection();

// 5. Routing
app.UseRouting();

// 6. Rate Limiting
app.UseRateLimiter();

// 7. Authorization
app.UseAuthorization();

// 8. Static Files
app.MapStaticAssets();

// 9. SignalR Hub
app.MapHub<AttendanceHub>("/attendanceHub");

// 10. Health Checks
app.MapHealthChecks("/health/checks");

// 11. Controllers
app.MapControllers();
app.MapControllerRoute(...);
```

### GlobalExceptionHandlerMiddleware
```csharp
// Catches and handles:
- InvalidOperationException → 400 Bad Request
- UnauthorizedAccessException → 401 Unauthorized
- ArgumentNullException → 400 Bad Request
- TimeoutException → 408 Request Timeout
- NpgsqlException → 500 Internal Server Error
- Default → 500 Internal Server Error

// Returns standardized JSON error response
{
  "success": false,
  "message": "Error description",
  "errorCode": "ERROR_CODE",
  "timestamp": "2025-11-26T10:30:00Z"
}
```

### RequestResponseLoggingMiddleware
```csharp
// Features:
- Request logging (method, path, headers, body)
- Response logging (status, timing, body)
- Slow request warnings (> 1000ms threshold)
- Sensitive data masking (passwords, tokens)
- Request ID correlation
- Excluded paths (static files, health checks)
```

---

## 📡 Real-time Communication

### SignalR Hub (AttendanceHub)
```csharp
public class AttendanceHub : Hub
{
    // Client Methods (called from server)
    - ReceiveAttendanceUpdate(data)    // New attendance recorded
    - ReceiveNotification(notification) // User notification
    - ReceiveScanResult(result)        // Scan feedback
    
    // Server Methods (called from client)
    - JoinClassGroup(classId)          // Subscribe to class updates
    - LeaveClassGroup(classId)         // Unsubscribe
    - JoinUserGroup(userId)            // Subscribe to user notifications
}
```

### Client Integration
```javascript
// signalr-connection.js
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/attendanceHub")
    .withAutomaticReconnect()
    .build();

connection.on("ReceiveAttendanceUpdate", (data) => {
    // Update UI with new attendance
    refreshAttendanceTable();
    showToast(`${data.studentName} - ${data.action}`);
});

connection.start();
```

---

## ⏰ Background Services

### AttendanceBackgroundService
```csharp
public class AttendanceBackgroundService : BackgroundService
{
    // Runs daily at 11:59 PM
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var targetTime = DateTime.Today.AddHours(23).AddMinutes(59);
            
            if (now > targetTime)
                targetTime = targetTime.AddDays(1);
            
            var delay = targetTime - now;
            await Task.Delay(delay, stoppingToken);
            
            // Process end-of-day tasks
            await _absenceService.ProcessAbsencesForDateAsync(DateTime.Today);
            await _missedTimeOutService.FlagIncompleteRecordsAsync(DateTime.Today);
        }
    }
}
```

---

## 🔒 Security Implementation

### Authentication Flow
```
1. User submits credentials (email/password or fingerprint)
2. Credentials validated against database
3. Password verified using BCrypt
4. Session created with user info
5. Session cookie sent to client
6. Subsequent requests include session cookie
7. RoleAuthorizeAttribute validates access
```

### RoleAuthorizeAttribute
```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly USER_ROLES[] _allowedRoles;
    
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userId = context.HttpContext.Session.GetInt32("UserId");
        var userRole = context.HttpContext.Session.GetInt32("UserRole");
        
        if (userId == null || userRole == null)
        {
            // Redirect to login
            context.Result = new RedirectToActionResult("Index", "Auth", null);
            return;
        }
        
        if (!_allowedRoles.Contains((USER_ROLES)userRole))
        {
            // Access denied
            context.Result = new RedirectToActionResult("AccessDenied", "Error", null);
        }
    }
}
```

### Rate Limiting Configuration
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;           // 5 attempts
        opt.Window = TimeSpan.FromMinutes(1);  // per minute
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;            // No queuing
    });
});
```

---

## 🖐 Fingerprint Integration

### ZKFinger SDK Integration
```
┌─────────────────────────────────────────────────────────────┐
│                    ZKFinger SDK (Native)                     │
│  libzkfpcsharp.dll → zkfp2 class → Native device calls      │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    FingerprintService                        │
│  - Wraps SDK calls with error handling                      │
│  - Manages device lifecycle                                  │
│  - Provides retry logic for captures                        │
│  - Converts templates to/from Base64                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                FingerprintMatchingService                    │
│  - Loads all templates from database                        │
│  - Compares captured template against each                  │
│  - Returns best match above threshold                       │
│  - Validates user role for attendance                       │
└─────────────────────────────────────────────────────────────┘
```

### Capture Flow
```
1. EnsureScannerReady()
   - Check device connection
   - Reconnect if needed
   
2. CaptureFingerprintWithImage()
   - Allocate buffers
   - Call zkfp2.AcquireFingerprint()
   - Retry on failure (max 3 attempts)
   - Return template + image data
   
3. Match against database
   - Load all fingerprint_templates
   - Call zkfp2.DBMatch() for each
   - Track best score
   - Return match if score >= threshold (50)
```

### Configuration (AttendanceSettings)
```json
{
  "FingerprintMatchThreshold": 50,    // Minimum match score
  "MaxCaptureRetries": 3,             // Retry attempts
  "CaptureRetryDelayMs": 300,         // Delay between retries
  "TemplateBufferSize": 2048,         // Buffer size in bytes
  "MinTemplateSize": 100,             // Minimum valid template
  "CaptureTimeoutSeconds": 10         // Capture timeout
}
```

---

## 🗄 Database Design

### Entity Relationship Diagram (Simplified)
```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  user_roles │     │    users    │     │   students  │
│─────────────│     │─────────────│     │─────────────│
│ role_id (PK)│◄────│ role_id (FK)│     │ stud_id (PK)│
│ role_name   │     │ user_id (PK)│◄────│ user_id (FK)│
└─────────────┘     │ user_email  │     │ stud_number │
                    │ user_pass   │     │ program_id  │
                    └─────────────┘     └──────┬──────┘
                           │                   │
                           ▼                   ▼
                    ┌─────────────┐     ┌─────────────┐
                    │   faculty   │     │  enrollment │
                    │─────────────│     │─────────────│
                    │ fac_id (PK) │     │ enroll_id   │
                    │ user_id (FK)│     │ stud_id (FK)│
                    │ dept_id (FK)│     │ class_id(FK)│
                    └──────┬──────┘     └──────┬──────┘
                           │                   │
                           ▼                   ▼
                    ┌─────────────┐     ┌─────────────┐
                    │   classes   │◄────│ attendance  │
                    │─────────────│     │─────────────│
                    │ class_id(PK)│     │ attend_id   │
                    │ course_id   │     │ class_id(FK)│
                    │ fac_id (FK) │     │ stud_id (FK)│
                    │ room_id     │     │ attend_date │
                    │ term_id     │     │ attend_status│
                    └─────────────┘     └─────────────┘
```

### Key Indexes
```sql
-- Performance indexes
CREATE INDEX idx_attendance_class_date ON attendance(class_id, attend_date);
CREATE INDEX idx_attendance_stud_date ON attendance(stud_id, attend_date);
CREATE INDEX idx_daily_attend_stud_class_date ON daily_attendance_log(stud_id, class_id, attend_date);
CREATE INDEX idx_enrollment_stud_id ON enrollment(stud_id);
CREATE INDEX idx_enrollment_class_id ON enrollment(class_id);
CREATE INDEX idx_fingerprint_user_id ON fingerprint_templates(user_id);
```

### Database Views
```sql
-- v_student_daily_schedule: Student's enrolled classes with schedules
-- v_today_attendance_summary: Today's attendance statistics
-- v_student_attendance_history: Historical attendance records
```

---

## ⚙️ Configuration Management

### Configuration Sources
1. `appsettings.json` - Base configuration
2. `appsettings.Development.json` - Development overrides
3. `.env` - Environment variables (database credentials)
4. `IOptions<T>` - Strongly-typed configuration

### Dependency Injection Registration
```csharp
// Singleton Services (shared across all requests)
builder.Services.AddSingleton<FingerprintService>();
builder.Services.AddSingleton<IFingerprintService>(sp => sp.GetRequiredService<FingerprintService>());

// Scoped Services (per-request)
builder.Services.AddScoped<FingerprintMatchingService>();
builder.Services.AddScoped<IFingerprintMatchingService>(sp => sp.GetRequiredService<FingerprintMatchingService>());
builder.Services.AddScoped<AttendanceProcessingService>();
builder.Services.AddScoped<DailyAttendanceService>();
// ... all repositories

// Configuration
builder.Services.Configure<AttendanceSettings>(
    builder.Configuration.GetSection(AttendanceSettings.SectionName));

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<FingerprintScannerHealthCheck>("scanner")
    .AddCheck<DatabaseHealthCheck>("database");
```

---

*Technical Architecture Document for Fingerprint Attendance System v1.0.0*
*Last updated: November 2025*
