using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Faculty
{
    [RoleAuthorize(USER_ROLES.FACULTY)]
    public class FacultyClassController : Controller
    {
        private readonly ILogger<FacultyClassController> _logger;
        private readonly IFacultyRepository _facultyRepository;
        private readonly IClassRepository _classRepository;

        public FacultyClassController(
            ILogger<FacultyClassController> logger,
            IFacultyRepository facultyRepository,
            IClassRepository classRepository)
        {
            _logger = logger;
            _facultyRepository = facultyRepository;
            _classRepository = classRepository;
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
                    roomName = c.RoomName,
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
    }
}
