using System.Diagnostics;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Student;

[RoleAuthorize(USER_ROLES.STUDENT)]
public class StudentEnrollmentController : Controller
{
    private readonly ILogger<StudentEnrollmentController> _logger;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IStudentRepository _studentRepository;

    public StudentEnrollmentController(
        ILogger<StudentEnrollmentController> logger,
        IEnrollmentRepository enrollmentRepository,
        IStudentRepository studentRepository)
    {
        _logger = logger;
        _enrollmentRepository = enrollmentRepository;
        _studentRepository = studentRepository;
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

            // Single optimized query instead of 3 separate queries
            var studentInfo = await _studentRepository.GetStudentInfoByUserIdAsync(userId.Value);

            if (studentInfo == null)
            {
                _logger.LogWarning($"GetStudentInfo - No student found for userId: {userId.Value}");
                return Json(new { success = false, message = "Student profile not found. Please complete your application first." });
            }

            if (!studentInfo.ProgramId.HasValue)
            {
                _logger.LogWarning($"GetStudentInfo - Student {studentInfo.StudId} has no program assigned");
                return Json(new { success = false, message = "Please complete your student application to enroll in classes." });
            }

            _logger.LogInformation($"GetStudentInfo - Student: {studentInfo.StudId}, Program: {studentInfo.ProgramName}");

            return Json(new
            {
                success = true,
                data = new
                {
                    studId = studentInfo.StudId,
                    studNumber = studentInfo.StudNumber,
                    fullName = studentInfo.FullName,
                    programId = studentInfo.ProgramId,
                    programName = studentInfo.ProgramName,
                    yearLevel = studentInfo.YearLevel,
                    hasProfile = studentInfo.HasProfile
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
            
            // Get student info with single query
            var studentInfo = await _studentRepository.GetStudentInfoByUserIdAsync(userId.Value);

            if (studentInfo == null)
            {
                _logger.LogWarning($"GetCurrentEnrollments - No student found for userId: {userId.Value}");
                return Json(new { success = false, data = new List<object>(), message = "Student profile not found" });
            }

            _logger.LogInformation($"GetCurrentEnrollments - Student: {studentInfo.StudId}");

            // Single optimized query with all JOINs instead of 7 separate queries
            var enrollments = await _enrollmentRepository.GetStudentEnrollmentsWithDetailsAsync(studentInfo.StudId);

            _logger.LogInformation($"GetCurrentEnrollments - Returning {enrollments.Count()} enrollments");
            return Json(new { success = true, data = enrollments });
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
            
            // Get student info with single query
            var studentInfo = await _studentRepository.GetStudentInfoByUserIdAsync(userId.Value);

            if (studentInfo == null)
            {
                _logger.LogWarning($"GetAvailableClasses - No student found for userId: {userId.Value}");
                return Json(new { success = false, data = new List<object>(), message = "Student profile not found" });
            }

            if (!studentInfo.ProgramId.HasValue)
            {
                _logger.LogWarning($"GetAvailableClasses - Student {studentInfo.StudId} has no program assigned");
                return Json(new { success = false, data = new List<object>(), message = "No program assigned" });
            }

            _logger.LogInformation($"GetAvailableClasses - Student: {studentInfo.StudId}, Program: {studentInfo.ProgramId}");

            // Single optimized query with all JOINs and filtering instead of 7 separate queries
            var availableClasses = await _enrollmentRepository.GetAvailableClassesForStudentAsync(
                studentInfo.StudId, 
                studentInfo.ProgramId.Value);

            _logger.LogInformation($"GetAvailableClasses - Found {availableClasses.Count()} available classes");
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
            
            // Get student info with single query
            var studentInfo = await _studentRepository.GetStudentInfoByUserIdAsync(userId.Value);

            if (studentInfo == null)
                return Json(new { success = false, message = "Student profile not found" });

            var enrollment = new Enrollment
            {
                StudId = studentInfo.StudId,
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
