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

            var student = (await _studentRepository.GetAllAsync())
                .FirstOrDefault(s => s.UserId == userId.Value);

            if (student == null)
            {
                _logger.LogWarning($"GetMyClasses - No student found for userId: {userId.Value}");
                return Json(new { success = false, message = "Student profile not found" });
            }

            _logger.LogInformation($"GetMyClasses - Student: {student.StudId}");

            var enrollments = await _enrollmentRepository.GetByStudentIdAsync(student.StudId);
            var classes = await _classRepository.GetAllAsync();
            var courses = await _courseRepository.GetAllAsync();
            var rooms = await _roomRepository.GetAllAsync();
            var faculties = await _facultyRepository.GetAllAsync();
            var users = await _userRepository.GetAllAsync();
            var schedules = await _classScheduleRepository.GetAllAsync();
            var allAttendances = await _attendanceRepository.GetAllAsync();

            _logger.LogInformation($"GetMyClasses - Found {enrollments.Count()} enrollments");

            var classList = new List<object>();

            foreach (var enrollment in enrollments)
            {
                var classItem = classes.FirstOrDefault(c => c.ClassId == enrollment.ClassId);
                if (classItem == null) continue;

                var course = courses.FirstOrDefault(c => c.CourseId == classItem.CourseId);
                var room = rooms.FirstOrDefault(r => r.RoomId == classItem.RoomId);
                var faculty = faculties.FirstOrDefault(f => f.FacId == classItem.FacId);
                var facultyUser = faculty != null ? users.FirstOrDefault(u => u.UserId == faculty.UserId) : null;

                var classSchedules = schedules.Where(s => s.ClassId == classItem.ClassId).ToList();
                var scheduleText = string.Join(", ", classSchedules.Select(s =>
                {
                    var startTime = DateTime.Today.Add(s.SchedStartTime).ToString("hh:mm tt");
                    var endTime = DateTime.Today.Add(s.SchedEndTime).ToString("hh:mm tt");
                    return $"{s.SchedDayOfWeek} {startTime}-{endTime}";
                }));

                // Calculate attendance rate
                var classAttendances = allAttendances.Where(a => a.ClassId == classItem.ClassId && a.StudId == student.StudId).ToList();
                var totalSessions = classAttendances.Count();
                var presentSessions = classAttendances.Count(a => a.AttendStatus == AttendanceStatus.Present);
                var attendanceRate = totalSessions > 0 ? (int)Math.Round((double)presentSessions / totalSessions * 100) : 0;

                classList.Add(new
                {
                    classId = classItem.ClassId,
                    courseCode = course?.CourseCode ?? "N/A",
                    courseName = course?.CourseName ?? "N/A",
                    courseUnits = course?.CourseUnits ?? 0,
                    facultyName = facultyUser != null ? $"{facultyUser.UserFirstName} {facultyUser.UserLastName}" : "N/A",
                    schedule = scheduleText,
                    roomCode = room?.RoomCode ?? "N/A",
                    attendanceRate = attendanceRate
                });
            }

            _logger.LogInformation($"GetMyClasses - Returning {classList.Count} classes");
            return Json(new { success = true, data = classList });
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
