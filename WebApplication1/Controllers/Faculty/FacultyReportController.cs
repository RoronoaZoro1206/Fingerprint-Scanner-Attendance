using System.Diagnostics;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Faculty;

[RoleAuthorize(USER_ROLES.FACULTY)]
public class FacultyReportController : Controller
{
    private readonly ILogger<FacultyReportController> _logger;
    private readonly IFacultyRepository _facultyRepository;
    private readonly IAttendanceRepository _attendanceRepository;

    public FacultyReportController(
        ILogger<FacultyReportController> logger,
        IFacultyRepository facultyRepository,
        IAttendanceRepository attendanceRepository)
    {
        _logger = logger;
        _facultyRepository = facultyRepository;
        _attendanceRepository = attendanceRepository;
    }

    public IActionResult Records()
    {
        return View("~/Views/Faculty/FacultyReports.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> GetReportData()
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation($"GetReportData - UserId from session: {userId}");

            if (!userId.HasValue)
            {
                _logger.LogWarning("GetReportData - No userId in session");
                return Json(new { success = false, message = "User not authenticated" });
            }

            var faculty = await _facultyRepository.GetByUserIdAsync(userId.Value);

            if (faculty == null)
            {
                _logger.LogWarning($"GetReportData - No faculty found for userId: {userId.Value}");
                return Json(new { success = false, message = "Faculty profile not found" });
            }

            _logger.LogInformation($"GetReportData - Faculty: {faculty.FacId}");

            // Get statistics and records with optimized queries
            var stats = await _attendanceRepository.GetFacultyReportStatsAsync(faculty.FacId);
            var records = await _attendanceRepository.GetFacultyAttendanceRecordsAsync(faculty.FacId, 20);

            var result = new
            {
                statistics = new
                {
                    averageAttendance = stats.AverageAttendance,
                    lateArrivalsToday = stats.LateArrivalsToday,
                    absentStudents = stats.AbsentStudents,
                    totalStudents = stats.TotalStudents
                },
                records = records.Select(r => new
                {
                    date = r.Date,
                    courseCode = r.CourseCode,
                    present = r.Present,
                    late = r.Late,
                    absent = r.Absent,
                    excused = r.Excused
                })
            };

            _logger.LogInformation($"GetReportData - Returning stats and {records.Count()} records");
            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting report data: {ex.Message}");
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
