using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Faculty
{
    [RoleAuthorize(USER_ROLES.FACULTY)]
    public class FacultyClassController : Controller
    {
        private readonly ILogger<FacultyClassController> _logger;
        private readonly IFacultyRepository _facultyRepository;
        private readonly IClassRepository _classRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IEnrollmentBlockRepository _enrollmentBlockRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly UniversalAuditService _auditService;

        public FacultyClassController(
            ILogger<FacultyClassController> logger,
            IFacultyRepository facultyRepository,
            IClassRepository classRepository,
            IEnrollmentRepository enrollmentRepository,
            IEnrollmentBlockRepository enrollmentBlockRepository,
            ICourseRepository courseRepository,
            UniversalAuditService auditService)
        {
            _logger = logger;
            _facultyRepository = facultyRepository;
            _classRepository = classRepository;
            _enrollmentRepository = enrollmentRepository;
            _enrollmentBlockRepository = enrollmentBlockRepository;
            _courseRepository = courseRepository;
            _auditService = auditService;
        }

        // GET: FacultyClass/Index
        public IActionResult Index()
        {
            return View("~/Views/Faculty/Index.cshtml");
        }

        // GET: FacultyClass/GetMyClasses (Optimized with single query)
        [HttpGet]
        public async Task<IActionResult> GetMyClasses()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Get faculty record for this user
                var faculty = await _facultyRepository.GetByUserIdAsync(userId.Value);
                if (faculty == null)
                {
                    return Json(new { success = false, message = "Faculty record not found" });
                }

                // Get all classes with complete data in a single optimized query
                var classesData = await _classRepository.GetFacultyClassesAsync(faculty.FacId);

                var result = classesData.Select(c => new
                {
                    classId = c.ClassId,
                    courseCode = c.CourseCode,
                    courseName = c.CourseName,
                    roomName = c.RoomCode,  // ✅ FIX: Use RoomCode instead of RoomName (RoomName was empty)
                    schedule = c.Schedule,
                    studentCount = c.StudentCount,
                    termName = c.TermName
                });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching faculty classes");
                return Json(new { success = false, message = "Failed to fetch classes" });
            }
        }

        /// <summary>
        /// Get enrolled students for a specific class (for faculty to manage)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEnrolledStudents(int classId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Verify faculty owns this class
                var faculty = await _facultyRepository.GetByUserIdAsync(userId.Value);
                if (faculty == null)
                {
                    return Json(new { success = false, message = "Faculty record not found" });
                }

                var classInfo = await _classRepository.GetByIdAsync(classId);
                if (classInfo == null || classInfo.FacId != faculty.FacId)
                {
                    return Json(new { success = false, message = "You don't have access to this class" });
                }

                // Get enrolled students with details
                var students = await _classRepository.GetEnrolledStudentsForClassAsync(classId);
                
                // Log for debugging
                _logger.LogInformation("GetEnrolledStudents for class {ClassId}: Found {Count} students", 
                    classId, students.Count());
                foreach (var student in students)
                {
                    _logger.LogInformation("Student: EnrollId={EnrollId}, StudId={StudId}, FullName={FullName}, StudNumber={StudNumber}, Program={Program}", 
                        student.EnrollId, student.StudId, student.FullName, student.StudNumber, student.ProgramName);
                }

                return Json(new { success = true, data = students });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enrolled students for class {ClassId}", classId);
                return Json(new { success = false, message = "Failed to fetch enrolled students" });
            }
        }

        /// <summary>
        /// Force unenroll a student and block them from re-enrolling
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ForceUnenrollStudent([FromBody] ForceUnenrollRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Json(new { success = false, message = "Invalid request" });
                }

                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Verify faculty owns this class
                var faculty = await _facultyRepository.GetByUserIdAsync(userId.Value);
                if (faculty == null)
                {
                    return Json(new { success = false, message = "Faculty record not found" });
                }

                var classInfo = await _classRepository.GetByIdAsync(request.ClassId);
                if (classInfo == null || classInfo.FacId != faculty.FacId)
                {
                    return Json(new { success = false, message = "You don't have access to this class" });
                }

                // Get enrollment record
                var enrollment = await _enrollmentRepository.GetByIdAsync(request.EnrollId);
                if (enrollment == null || enrollment.ClassId != request.ClassId)
                {
                    return Json(new { success = false, message = "Enrollment not found" });
                }

                // Delete the enrollment
                var deleteResult = await _enrollmentRepository.DeleteAsync(request.EnrollId);
                if (!deleteResult)
                {
                    return Json(new { success = false, message = "Failed to remove student from class" });
                }

                // Create enrollment block to prevent re-enrollment
                var block = new EnrollmentBlock
                {
                    StudId = enrollment.StudId,
                    ClassId = request.ClassId,
                    BlockedByFacId = faculty.FacId,
                    BlockReason = request.Reason ?? "Force unenrolled by faculty",
                    BlockedAt = DateTime.Now
                };

                await _enrollmentBlockRepository.AddBlockAsync(block);

                // Log audit
                var course = classInfo.CourseId.HasValue 
                    ? await _courseRepository.GetByIdAsync(classInfo.CourseId.Value) 
                    : null;
                
                _logger.LogInformation(
                    "Faculty {FacId} force-unenrolled student {StudId} from class {ClassId} ({CourseCode}). Reason: {Reason}",
                    faculty.FacId, enrollment.StudId, request.ClassId, 
                    course?.CourseCode ?? "N/A", request.Reason ?? "No reason provided");

                return Json(new { 
                    success = true, 
                    message = "Student has been removed from the class and blocked from re-enrolling" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force-unenrolling student");
                return Json(new { success = false, message = "An error occurred while removing the student" });
            }
        }

        /// <summary>
        /// Get class details including course info
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetClassDetails(int classId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var faculty = await _facultyRepository.GetByUserIdAsync(userId.Value);
                if (faculty == null)
                {
                    return Json(new { success = false, message = "Faculty record not found" });
                }

                var classInfo = await _classRepository.GetByIdAsync(classId);
                if (classInfo == null || classInfo.FacId != faculty.FacId)
                {
                    return Json(new { success = false, message = "Class not found or access denied" });
                }

                var course = classInfo.CourseId.HasValue 
                    ? await _courseRepository.GetByIdAsync(classInfo.CourseId.Value) 
                    : null;

                var schedules = await _classRepository.GetClassSchedulesAsync(classId);
                var scheduleStr = schedules.Any() 
                    ? string.Join(", ", schedules.Select(s => $"{s.DayOfWeek} {s.StartTime}-{s.EndTime}"))
                    : "TBA";

                return Json(new { 
                    success = true, 
                    data = new {
                        classId = classInfo.ClassId,
                        courseCode = course?.CourseCode ?? "N/A",
                        courseName = course?.CourseName ?? "N/A",
                        schedule = scheduleStr
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching class details");
                return Json(new { success = false, message = "Failed to fetch class details" });
            }
        }
    }

    public class ForceUnenrollRequest
    {
        public int EnrollId { get; set; }
        public int ClassId { get; set; }
        public string? Reason { get; set; }
    }
}
