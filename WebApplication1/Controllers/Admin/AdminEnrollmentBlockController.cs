using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

[RoleAuthorize(USER_ROLES.ADMIN)]
public class AdminEnrollmentBlockController : Controller
{
    private readonly ILogger<AdminEnrollmentBlockController> _logger;
    private readonly IEnrollmentBlockRepository _enrollmentBlockRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IClassRepository _classRepository;
    private readonly IFacultyRepository _facultyRepository;
    private readonly IUserRepository _userRepository;
    private readonly AdminAuditService _auditService;

    public AdminEnrollmentBlockController(
        ILogger<AdminEnrollmentBlockController> logger,
        IEnrollmentBlockRepository enrollmentBlockRepository,
        IStudentRepository studentRepository,
        IClassRepository classRepository,
        IFacultyRepository facultyRepository,
        IUserRepository userRepository,
        AdminAuditService auditService)
    {
        _logger = logger;
        _enrollmentBlockRepository = enrollmentBlockRepository;
        _studentRepository = studentRepository;
        _classRepository = classRepository;
        _facultyRepository = facultyRepository;
        _userRepository = userRepository;
        _auditService = auditService;
    }

    // GET: AdminEnrollmentBlock/Index
    public IActionResult Index()
    {
        return View("~/Views/Admin/EnrollmentBlocks.cshtml");
    }

    // GET: AdminEnrollmentBlock/GetAll - DataTables AJAX endpoint
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            _logger.LogInformation("Fetching all enrollment blocks...");
            var blocks = await GetAllBlocksWithDetailsAsync();
            _logger.LogInformation($"Returning {blocks.Count()} enrollment block records");
            return Json(new { data = blocks });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching enrollment blocks: {Message}", ex.Message);
            return Json(new { error = $"Failed to fetch enrollment blocks: {ex.Message}" });
        }
    }

    // GET: AdminEnrollmentBlock/GetById/{id}
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var block = await _enrollmentBlockRepository.GetByIdAsync(id);
            if (block == null)
            {
                return Json(new { success = false, message = "Enrollment block not found" });
            }

            return Json(new { success = true, data = block });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching enrollment block by ID");
            return Json(new { success = false, message = "Failed to fetch enrollment block" });
        }
    }

    // POST: AdminEnrollmentBlock/Unblock/{id}
    [HttpPost]
    public async Task<IActionResult> Unblock(int id)
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var block = await _enrollmentBlockRepository.GetByIdAsync(id);
            if (block == null)
            {
                return Json(new { success = false, message = "Enrollment block not found" });
            }

            if (!block.IsActive)
            {
                return Json(new { success = false, message = "This block has already been lifted" });
            }

            var result = await _enrollmentBlockRepository.RemoveBlockAsync(id, userId.Value);

            if (result)
            {
                // Get details for audit log
                var student = await _studentRepository.GetByIdAsync(block.StudId);
                var classInfo = await _classRepository.GetByIdAsync(block.ClassId);
                
                var studentName = "Unknown Student";
                if (student != null)
                {
                    var user = await _userRepository.GetByIdAsync(student.UserId);
                    studentName = user != null ? $"{user.UserFirstName} {user.UserLastName}".Trim() : student.StudNumber;
                }

                await _auditService.LogActionAsync(
                    "Unblocked Student Enrollment",
                    $"Lifted enrollment block for student: {studentName} (Class ID: {block.ClassId})");

                _logger.LogInformation("Admin {UserId} unblocked student {StudId} from class {ClassId}", 
                    userId.Value, block.StudId, block.ClassId);

                return Json(new { success = true, message = "Enrollment block has been lifted. The student can now re-enroll in this class." });
            }

            return Json(new { success = false, message = "Failed to lift enrollment block" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblocking enrollment");
            return Json(new { success = false, message = "An error occurred while lifting the block" });
        }
    }

    // Helper method to get all blocks with full details
    private async Task<IEnumerable<object>> GetAllBlocksWithDetailsAsync()
    {
        var blocks = await GetAllBlocksAsync();
        var result = new List<object>();

        foreach (var block in blocks)
        {
            // Get student details
            var student = await _studentRepository.GetByIdAsync(block.StudId);
            string studentName = "Unknown";
            string studentNumber = "N/A";
            
            if (student != null)
            {
                studentNumber = student.StudNumber;
                var studentUser = await _userRepository.GetByIdAsync(student.UserId);
                if (studentUser != null)
                {
                    studentName = $"{studentUser.UserFirstName} {studentUser.UserLastName}".Trim();
                }
            }

            // Get class details
            var classInfo = await _classRepository.GetByIdAsync(block.ClassId);
            string courseCode = "N/A";
            string courseName = "Unknown Class";
            
            if (classInfo?.CourseId != null)
            {
                var course = await GetCourseByIdAsync(classInfo.CourseId.Value);
                if (course != null)
                {
                    courseCode = course.CourseCode ?? "N/A";
                    courseName = course.CourseName ?? "Unknown";
                }
            }

            // Get faculty who blocked
            string blockedByName = "Unknown";
            var faculty = await _facultyRepository.GetByIdAsync(block.BlockedByFacId);
            if (faculty != null)
            {
                var facultyUser = await _userRepository.GetByIdAsync(faculty.UserId);
                if (facultyUser != null)
                {
                    blockedByName = $"{facultyUser.UserFirstName} {facultyUser.UserLastName}".Trim();
                }
            }

            // Get unblocked by user if applicable
            string unblockedByName = null;
            if (block.UnblockedBy.HasValue)
            {
                var unblockedByUser = await _userRepository.GetByIdAsync(block.UnblockedBy.Value);
                if (unblockedByUser != null)
                {
                    unblockedByName = $"{unblockedByUser.UserFirstName} {unblockedByUser.UserLastName}".Trim();
                }
            }

            result.Add(new
            {
                blockId = block.BlockId,
                studId = block.StudId,
                studentName = studentName,
                studentNumber = studentNumber,
                classId = block.ClassId,
                courseCode = courseCode,
                courseName = courseName,
                blockedByFacId = block.BlockedByFacId,
                blockedByName = blockedByName,
                blockReason = block.BlockReason ?? "No reason provided",
                blockedAt = block.BlockedAt,
                isActive = block.IsActive,
                unblockedAt = block.UnblockedAt,
                unblockedByName = unblockedByName
            });
        }

        return result;
    }

    // Get all blocks from database
    private async Task<IEnumerable<Models.EnrollmentBlock>> GetAllBlocksAsync()
    {
        return await _enrollmentBlockRepository.GetAllAsync();
    }

    // Get course by ID
    private async Task<Models.Course?> GetCourseByIdAsync(int courseId)
    {
        var courseRepo = HttpContext.RequestServices.GetRequiredService<ICourseRepository>();
        return await courseRepo.GetByIdAsync(courseId);
    }
}
