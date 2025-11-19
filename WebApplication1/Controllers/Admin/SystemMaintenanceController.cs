using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

[RoleAuthorize(USER_ROLES.ADMIN)]
public class SystemMaintenanceController : Controller
{
    private readonly ILogger<SystemMaintenanceController> _logger;
    private readonly ProfilePictureMigrationService _migrationService;
    private readonly AdminAuditService _auditService;

    public SystemMaintenanceController(
        ILogger<SystemMaintenanceController> logger,
        ProfilePictureMigrationService migrationService,
        AdminAuditService auditService)
    {
        _logger = logger;
        _migrationService = migrationService;
        _auditService = auditService;
    }

    public IActionResult Index()
    {
        return View("~/Views/Admin/SystemMaintenance.cshtml");
    }

    /// <summary>
    /// Run profile picture migration
    /// Converts shared images to user-specific images
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> MigrateProfilePictures()
    {
        try
        {
            _logger.LogInformation("Starting profile picture migration from admin request");

            var result = await _migrationService.MigrateProfilePictures();

            // Log audit
            await _auditService.LogActionAsync(
                "Profile Picture Migration",
                $"Ran profile picture migration. Result: {result.Message}, Migrated: {result.UsersMigrated}, Failed: {result.UsersFailed}"
            );

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                data = new
                {
                    totalUsers = result.TotalUsers,
                    migrated = result.UsersMigrated,
                    alreadyMigrated = result.UsersAlreadyMigrated,
                    setToDefault = result.UsersSetToDefault,
                    failed = result.UsersFailed,
                    failedUsers = result.FailedUsers
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running profile picture migration");
            return Json(new
            {
                success = false,
                message = $"Migration failed: {ex.Message}"
            });
        }
    }
}
