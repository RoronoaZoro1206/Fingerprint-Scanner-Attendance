using DotNetEnv;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Repositories.Implementations;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Fingerprint_Attendance_System.Middleware; // ? NEW: For GlobalExceptionHandlerMiddleware
using Fingerprint_Attendance_System.Hubs; // ✅ NEW: For SignalR AttendanceHub
using System.Threading.RateLimiting; // ✅ NEW: For Rate Limiting
using Microsoft.AspNetCore.RateLimiting; // ✅ NEW: For Rate Limiting

var builder = WebApplication.CreateBuilder(args);

// Load .env file
Env.Load();

// Build connection string from environment variables
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ;
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ;
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ;
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ;
var dbPass = Environment.GetEnvironmentVariable("DB_PASS") ;

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass}";

// Override the connection string in configuration
builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // ✅ FIX: Use PascalCase for JSON serialization (matches C# property names)
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = 
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// ============================================================
// ✅ STEP 3: REGISTER SIGNALR FOR REAL-TIME ATTENDANCE NOTIFICATIONS
// ============================================================
builder.Services.AddSignalR(options =>
{
    // Enable detailed error messages in development
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    
    // Configure timeout settings
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(1);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

builder.Services.AddSession(options =>
{
    // ✅ FIX: Increased session timeout to 24 hours to prevent inactivity-based logout
    // Users should only be logged out when they:
    // 1. Click the logout button (manual logout)
    // 2. Close the browser/tab (auto logout detection)
    // NOT when they:
    // - Are inactive/AFK
    // - Have been logged in for a long time
    // - Navigate between pages
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSingleton<DatabaseHelper>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAdministrationRepository, AdministrationRepository>();
builder.Services.AddScoped<IAdministrationAuditRepository, AdministrationAuditRepository>();
builder.Services.AddScoped<IAcademicTermRepository, AcademicTermRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IProgramRepository, ProgramRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IFacultyRepository, FacultyRepository>();
builder.Services.AddScoped<IStudentIdPoolRepository, StudentIdPoolRepository>();
builder.Services.AddScoped<ISectionRepository, SectionRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IClassRepository, ClassRepository>();
builder.Services.AddScoped<IClassScheduleRepository, ClassScheduleRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IEnrollmentBlockRepository, EnrollmentBlockRepository>(); // ✅ NEW: Enrollment blocks for force-unenroll
builder.Services.AddScoped<IFingerprintTemplateRepository, FingerprintTemplateRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IScanLogRepository, ScanLogRepository>();
builder.Services.AddScoped<IAdminOverviewRepository, AdminOverviewRepository>();
builder.Services.AddScoped<IAuthenticationAuditRepository, AuthenticationAuditRepository>();
builder.Services.AddScoped<IDailyAttendanceLogRepository, DailyAttendanceLogRepository>();
builder.Services.AddScoped<IAttendanceNotificationRepository, AttendanceNotificationRepository>(); // ✅ NEW: Attendance notifications

// ? TODO: Faculty Attendance Management Repositories (Phase 1)
// Uncomment after creating repository implementations:
// builder.Services.AddScoped<IStudentExcuseRequestRepository, StudentExcuseRequestRepository>();
// builder.Services.AddScoped<IFacultyAttendanceActionRepository, FacultyAttendanceActionRepository>();

// ? NEW: Add Authentication Audit Service
builder.Services.AddScoped<AuthenticationAuditService>();

// ✅ NEW: Session Heartbeat Tracking for Auto-Logout Detection
// This service tracks active sessions via heartbeats and detects when tabs/browsers are closed
builder.Services.AddSingleton<SessionHeartbeatService>();
builder.Services.AddHostedService<SessionCleanupBackgroundService>();

// Add SuperAdminInitializer service
builder.Services.AddScoped<SuperAdminInitializer>();

// Add HttpContextAccessor for session access in services
builder.Services.AddHttpContextAccessor();

// Add Audit Services (both legacy and universal)
builder.Services.AddScoped<AdminAuditService>();
builder.Services.AddScoped<UniversalAuditService>(); // ? NEW: Universal audit service for all roles

// Add AcademicTermService
builder.Services.AddScoped<AcademicTermService>();

// Add DailyAttendanceService for Time In/Time Out toggle logic
builder.Services.AddScoped<DailyAttendanceService>();

// ✅ NEW: Profile Picture Migration Service
builder.Services.AddScoped<ProfilePictureMigrationService>();

// ? NEW: Phase 4 - Schedule Integration Services
builder.Services.AddScoped<LateDetectionService>();
builder.Services.AddScoped<AbsenceDetectionService>();

// ============================================================
// PHASE 5: ERROR HANDLING & EDGE CASES
// ============================================================
builder.Services.AddScoped<MissedTimeOutService>();

// ? NEW: Background service for automatic absence detection at 11:59 PM daily
builder.Services.AddHostedService<AttendanceBackgroundService>();

// ============================================================
// PHASE 6: API/SERVICE LAYER
// ============================================================
builder.Services.AddScoped<AttendanceProcessingService>();

// Enable API documentation (Swagger/OpenAPI)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Attendance API", 
        Version = "v1",
        Description = "RESTful API for fingerprint-based attendance system with Time In/Time Out management",
        Contact = new()
        {
            Name = "Support Team"
        }
    });
    
    // Enable XML documentation comments (if you add <GenerateDocumentationFile>true</GenerateDocumentationFile> to .csproj)
    // var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    // var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    // c.IncludeXmlComments(xmlPath);
});

// ============================================================
// ✅ PHILIPPINE TIMEZONE SERVICE (CRITICAL FOR ATTENDANCE)
// Ensures all datetime operations use PHT (UTC+8) regardless of server timezone
// ============================================================
builder.Services.AddSingleton<PhilippineTimeService>();

// Add FingerprintService as Singleton (shared across requests for device management)
// Register both concrete type and interface for DI flexibility
builder.Services.AddSingleton<FingerprintService>();
builder.Services.AddSingleton<Fingerprint_Attendance_System.Services.Interfaces.IFingerprintService>(
    sp => sp.GetRequiredService<FingerprintService>());

// ============================================================
// ✅ NEW: ATTENDANCE SETTINGS & FINGERPRINT MATCHING SERVICE
// Eliminates hardcoded magic numbers and duplicate code
// ============================================================
builder.Services.Configure<AttendanceSettings>(
    builder.Configuration.GetSection(AttendanceSettings.SectionName));
builder.Services.AddScoped<FingerprintMatchingService>();
builder.Services.AddScoped<Fingerprint_Attendance_System.Services.Interfaces.IFingerprintMatchingService>(
    sp => sp.GetRequiredService<FingerprintMatchingService>());

// ============================================================
// ✅ NEW: HEALTH CHECKS FOR MONITORING
// Supports Kubernetes/Docker probes, load balancers, and dashboards
// ============================================================
builder.Services.AddHealthChecks()
    .AddCheck<FingerprintScannerHealthCheck>("scanner", tags: new[] { "scanner", "device" })
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "database", "db" });

// ============================================================
// ✅ NEW: RATE LIMITING FOR BRUTE FORCE PROTECTION
// ============================================================
builder.Services.AddRateLimiter(options =>
{
    // Rate limiter for login endpoints - 5 attempts per 1 minute window
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0; // No queuing - reject immediately when limit reached
    });
    
    // Custom response when rate limit is exceeded
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
            ? retryAfterValue.TotalSeconds
            : 60;
        
        context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString();
        
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            success = false,
            errors = new[] { $"Too many login attempts. Please wait {retryAfter:F0} seconds before trying again." }
        }, cancellationToken);
    };
});

var app = builder.Build();

// ============================================================
// ADD GLOBAL EXCEPTION HANDLER MIDDLEWARE
// ============================================================
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// ============================================================
// ✅ NEW: ADD REQUEST/RESPONSE LOGGING MIDDLEWARE
// Provides audit trail, performance metrics, and debugging capabilities
// ============================================================
app.UseMiddleware<RequestResponseLoggingMiddleware>(new RequestLoggingOptions
{
    LogRequestBody = true,
    LogResponseBody = false, // Set to true for detailed debugging
    SlowRequestThresholdMs = 1000,
    MaxBodyLogSize = 4096
});

// Initialize super admin account
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
      var initializer = services.GetRequiredService<SuperAdminInitializer>();
        await initializer.InitializeAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
   logger.LogError(ex, "An error occurred while initializing super admin account.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession();
app.UseHttpsRedirection();
app.UseRouting();

// ✅ NEW: Enable Rate Limiting middleware
app.UseRateLimiter();

app.UseAuthorization();

app.MapStaticAssets();

// ============================================================
// ✅ STEP 3: MAP SIGNALR HUB ENDPOINT
// ============================================================
app.MapHub<AttendanceHub>("/attendanceHub");

// ============================================================
// ✅ NEW: MAP HEALTH CHECK ENDPOINTS
// Supports Kubernetes probes, load balancers, and monitoring
// ============================================================
app.MapHealthChecks("/health/checks", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data
            })
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

// CRITICAL FIX: Map API controller routes FIRST (attribute-based routing)
// This ensures /api/* routes are handled by API controllers, not MVC convention routing
app.MapControllers();

// Then map MVC routes (convention-based routing)
app.MapControllerRoute(
      name: "default",
 pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Attendance API v1");
        c.RoutePrefix = "api/docs"; // Access at https://localhost:7048/api/docs
        c.DocumentTitle = "Attendance API Documentation";
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None); // Collapse all by default
    });
}

app.Run();
