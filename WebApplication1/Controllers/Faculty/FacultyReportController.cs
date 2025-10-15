using System.Diagnostics;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Faculty;

public class FacultyReportController : Controller
{
    private readonly ILogger<FacultyReportController> _logger;
    private readonly IFacultyRepository _facultyRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IClassRepository _classRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public FacultyReportController(
        ILogger<FacultyReportController> logger,
        IFacultyRepository facultyRepository,
        IAttendanceRepository attendanceRepository,
        IClassRepository classRepository,
        ICourseRepository courseRepository,
        IEnrollmentRepository enrollmentRepository)
    {
        _logger = logger;
        _facultyRepository = facultyRepository;
        _attendanceRepository = attendanceRepository;
        _classRepository = classRepository;
        _courseRepository = courseRepository;
        _enrollmentRepository = enrollmentRepository;
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

            // Get all classes taught by this faculty
            var facultyClasses = await _classRepository.GetClassesByFacultyIdAsync(faculty.FacId);
            var classIds = facultyClasses.Select(c => c.ClassId).ToList();

            // Get all attendance records for these classes
            var allAttendances = await _attendanceRepository.GetAllAsync();
            var classAttendances = allAttendances
                .Where(a => a.ClassId.HasValue && classIds.Contains(a.ClassId.Value))
                .OrderByDescending(a => a.AttendDate)
                .ThenByDescending(a => a.AttendScanTime)
                .ToList();

            _logger.LogInformation($"GetReportData - Found {classAttendances.Count} attendance records");

            // Calculate statistics - group by date and class to count unique sessions
            var uniqueSessions = classAttendances
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
            
            // Get total enrolled students across all classes
            var allEnrollments = await _enrollmentRepository.GetAllAsync();
            var totalStudents = allEnrollments
                .Where(e => classIds.Contains(e.ClassId))
                .Select(e => e.StudId)
                .Distinct()
                .Count();

            var totalSessions = uniqueSessions.Count;
            var attendanceRate = totalSessions > 0 
                ? (int)Math.Round((double)(totalPresent + totalLate) / totalSessions * 100) 
                : 0;

            // Get recent attendance records with course info
            var courses = await _courseRepository.GetAllAsync();

            // Group by date and class to get attendance summary
            var attendanceRecords = new List<object>();
            var groupedAttendances = classAttendances
                .GroupBy(a => new { a.AttendDate, a.ClassId })
                .Take(20); // Get recent 20 records

            foreach (var group in groupedAttendances)
            {
                var classItem = facultyClasses.FirstOrDefault(c => c.ClassId == group.Key.ClassId);
                var course = classItem != null ? courses.FirstOrDefault(c => c.CourseId == classItem.CourseId) : null;

                // Group by student to count each student only once per session
                // Use the status from Time In record, or first available record
                var studentStatuses = group
                    .GroupBy(a => a.StudId)
                    .Select(studentGroup => 
                    {
                        // Prefer Time In status, otherwise use first record
                        var timeInRecord = studentGroup.FirstOrDefault(a => a.AttendType == ScanType.In);
                        var status = timeInRecord?.AttendStatus ?? studentGroup.First().AttendStatus;
                        return status;
                    })
                    .ToList();

                // Count unique students by status
                var presentCount = studentStatuses.Count(s => s == AttendanceStatus.Present);
                var lateCount = studentStatuses.Count(s => s == AttendanceStatus.Late);
                var absentCount = studentStatuses.Count(s => s == AttendanceStatus.Absent);
                var excusedCount = studentStatuses.Count(s => s == AttendanceStatus.Excused);

                attendanceRecords.Add(new
                {
                    date = group.Key.AttendDate.ToString("yyyy-MM-dd"),
                    courseCode = course?.CourseCode ?? "N/A",
                    present = presentCount,
                    late = lateCount,
                    absent = absentCount,
                    excused = excusedCount
                });
            }

            var result = new
            {
                statistics = new
                {
                    averageAttendance = attendanceRate,
                    lateArrivalsToday = totalLate,
                    absentStudents = totalAbsent,
                    totalStudents = totalStudents
                },
                records = attendanceRecords
            };

            _logger.LogInformation($"GetReportData - Returning stats and {attendanceRecords.Count} records");
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
