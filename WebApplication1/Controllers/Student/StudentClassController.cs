using System.Diagnostics;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Student;

[RoleAuthorize(USER_ROLES.STUDENT)]
public class StudentClassController : Controller
{
    private readonly ILogger<StudentClassController> _logger;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IClassRepository _classRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IFacultyRepository _facultyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IClassScheduleRepository _classScheduleRepository;
    private readonly IAttendanceRepository _attendanceRepository;

    public StudentClassController(
        ILogger<StudentClassController> logger,
        IEnrollmentRepository enrollmentRepository,
        IStudentRepository studentRepository,
        IClassRepository classRepository,
        ICourseRepository courseRepository,
        IRoomRepository roomRepository,
        IFacultyRepository facultyRepository,
        IUserRepository userRepository,
        IClassScheduleRepository classScheduleRepository,
        IAttendanceRepository attendanceRepository)
    {
        _logger = logger;
        _enrollmentRepository = enrollmentRepository;
        _studentRepository = studentRepository;
        _classRepository = classRepository;
        _courseRepository = courseRepository;
        _roomRepository = roomRepository;
        _facultyRepository = facultyRepository;
        _userRepository = userRepository;
        _classScheduleRepository = classScheduleRepository;
        _attendanceRepository = attendanceRepository;
    }

    public IActionResult Classes()
    {
        return View("~/Views/Student/StudentClasses.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> GetMyClasses()
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation($"GetMyClasses - UserId from session: {userId}");

            if (!userId.HasValue)
            {
                _logger.LogWarning("GetMyClasses - No userId in session");
                return Json(new { success = false, message = "User not authenticated" });
            }

            var student = await _studentRepository.GetByUserIdAsync(userId.Value);

            if (student == null)
            {
                _logger.LogWarning($"GetMyClasses - No student found for userId: {userId.Value}");
                return Json(new { success = false, message = "Student profile not found" });
            }

            _logger.LogInformation($"GetMyClasses - Student: {student.StudId}");

            var classList = await _enrollmentRepository.GetStudentClassesAsync(student.StudId);

            var result = classList.Select(c => new
            {
                classId = c.ClassId,
                courseCode = c.CourseCode,
                courseName = c.CourseName,
                courseUnits = c.CourseUnits,
                facultyName = c.FacultyName,
                schedule = c.Schedule,
                roomCode = c.RoomCode,
                attendanceRate = c.AttendanceRate
            });

            _logger.LogInformation($"GetMyClasses - Returning {result.Count()} classes");
            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting my classes: {ex.Message}");
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
