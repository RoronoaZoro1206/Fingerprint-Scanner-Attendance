using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Faculty
{
    public class FacultyController : Controller
    {
        private readonly ILogger<FacultyController> _logger;
        private readonly IFacultyRepository _facultyRepository;
        private readonly IClassRepository _classRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IClassScheduleRepository _classScheduleRepository;

        public FacultyController(
            ILogger<FacultyController> logger,
            IFacultyRepository facultyRepository,
            IClassRepository classRepository,
            IEnrollmentRepository enrollmentRepository,
            IClassScheduleRepository classScheduleRepository)
        {
            _logger = logger;
            _facultyRepository = facultyRepository;
            _classRepository = classRepository;
            _enrollmentRepository = enrollmentRepository;
            _classScheduleRepository = classScheduleRepository;
        }

        public IActionResult Index()
        {
            return View();
        }
        
        public IActionResult FacultyAttendance()
        {
            return View();
        }
        
        public IActionResult FacultyRegisterFingerprint()
        {
            return View();
        }

        // GET: Faculty/GetMyClasses
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

                // Get all classes for this faculty
                var classes = await _classRepository.GetClassesByFacultyIdAsync(faculty.FacId);
                
                // Get schedules and enrollment count for each class
                var classesData = new List<object>();
                foreach (var cls in classes)
                {
                    var schedules = await _classScheduleRepository.GetByClassIdAsync(cls.ClassId);
                    var enrollments = await _enrollmentRepository.GetByClassIdAsync(cls.ClassId);

                    // Format schedule string
                    var scheduleString = "";
                    if (schedules.Any())
                    {
                        var firstSchedule = schedules.First();
                        scheduleString = $"{firstSchedule.SchedDayOfWeek} {firstSchedule.SchedStartTime:hh\\:mm} - {firstSchedule.SchedEndTime:hh\\:mm}";
                    }

                    classesData.Add(new
                    {
                        classId = cls.ClassId,
                        courseCode = cls.Course?.CourseCode ?? "",
                        courseName = cls.Course?.CourseName ?? "N/A",
                        roomName = cls.Room?.RoomCode ?? "N/A",
                        schedule = scheduleString,
                        studentCount = enrollments.Count(),
                        termName = cls.Term?.TermName ?? "N/A"
                    });
                }

                return Json(new { success = true, data = classesData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching faculty classes");
                return Json(new { success = false, message = "Failed to fetch classes" });
            }
        }
    }
}
