using System.Diagnostics;
using System.Security.Claims;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Student;

public class StudentEnrollmentController : Controller
{
    private readonly ILogger<StudentEnrollmentController> _logger;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IClassRepository _classRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IFacultyRepository _facultyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IClassScheduleRepository _classScheduleRepository;
    private readonly IProgramRepository _programRepository;

    public StudentEnrollmentController(
        ILogger<StudentEnrollmentController> logger,
        IEnrollmentRepository enrollmentRepository,
        IStudentRepository studentRepository,
        IClassRepository classRepository,
        ICourseRepository courseRepository,
        IRoomRepository roomRepository,
        IFacultyRepository facultyRepository,
        IUserRepository userRepository,
        IClassScheduleRepository classScheduleRepository,
        IProgramRepository programRepository)
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
        _programRepository = programRepository;
    }

    public IActionResult Enrollment()
    {
        return View("~/Views/Student/StudentEnrollment.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> GetStudentInfo()
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation($"GetStudentInfo - UserId from session: {userId}");
            
            if (!userId.HasValue || userId.Value == 0)
            {
                _logger.LogWarning("GetStudentInfo - User not authenticated");
                return Json(new { success = false, message = "User not authenticated" });
            }

            var student = (await _studentRepository.GetAllAsync())
                .FirstOrDefault(s => s.UserId == userId.Value);

            if (student == null)
            {
                _logger.LogWarning($"GetStudentInfo - No student found for userId: {userId.Value}");
                return Json(new { success = false, message = "Student profile not found. Please complete your application first." });
            }

            if (!student.ProgramId.HasValue)
            {
                _logger.LogWarning($"GetStudentInfo - Student {student.StudId} has no program assigned");
                return Json(new { success = false, message = "Please complete your student application to enroll in classes." });
            }

            var user = await _userRepository.GetByIdAsync(userId.Value);
            var program = await _programRepository.GetByIdAsync(student.ProgramId.Value);

            _logger.LogInformation($"GetStudentInfo - Student: {student.StudId}, Program: {program?.ProgramName}");

            return Json(new
            {
                success = true,
                data = new
                {
                    studId = student.StudId,
                    studNumber = student.StudNumber,
                    fullName = $"{user?.UserFirstName} {user?.UserLastName}",
                    programId = student.ProgramId,
                    programName = program?.ProgramName ?? "Unknown Program",
                    yearLevel = student.StudYearLevel,
                    hasProfile = true
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting student info: {ex.Message}");
            return Json(new { success = false, message = $"Failed to load student information: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrentEnrollments()
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation($"GetCurrentEnrollments - UserId from session: {userId}");
            
            if (!userId.HasValue)
            {
                _logger.LogWarning("GetCurrentEnrollments - No userId in session");
                return Json(new { success = false, data = new List<object>(), message = "User not authenticated" });
            }
            
            var student = (await _studentRepository.GetAllAsync())
                .FirstOrDefault(s => s.UserId == userId.Value);

            if (student == null)
            {
                _logger.LogWarning($"GetCurrentEnrollments - No student found for userId: {userId.Value}");
                return Json(new { success = false, data = new List<object>(), message = "Student profile not found" });
            }

            _logger.LogInformation($"GetCurrentEnrollments - Student: {student.StudId}");

            var enrollments = await _enrollmentRepository.GetByStudentIdAsync(student.StudId);
            var classes = await _classRepository.GetAllAsync();
            var courses = await _courseRepository.GetAllAsync();
            var rooms = await _roomRepository.GetAllAsync();
            var faculties = await _facultyRepository.GetAllAsync();
            var users = await _userRepository.GetAllAsync();
            var schedules = await _classScheduleRepository.GetAllAsync();

            _logger.LogInformation($"GetCurrentEnrollments - Found {enrollments.Count()} enrollments");

            var enrollmentList = new List<object>();

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

                enrollmentList.Add(new
                {
                    enrollId = enrollment.EnrollId,
                    classId = classItem.ClassId,
                    courseCode = course?.CourseCode ?? "N/A",
                    courseName = course?.CourseName ?? "N/A",
                    facultyName = facultyUser != null ? $"{facultyUser.UserFirstName} {facultyUser.UserLastName}" : "N/A",
                    schedule = scheduleText,
                    roomCode = room?.RoomCode ?? "N/A"
                });
            }

            _logger.LogInformation($"GetCurrentEnrollments - Returning {enrollmentList.Count} enrollments");
            return Json(new { success = true, data = enrollmentList });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting current enrollments: {ex.Message}");
            return Json(new { success = false, data = new List<object>(), message = $"Error: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableClasses()
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation($"GetAvailableClasses - UserId from session: {userId}");
            
            if (!userId.HasValue)
            {
                _logger.LogWarning("GetAvailableClasses - No userId in session");
                return Json(new { success = false, data = new List<object>(), message = "User not authenticated" });
            }
            
            var student = (await _studentRepository.GetAllAsync())
                .FirstOrDefault(s => s.UserId == userId.Value);

            if (student == null)
            {
                _logger.LogWarning($"GetAvailableClasses - No student found for userId: {userId.Value}");
                return Json(new { success = false, data = new List<object>(), message = "Student profile not found" });
            }

            if (!student.ProgramId.HasValue)
            {
                _logger.LogWarning($"GetAvailableClasses - Student {student.StudId} has no program assigned");
                return Json(new { success = false, data = new List<object>(), message = "No program assigned" });
            }

            _logger.LogInformation($"GetAvailableClasses - Student: {student.StudId}, Program: {student.ProgramId}");

            var enrollments = await _enrollmentRepository.GetByStudentIdAsync(student.StudId);
            var enrolledClassIds = enrollments.Select(e => e.ClassId).ToHashSet();

            var classes = await _classRepository.GetAllAsync();
            var courses = await _courseRepository.GetAllAsync();
            var rooms = await _roomRepository.GetAllAsync();
            var faculties = await _facultyRepository.GetAllAsync();
            var users = await _userRepository.GetAllAsync();
            var schedules = await _classScheduleRepository.GetAllAsync();

            _logger.LogInformation($"GetAvailableClasses - Total classes: {classes.Count()}, Total courses: {courses.Count()}");

            var availableClasses = new List<object>();

            foreach (var classItem in classes)
            {
                if (enrolledClassIds.Contains(classItem.ClassId))
                    continue;

                var course = courses.FirstOrDefault(c => c.CourseId == classItem.CourseId);
                
                // Filter by student's program
                if (course?.ProgramId != student.ProgramId)
                    continue;

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

                availableClasses.Add(new
                {
                    classId = classItem.ClassId,
                    courseCode = course?.CourseCode ?? "N/A",
                    courseName = course?.CourseName ?? "N/A",
                    facultyName = facultyUser != null ? $"{facultyUser.UserFirstName} {facultyUser.UserLastName}" : "N/A",
                    schedule = scheduleText,
                    roomCode = room?.RoomCode ?? "N/A"
                });
            }

            _logger.LogInformation($"GetAvailableClasses - Found {availableClasses.Count} available classes");
            return Json(new { success = true, data = availableClasses });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting available classes: {ex.Message}");
            return Json(new { success = false, data = new List<object>(), message = $"Error: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequest request)
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { success = false, message = "User not authenticated" });
            
            var student = (await _studentRepository.GetAllAsync())
                .FirstOrDefault(s => s.UserId == userId.Value);

            if (student == null)
                return Json(new { success = false, message = "Student profile not found" });

            var enrollment = new Enrollment
            {
                StudId = student.StudId,
                ClassId = request.ClassId,
                EnrollAt = DateTime.Now
            };

            var result = await _enrollmentRepository.AddAsync(enrollment);

            if (result)
                return Json(new { success = true, message = "Successfully enrolled in class" });

            return Json(new { success = false, message = "Failed to enroll in class" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enrolling in class");
            return Json(new { success = false, message = "An error occurred while enrolling" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Unenroll([FromBody] UnenrollRequest request)
    {
        try
        {
            _logger.LogInformation($"Attempting to unenroll: enrollId={request.EnrollId}");
            
            var result = await _enrollmentRepository.DeleteAsync(request.EnrollId);

            if (result)
            {
                _logger.LogInformation($"Successfully unenrolled: enrollId={request.EnrollId}");
                return Json(new { success = true, message = "Successfully unenrolled from class" });
            }

            _logger.LogWarning($"Failed to unenroll: enrollId={request.EnrollId}");
            return Json(new { success = false, message = "Failed to unenroll from class" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error unenrolling from class: {ex.Message}");
            return Json(new { success = false, message = "An error occurred while unenrolling" });
        }
    }

    public class EnrollRequest
    {
        public int ClassId { get; set; }
    }

    public class UnenrollRequest
    {
        public int EnrollId { get; set; }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
