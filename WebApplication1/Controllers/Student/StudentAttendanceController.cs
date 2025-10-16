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
    private readonly IClassRepository _classRepository;
    private readonly ICourseRepository _courseRepository;

    public StudentAttendanceController(
        ILogger<StudentAttendanceController> logger,
        IStudentRepository studentRepository,
        IAttendanceRepository attendanceRepository,
        IClassRepository classRepository,
        ICourseRepository courseRepository)
    {
        _logger = logger;
        _studentRepository = studentRepository;
        _attendanceRepository = attendanceRepository;
        _classRepository = classRepository;
        _courseRepository = courseRepository;
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

            var student = (await _studentRepository.GetAllAsync())
                .FirstOrDefault(s => s.UserId == userId.Value);

            if (student == null)
            {
                _logger.LogWarning($"GetAttendanceData - No student found for userId: {userId.Value}");
                return Json(new { success = false, message = "Student profile not found" });
            }

            _logger.LogInformation($"GetAttendanceData - Student: {student.StudId}");

            var allAttendances = await _attendanceRepository.GetAllAsync();
            var studentAttendances = allAttendances
                .Where(a => a.StudId == student.StudId)
                .OrderByDescending(a => a.AttendDate)
                .ThenByDescending(a => a.AttendScanTime)
                .ToList();

            _logger.LogInformation($"GetAttendanceData - Found {studentAttendances.Count} attendance records");

            // Calculate statistics - group by date and class to count unique sessions
            var uniqueSessions = studentAttendances
                .GroupBy(a => new { a.AttendDate, a.ClassId })
                .Select(g => new
                {
                    Date = g.Key.AttendDate,
                    ClassId = g.Key.ClassId,
                    Status = g.FirstOrDefault(a => a.AttendType == ScanType.In)?.AttendStatus 
                             ?? g.FirstOrDefault()?.AttendStatus 
                             ?? AttendanceStatus.Pending
                })
                .ToList();

            var totalPresent = uniqueSessions.Count(s => s.Status == AttendanceStatus.Present);
            var totalLate = uniqueSessions.Count(s => s.Status == AttendanceStatus.Late);
            var totalAbsent = uniqueSessions.Count(s => s.Status == AttendanceStatus.Absent);
            var totalSessions = uniqueSessions.Count;
            var attendanceRate = totalSessions > 0 
                ? (int)Math.Round((double)(totalPresent + totalLate) / totalSessions * 100) 
                : 0;

            // Get recent attendance records with course info
            var classes = await _classRepository.GetAllAsync();
            var courses = await _courseRepository.GetAllAsync();

            // Group by date and class to get time in/out pairs
            var attendanceRecords = new List<object>();
            var groupedAttendances = studentAttendances
                .GroupBy(a => new { a.AttendDate, a.ClassId })
                .Take(20); // Get recent 20 records

            foreach (var group in groupedAttendances)
            {
                var classItem = classes.FirstOrDefault(c => c.ClassId == group.Key.ClassId);
                var course = classItem != null ? courses.FirstOrDefault(c => c.CourseId == classItem.CourseId) : null;

                var timeIn = group.FirstOrDefault(a => a.AttendType == ScanType.In);
                var timeOut = group.FirstOrDefault(a => a.AttendType == ScanType.Out);

                // Use the status from time in, or if not available, from time out
                var status = timeIn?.AttendStatus ?? timeOut?.AttendStatus ?? AttendanceStatus.Pending;

                attendanceRecords.Add(new
                {
                    date = group.Key.AttendDate.ToString("yyyy-MM-dd"),
                    courseCode = course?.CourseCode ?? "N/A",
                    status = status.ToString(),
                    timeIn = timeIn?.AttendScanTime.ToString("hh:mm:ss tt") ?? "-",
                    timeOut = timeOut?.AttendScanTime.ToString("hh:mm:ss tt") ?? "-"
                });
            }

            var result = new
            {
                statistics = new
                {
                    classesAttended = totalPresent,
                    lateArrivals = totalLate,
                    absences = totalAbsent,
                    attendanceRate = attendanceRate
                },
                records = attendanceRecords
            };

            _logger.LogInformation($"GetAttendanceData - Returning stats and {attendanceRecords.Count} records");
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
