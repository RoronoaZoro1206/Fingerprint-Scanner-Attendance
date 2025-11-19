using DotNetEnv;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Repositories.Implementations;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Fingerprint_Attendance_System.Middleware; // ? NEW: For GlobalExceptionHandlerMiddleware

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
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
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

// Add FingerprintService as Singleton (shared across requests for device management)
builder.Services.AddSingleton<FingerprintService>();

var app = builder.Build();

// ============================================================
// ADD GLOBAL EXCEPTION HANDLER MIDDLEWARE
// ============================================================
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

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

app.UseAuthorization();

app.MapStaticAssets();

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
