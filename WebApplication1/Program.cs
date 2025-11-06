using DotNetEnv;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Repositories.Implementations;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;

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
builder.Services.AddControllersWithViews();
builder.Services.AddControllers(); // Enable API controllers

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

// Add FingerprintService as Singleton (shared across requests for device management)
builder.Services.AddSingleton<FingerprintService>();

var app = builder.Build();

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


app.Run();