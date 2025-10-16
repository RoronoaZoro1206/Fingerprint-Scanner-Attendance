using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers;

[RoleAuthorize(USER_ROLES.ADMIN, USER_ROLES.FACULTY, USER_ROLES.STUDENT)]
public class SharedDataController : Controller
{
    private readonly AcademicTermService _academicTermService;
    private readonly ILogger<SharedDataController> _logger;

    public SharedDataController(
        AcademicTermService academicTermService,
        ILogger<SharedDataController> logger)
    {
        _academicTermService = academicTermService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveAcademicTerm()
    {
        try
        {
            _logger.LogInformation("GetActiveAcademicTerm called");
            
            var termName = await _academicTermService.GetActiveAcademicTermNameAsync();
            
            _logger.LogInformation($"Term name retrieved: {termName ?? "null"}");

            if (!string.IsNullOrEmpty(termName))
            {
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        termName = termName
                    }
                });
            }

            _logger.LogWarning("No active academic term found");
            return Json(new { success = false, message = "No active academic term" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active academic term");
            return Json(new { success = false, message = ex.Message });
        }
    }
}
