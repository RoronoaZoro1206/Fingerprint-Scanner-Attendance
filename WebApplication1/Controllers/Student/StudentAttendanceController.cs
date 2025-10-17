using System.Diagnostics;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Student;

[RoleAuthorize(USER_ROLES.STUDENT)]
public class StudentAttendanceController : Controller
{
    private readonly ILogger<StudentAttendanceController> _logger;
    private readonly IStudentRepository _studentRepository;
    private readonly IAttendanceRepository _attendanceRepository;

    public StudentAttendanceController(
        ILogger<StudentAttendanceController> logger,
        IStudentRepository studentRepository,
        IAttendanceRepository attendanceRepository)
    {
        _logger = logger;
        _studentRepository = studentRepository;
        _attendanceRepository = attendanceRepository;
    }

    public IActionResult Attendance()
    {
        return View("~/Views/Student/StudentAttendance.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> GetAttendanceData()
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation($"GetAttendanceData - UserId from session: {userId}");

            if (!userId.HasValue)
            {
                _logger.LogWarning("GetAttendanceData - No userId in session");
                return Json(new { success = false, message = "User not authenticated" });
            }

            var student = await _studentRepository.GetByUserIdAsync(userId.Value);

            if (student == null)
            {
                _logger.LogWarning($"GetAttendanceData - No student found for userId: {userId.Value}");
                return Json(new { success = false, message = "Student profile not found" });
            }

            _logger.LogInformation($"GetAttendanceData - Student: {student.StudId}");

            // Use optimized repository method - single query instead of multiple
            var attendanceData = await _attendanceRepository.GetStudentAttendanceDataAsync(student.StudId, 20);

            var result = new
            {
                statistics = new
                {
                    classesAttended = attendanceData.Statistics.ClassesAttended,
                    lateArrivals = attendanceData.Statistics.LateArrivals,
                    absences = attendanceData.Statistics.Absences,
                    attendanceRate = attendanceData.Statistics.AttendanceRate
                },
                records = attendanceData.Records.Select(r => new
                {
                    date = r.Date,
                    courseCode = r.CourseCode,
                    status = r.Status,
                    timeIn = r.TimeIn,
                    timeOut = r.TimeOut
                })
            };

            _logger.LogInformation($"GetAttendanceData - Returning stats and {attendanceData.Records.Count} records");
            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting attendance data: {ex.Message}");
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
